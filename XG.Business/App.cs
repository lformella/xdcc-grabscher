// 
//  App.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using SharpRobin.Core;
using XG.Business.Helper;
using XG.Model.Domain;
using XG.Plugin;
using XG.Config.Properties;
using XG.Business.Worker;
using XG.DB;

namespace XG.Business
{
	public class App : APlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly Dao _dao = new Dao();

		readonly Workers _workers;

		RrdDb _rrdDb;

		public RrdDb RrdDb
		{
			get
			{
				return _rrdDb;
			}
		}

		#endregion

		#region FUNCTIONS

		public App()
		{
			FileActions.OnNotificationAdded += NotificationAdded;

			_workers = new Workers();
			_rrdDb = new Helper.Rrd().GetDb();

			LoadObjects();
			CheckForDuplicates();
			ResetObjects();
			ClearOldDownloads();
			TryToRecoverOpenFiles();
		}

		void TryToRecoverOpenFiles()
		{
			foreach (XG.Model.Domain.File file in Files.All)
			{
				// lets check if the directory is still on the harddisk
				if (!Directory.Exists(Settings.Default.TempPath + file.TmpPath))
				{
					Log.Warn("Run() crash recovery directory " + file + " is missing ");
					Files.Remove(file);
					continue;
				}

				if (!file.Enabled)
				{
					bool complete = true;
					string tmpPath = Settings.Default.TempPath + file.TmpPath;

					foreach (FilePart part in file.Parts)
					{
						// first part is always checked!
						if (part.StartSize == 0)
						{
							part.Checked = true;
						}

						// check if the real file and the part is actual the same
						var info = new FileInfo(tmpPath + part.StartSize);
						if (info.Exists)
						{
							// TODO uhm, should we do smt here ?! maybe check the size and set the state to ready?
							if (part.CurrentSize != part.StartSize + info.Length)
							{
								Log.Warn("Run() crash recovery size mismatch of " + part + " from " + file + " - db:" + part.CurrentSize + " real:" + info.Length);
								part.CurrentSize = part.StartSize + info.Length;
								complete = false;
							}
						}
						else
						{
							Log.Error("Run() crash recovery " + part + " of " + file + " is missing");
							FileActions.RemovePart(file, part);
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
								FilePart next = null;
								try
								{
									next = (from currentPart in file.Parts where currentPart.StartSize == part.StopSize select currentPart).Single();
								}
								catch (Exception) {}
								if (next != null && !next.Checked && next.CurrentSize - next.StartSize >= Settings.Default.FileRollbackCheckBytes)
								{
									complete = false;
									try
									{
										Log.Fatal("Run() crash recovery checking " + next.Name);
										byte[] bytes = null;
										using (var fileStream = System.IO.File.Open(FileActions.CompletePath(part), FileMode.Open, FileAccess.ReadWrite))
										{
											var fileReader = new BinaryReader(fileStream);
											// extract the needed refernce bytes
											fileStream.Seek(-Settings.Default.FileRollbackCheckBytes, SeekOrigin.End);
											bytes = fileReader.ReadBytes(Settings.Default.FileRollbackCheckBytes);
											fileReader.Close();
										}
										FileActions.CheckNextReferenceBytes(part, bytes);
									}
									catch (Exception ex)
									{
										Log.Fatal("Run() crash recovery", ex);
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
					if (complete && file.Parts.Any())
					{
						FileActions.CheckFile(file);
					}
				}
			}
		}

		void StartWorkers()
		{
			var snapShotWorker = new Worker.Rrd { SecondsToSleep = Settings.Default.TakeSnapshotTimeInMinutes * 60 };
			snapShotWorker.RrdDB = _rrdDb;
			AddWorker(snapShotWorker);

			AddWorker(new BotWatchdog { SecondsToSleep = Settings.Default.BotOfflineCheckTime });

			_workers.StartAll();
		}

		void ClearOldDownloads()
		{
			List<string> dirs = Directory.GetDirectories(Settings.Default.TempPath).ToList();

			foreach (XG.Model.Domain.File file in Files.All)
			{
				if (file.Enabled)
				{
					Files.Remove(file);
					Log.Info("Run() removing ready " + file);
				}
				else
				{
					string dir = Settings.Default.TempPath + file.TmpPath;
					dirs.Remove(dir.Substring(0, dir.Length - 1));
				}
			}

			foreach (string dir in dirs)
			{
				FileSystem.DeleteDirectory(dir);
			}
		}

		void ResetObjects()
		{
			foreach (Server tServer in Servers.All)
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
						tBot.QueuePosition = 0;
						tBot.QueueTime = 0;

						foreach (Packet pack in tBot.Packets)
						{
							pack.Connected = false;
						}
					}
				}
			}
		}

		void CheckForDuplicates()
		{
			foreach (Server serv in Servers.All)
			{
				foreach (Server s in Servers.All)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						Log.Error("Run() removing dupe " + s);
						Servers.Remove(s);
					}
				}

				foreach (Channel chan in serv.Channels)
				{
					foreach (Channel c in serv.Channels)
					{
						if (c.Name == chan.Name && c.Guid != chan.Guid)
						{
							Log.Error("Run() removing dupe " + c);
							serv.RemoveChannel(c);
						}
					}

					foreach (Bot bot in chan.Bots)
					{
						foreach (Bot b in chan.Bots)
						{
							if (b.Name == bot.Name && b.Guid != bot.Guid)
							{
								Log.Error("Run() removing dupe " + b);
								chan.RemoveBot(b);
							}
						}

						foreach (Packet pack in bot.Packets)
						{
							foreach (Packet p in bot.Packets)
							{
								if (p.Id == pack.Id && p.Guid != pack.Guid)
								{
									Log.Error("Run() removing dupe " + p);
									bot.RemovePacket(p);
								}
							}
						}
					}
				}
			}
		}

		void LoadObjects()
		{
			Servers = _dao.Servers();
			Files = _dao.Files();
			Searches = _dao.Searches();

			FileActions.Files = Files;
			FileActions.Servers = Servers;
			Notifications = new Notifications();
			Snapshots.Servers = Servers;
			Snapshots.Files = Files;
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			StartWorkers();
		}

		protected override void StopRun()
		{
			_workers.StopAll();
			_dao.Dispose();
		}

		public void AddWorker(APlugin aWorker)
		{
			aWorker.Servers = Servers;
			aWorker.Files = Files;
			aWorker.Searches = Searches;
			aWorker.Notifications = Notifications;

			_workers.Add(aWorker);
		}

		#endregion
	}
}
