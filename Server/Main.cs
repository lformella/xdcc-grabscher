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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using log4net;

using XG.Core;
using XG.Server.Helper;
using XG.Server.Irc;
using XG.Server.Plugin;

namespace XG.Server
{
	/// <summary>
	/// This class holds all information about the files and the irc objects
	/// it does the following things
	/// - loading and saving the object to a file
	/// - calling the ServerHandler to connect to or disconnect from an irc server
	/// - communicate with XG.Server.* plugins, meaning the client
	/// </summary>
	public class Main
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(Main));

		Parser _ircParser;
		Servers _servers;
		FileActions _fileActions;

		Core.Servers _serverObjects;
		Core.Servers Servers
		{
			set
			{
				if(_serverObjects != null)
				{
					_serverObjects.Added -= new ObjectsDelegate(ObjectAdded);
					_serverObjects.Removed -= new ObjectsDelegate(ObjectRemoved);
					_serverObjects.EnabledChanged -= new ObjectDelegate(EnabledChanged);
				}
				_serverObjects = value;
				if(_serverObjects != null)
				{
					_serverObjects.Added += new ObjectsDelegate(ObjectAdded);
					_serverObjects.Removed += new ObjectsDelegate(ObjectRemoved);
					_serverObjects.EnabledChanged += new ObjectDelegate(EnabledChanged);
				}

				_fileActions.Servers = _serverObjects;
			}
		}

		Files _files;
		Files Files
		{
			set
			{
				_files = value;

				_fileActions.Files = _files;
			}
		}

		Objects _searches;
		Objects Searches
		{
			set
			{
				_searches = value;
			}
		}

		Snapshots _snapshots;
		Snapshots Snapshots
		{
			set
			{
				_snapshots = value;
			}
		}

		#endregion

		#region INTITIALIZE, RUN, STOP

		public Main()
		{
			_fileActions = new FileActions();

			_ircParser = new Parser();
			_ircParser.FileActions = _fileActions;
			_ircParser.ParsingError += new DataTextDelegate(IrcParserParsingError);

			_servers = new Servers();
			_servers.FileActions = _fileActions;
			_servers.IrcParser = _ircParser;
		}

		/// <summary>
		/// Run method - should be called via thread
		/// </summary>
		public void Start()
		{
			#region DUPE CHECK

			// check if there are some dupes in our database
			foreach (Core.Server serv in _serverObjects.All)
			{
				foreach (Core.Server s in _serverObjects.All)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						_log.Error("Run() removing dupe server " + s.Name);
						_serverObjects.Remove(s);
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
			foreach (Core.Server tServer in _serverObjects.All)
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

			if (_files.All.Count() > 0 && Settings.Instance.ClearReadyDownloads)
			{
				foreach (File file in _files.All)
				{
					if (file.Enabled)
					{
						_files.Remove(file);
						_log.Info("Run() removing ready file " + file.Name);
					}
				}
			}

			#endregion

			#region CRASH RECOVERY

			if (_files.All.Count() > 0)
			{
				foreach (File file in _files.All)
				{
					// lets check if the directory is still on the harddisk
					if(!System.IO.Directory.Exists(Settings.Instance.TempPath + file.TmpPath))
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
							System.IO.FileInfo info = new System.IO.FileInfo(tmpPath + part.StartSize);
							if (info.Exists)
							{
								// TODO uhm, should we do smt here ?! maybe check the size and set the state to ready?
								if (part.CurrentSize != part.StartSize + info.Length)
								{
									_log.Warn("Run() crash recovery size mismatch of part " + part.StartSize + " from file " + file.TmpPath + " - db:" + part.CurrentSize + " real:" + info.Length);
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
									if (next != null && !next.Checked && next.CurrentSize - next.StartSize >= Settings.Instance.FileRollbackCheck)
									{
										complete = false;
										try
										{
											_log.Fatal("Run() crash recovery checking " + next.Name);
											System.IO.FileStream fileStream = System.IO.File.Open(_fileActions.CompletePath(part), System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
											System.IO.BinaryReader fileReader = new System.IO.BinaryReader(fileStream);
											// extract the needed refernce bytes
											fileStream.Seek(-Settings.Instance.FileRollbackCheck, System.IO.SeekOrigin.End);
											byte[] bytes = fileReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
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
						if (complete && file.Parts.Count() > 0) { _fileActions.CheckFile(file); }
					}
				}
			}

			#endregion

			// connect to all servers which are enabled
			foreach (Core.Server serv in _serverObjects.All)
			{
				// TODO check this
				serv.Parent = null;
				serv.Parent = _serverObjects;

				if (serv.Enabled)
				{
					_servers.ServerConnect(serv);
				}
			}

			#region COLLECT STATISTICS

			while (true)
			{
				IEnumerable<Core.Server> servers = from server in _serverObjects.All select server;
				IEnumerable<Channel> channels = from server in servers from channel in server.Channels select channel;
				IEnumerable<Bot> bots = from channel in channels from bot in channel.Bots select bot;
				IEnumerable<Packet> packets = from bot in bots from packet in bot.Packets select packet;

				Snapshot snap = new Snapshot();
				snap.Set(SnapshotValue.Timestamp, Core.Helper.Date2Timestamp(DateTime.Now));

				snap.Set(SnapshotValue.Speed, (from file in _files.All from part in file.Parts select part.Speed).Sum());

				snap.Set(SnapshotValue.ServersConnected, (from server in servers where server.Connected select server).Count());
				snap.Set(SnapshotValue.ServersDisconnected, (from server in servers where !server.Connected select server).Count());

				snap.Set(SnapshotValue.ChannelsConnected, (from channel in channels where channel.Connected select channel).Count());
				snap.Set(SnapshotValue.ChannelsDisconnected, (from channel in channels where !channel.Connected select channel).Count());

				snap.Set(SnapshotValue.Bots, (from bot in bots select bot).Count());
				snap.Set(SnapshotValue.BotsConnected, (from bot in bots where bot.Connected select bot).Count());
				snap.Set(SnapshotValue.BotsDisconnected, (from bot in bots where !bot.Connected select bot).Count());
				snap.Set(SnapshotValue.BotsFreeSlots, (from bot in bots where bot.InfoSlotCurrent > 0 select bot).Count());
				snap.Set(SnapshotValue.BotsFreeQueue, (from bot in bots where bot.InfoQueueCurrent > 0 select bot).Count());

				snap.Set(SnapshotValue.Packets, (from packet in packets select packet).Count());
				snap.Set(SnapshotValue.PacketsConnected, (from packet in packets where packet.Connected select packet).Count());
				snap.Set(SnapshotValue.PacketsDisconnected, (from packet in packets where !packet.Connected select packet).Count());
				snap.Set(SnapshotValue.PacketsSize, (from packet in packets select packet.Size).Sum());
				snap.Set(SnapshotValue.PacketsSizeConnected, (from packet in packets where packet.Connected select packet.Size).Sum());
				snap.Set(SnapshotValue.PacketsSizeDisconnected, (from packet in packets where !packet.Connected select packet.Size).Sum());

				_snapshots.Add(snap);

				Thread.Sleep((int)Settings.Instance.TimerSnapshotsSleepTime);
			}

			#endregion
		}

		/// <summary>
		/// stop
		/// </summary>
		public void Stop()
		{
			// TODO stop server plugins
			foreach (Core.Server serv in _serverObjects.All)
			{
				_servers.ServerDisconnect(serv);
			}
		}

		#endregion

		#region PLUGINS

		public void AddBackendPlugin(ABackendPlugin aPlugin)
		{
			Servers = aPlugin.LoadServers();
			Files = aPlugin.LoadFiles();
			Searches = aPlugin.LoadSearches();
			Snapshots = aPlugin.LoadStatistics();

			AddPlugin(aPlugin);
		}

		public void AddPlugin(APlugin aPlugin)
		{
			aPlugin.Servers = _serverObjects;
			aPlugin.Files = _files;
			aPlugin.Searches = _searches;
			aPlugin.Snapshots = _snapshots;

			aPlugin.Start();
		}

		#endregion

		#region EVENTHANDLER

		void ObjectAdded(AObject aParent, AObject aObj)
		{
			if(aObj is Core.Server)
			{
				Core.Server aServer = aObj as Core.Server;

				_log.Info("ServerObjectAdded(" + aServer.Name + ")");
				_servers.ServerConnect(aServer);
			}
		}

		void ObjectRemoved(AObject aParent, AObject aObj)
		{
			if(aObj is Core.Server)
			{
				Core.Server aServer = aObj as Core.Server;

				aServer.Enabled = false;
				aServer.Commit();

				_log.Info("ServerObjectRemoved(" + aServer.Name + ")");
				_servers.ServerDisconnect(aServer);
			}
		}

		void EnabledChanged(AObject aObj)
		{
			if(aObj is Core.Server)
			{
				Core.Server aServer = aObj as Core.Server;

				if(aObj.Enabled)
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
					System.IO.StreamWriter sw = new System.IO.StreamWriter(System.IO.File.OpenWrite(Settings.Instance.ParsingErrorFile));
					sw.BaseStream.Seek(0, System.IO.SeekOrigin.End);
					sw.WriteLine(aData.Normalize());
					sw.Close();
				}
				catch (Exception) { }
			}
		}

		#endregion
	}
}
