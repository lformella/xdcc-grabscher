// 
//  Main.cs
// 
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//  

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using XG.Core;
using XG.Server.Helper;
using XG.Server.Irc;
using XG.Server.Plugin;
using XG.Server.Worker;

using log4net;

using File = XG.Core.File;

namespace XG.Server
{
	public class Main : AWorker
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly Parser _ircParser;
		readonly Servers _servers;
		readonly FileActions _fileActions;
		readonly Workers _workers;

		#endregion

		#region FUNCTIONS

		public Main()
		{
			_fileActions = new FileActions();

			_ircParser = new Parser();
			_ircParser.FileActions = _fileActions;
			_ircParser.ParsingError += IrcParserParsingError;

			_servers = new Servers();
			_servers.FileActions = _fileActions;
			_servers.IrcParser = _ircParser;

			_workers = new Workers();
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			#region DUPE CHECK

			// check if there are some dupes in our database
			foreach (Core.Server serv in Servers.All)
			{
				foreach (Core.Server s in Servers.All)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						_log.Error("Run() removing dupe server " + s.Name);
						Servers.Remove(s);
					}
				}

				foreach (Channel chan in serv.Channels)
				{
					foreach (Channel c in serv.Channels)
					{
						if (c.Name == chan.Name && c.Guid != chan.Guid)
						{
							_log.Error("Run() removing dupe channel " + c.Name);
							serv.RemoveChannel(c);
						}
					}

					foreach (Bot bot in chan.Bots)
					{
						foreach (Bot b in chan.Bots)
						{
							if (b.Name == bot.Name && b.Guid != bot.Guid)
							{
								_log.Error("Run() removing dupe bot " + b.Name);
								chan.RemoveBot(b);
							}
						}

						foreach (Packet pack in bot.Packets)
						{
							foreach (Packet p in bot.Packets)
							{
								if (p.Id == pack.Id && p.Guid != pack.Guid)
								{
									_log.Error("Run() removing dupe Packet " + p.Name);
									bot.RemovePacket(p);
								}
							}
						}
					}
				}
			}

			#endregion

			#region RESET

			// reset all objects if the server crashed
			foreach (Core.Server tServer in Servers.All)
			{
				tServer.Connected = false;
				tServer.ErrorCode = SocketErrorCode.None;

				foreach (Channel tChannel in tServer.Channels)
				{
					tChannel.Connected = false;
					tChannel.ErrorCode = 0;

					foreach (Bot tBot in tChannel.Bots)
					{
						tBot.Connected = false;
						tBot.State = Bot.States.Idle;

						foreach (Packet pack in tBot.Packets)
						{
							pack.Connected = false;
						}
					}
				}
			}

			#endregion

			#region CLEAR OLD DL

			if (Files.All.Count() > 0 && Settings.Instance.ClearReadyDownloads)
			{
				foreach (File file in Files.All)
				{
					if (file.Enabled)
					{
						Files.Remove(file);
						_log.Info("Run() removing ready file " + file.Name);
					}
				}
			}

			#endregion

			#region CRASH RECOVERY

			if (Files.All.Count() > 0)
			{
				foreach (File file in Files.All)
				{
					// lets check if the directory is still on the harddisk
					if (!Directory.Exists(Settings.Instance.TempPath + file.TmpPath))
					{
						_log.Warn("Run() crash recovery directory " + file.TmpPath + " is missing ");
						_fileActions.RemoveFile(file);
						continue;
					}

					file.Lock = new object();

					if (!file.Enabled)
					{
						bool complete = true;
						string tmpPath = Settings.Instance.TempPath + file.TmpPath;

						foreach (FilePart part in file.Parts)
						{
							// first part is always checked!
							if (part.StartSize == 0)
							{
								part.Checked = true;
							}

							// check if the real file and the part is actual the same
							FileInfo info = new FileInfo(tmpPath + part.StartSize);
							if (info.Exists)
							{
								// TODO uhm, should we do smt here ?! maybe check the size and set the state to ready?
								if (part.CurrentSize != part.StartSize + info.Length)
								{
									_log.Warn("Run() crash recovery size mismatch of part " + part.StartSize + " from file " + file.TmpPath + " - db:" + part.CurrentSize +
									          " real:" + info.Length);
									part.CurrentSize = part.StartSize + info.Length;
									complete = false;
								}
							}
							else
							{
								_log.Error("Run() crash recovery part " + part.StartSize + " of file " + file.TmpPath + " is missing");
								_fileActions.RemovePart(file, part);
								complete = false;
							}

							// uhh, this is bad - close it and hope it works again
							if (part.State == FilePart.States.Open)
							{
								part.State = FilePart.States.Closed;
								complete = false;
							}
								// the file is closed, so do smt
							else
							{
								// check the file for safety
								if (part.Checked && part.State == FilePart.States.Ready)
								{
									FilePart next = file.Next(part) as FilePart;
									if (next != null && !next.Checked && next.CurrentSize - next.StartSize >= Settings.Instance.FileRollbackCheckBytes)
									{
										complete = false;
										try
										{
											_log.Fatal("Run() crash recovery checking " + next.Name);
											FileStream fileStream = System.IO.File.Open(_fileActions.CompletePath(part), FileMode.Open, FileAccess.ReadWrite);
											BinaryReader fileReader = new BinaryReader(fileStream);
											// extract the needed refernce bytes
											fileStream.Seek(-Settings.Instance.FileRollbackCheckBytes, SeekOrigin.End);
											byte[] bytes = fileReader.ReadBytes(Settings.Instance.FileRollbackCheckBytes);
											fileReader.Close();

											_fileActions.CheckNextReferenceBytes(part, bytes);
										}
										catch (Exception ex)
										{
											_log.Fatal("Run() crash recovery", ex);
										}
									}
								}
								else
								{
									complete = false;
								}
							}
						}

						// check and maybee join the files if something happend the last run
						// for exaple the disk was full or the rights were not there
						if (complete && file.Parts.Count() > 0)
						{
							_fileActions.CheckFile(file);
						}
					}
				}
			}

			#endregion

			#region CONNECT ALL ENABLED SERVERS

			foreach (Core.Server serv in Servers.All)
			{
				// TODO check this
				serv.Parent = null;
				serv.Parent = Servers;

				if (serv.Enabled)
				{
					_servers.ServerConnect(serv);
				}
			}

			#endregion

			#region WORKERS

			ALoopWorker worker;

			worker = new SnapshotWorker();
			worker.SecondsToSleep = Settings.Instance.TakeSnapshotTime;
			AddWorker(worker);

			worker = new BotWatchdogWorker();
			worker.SecondsToSleep = Settings.Instance.BotOfflineCheckTime;
			AddWorker(worker);

			#endregion
		}

		protected override void StopRun()
		{
			foreach (Core.Server serv in Servers.All)
			{
				_servers.ServerDisconnect(serv);
			}

			_workers.StopAll();
		}

		#endregion

		#region PLUGINS / WORKERS

		public void AddBackendPlugin(ABackendPlugin aPlugin)
		{
			Servers = aPlugin.LoadServers();
			_fileActions.Servers = Servers;
			Files = aPlugin.LoadFiles();
			_fileActions.Files = Files;
			Searches = aPlugin.LoadSearches();
			Snapshots = aPlugin.LoadStatistics();

			if (Snapshots.All.Count > 0)
			{
				// put a empty snapshot on top to nullify values
				var lasttime = (from snapshot in Snapshots.All orderby snapshot.Get(SnapshotValue.Timestamp) select snapshot).Last().Get(SnapshotValue.Timestamp);
				Snapshot lastSnapshot = new Snapshot();
				lastSnapshot.Set(SnapshotValue.Timestamp, lasttime + 1);
				Snapshots.Add(lastSnapshot);

				Snapshot firstSnapshot = new Snapshot();
				firstSnapshot.Set(SnapshotValue.Timestamp, DateTime.Now.ToTimestamp());
				Snapshots.Add(firstSnapshot);
			}

			AddWorker(aPlugin);
		}

		public void AddWorker(AWorker aWorker)
		{
			aWorker.Servers = Servers;
			aWorker.Files = Files;
			aWorker.Searches = Searches;
			aWorker.Snapshots = Snapshots;

			_workers.Add(aWorker);
			aWorker.Start();
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded(AObject aParent, AObject aObj)
		{
			if (aObj is Core.Server)
			{
				Core.Server aServer = aObj as Core.Server;

				_log.Info("ServerObjectAdded(" + aServer.Name + ")");
				_servers.ServerConnect(aServer);
			}
		}

		protected override void ObjectRemoved(AObject aParent, AObject aObj)
		{
			if (aObj is Core.Server)
			{
				Core.Server aServer = aObj as Core.Server;

				aServer.Enabled = false;
				aServer.Commit();

				_log.Info("ServerObjectRemoved(" + aServer.Name + ")");
				_servers.ServerDisconnect(aServer);
			}
		}

		protected override void ObjectEnabledChanged(AObject aObj)
		{
			if (aObj is Core.Server)
			{
				Core.Server aServer = aObj as Core.Server;

				if (aObj.Enabled)
				{
					_servers.ServerConnect(aServer);
				}
				else
				{
					_servers.ServerDisconnect(aServer);
				}
			}
		}

		void IrcParserParsingError(string aData)
		{
			lock (this)
			{
				try
				{
					StreamWriter sw = new StreamWriter(System.IO.File.OpenWrite(Settings.Instance.ParsingErrorFile));
					sw.BaseStream.Seek(0, SeekOrigin.End);
					sw.WriteLine(aData.Normalize());
					sw.Close();
				}
				catch (Exception) {}
			}
		}

		#endregion
	}
}
