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
using Quartz.Impl;
using SharpRobin.Core;
using XG.Config.Properties;
using XG.DB;
using XG.Model.Domain;
using XG.Plugin;
using log4net;
using XG.Business.Helper;

namespace XG.Business
{
	public class App : APlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly Dao _dao;

		readonly Plugins _plugins;

		readonly RrdDb _rrdDb;

		public RrdDb RrdDb
		{
			get
			{
				return _rrdDb;
			}
		}

		public bool ShutdownInProgress { get; private set; }

		#endregion

		#region EVENTS

		public virtual event EventHandler OnShutdownComplete = delegate {};

		protected void FireShutdownComplete(object aSender, EventArgs aEventArgs)
		{
			OnShutdownComplete(aSender, aEventArgs);
		}

		#endregion

		#region FUNCTIONS

		public App()
		{
			Scheduler = new StdSchedulerFactory().GetScheduler();

			_dao = new Dao();
			_dao.Scheduler = Scheduler;
			LoadObjects();

			FileActions.OnNotificationAdded += NotificationAdded;

			_plugins = new Plugins();
			_rrdDb = new Rrd().GetDb();

			Objects.CheckAndRemoveDuplicates(Servers);
			ClearOldDownloads();
			TryToRecoverOpenFiles();
		}

		void TryToRecoverOpenFiles()
		{
			foreach (XG.Model.Domain.File file in Files.All)
			{
				var info = new FileInfo(Settings.Default.TempPath + file.TmpName);

				// lets check if the file is still on the harddisk
				if (!info.Exists)
				{
					Log.Warn("TryToRecoverOpenFiles() " + info.FullName + " is missing ");
					Files.Remove(file);
					continue;
				}
				if (!file.Enabled)
				{
					// check if the real file and the part is actual the same
					if (file.CurrentSize != info.Length)
					{
						Log.Warn("TryToRecoverOpenFiles() size mismatch of " + file + " - db:" + file.CurrentSize + " real:" + info.Length);
						file.CurrentSize = info.Length;
					}

					// uhh, this is bad - close it and hope it works again
					if (file.Connected)
					{
						file.Connected = false;
					}

					file.Commit();
					if (!file.Enabled && file.MissingSize == 0)
					{
						FileActions.FinishFile(file);
					}
				}
			}
		}

		void CreateJobs()
		{
			AddRepeatingJob(typeof(Job.Rrd), "RrdDbCollector", "Core", Settings.Default.TakeSnapshotTimeInMinutes * 60, 
				new JobItem("RrdDB", _rrdDb));

			AddRepeatingJob(typeof(Job.BotWatchdog), "BotWatchdog", "Core", Settings.Default.BotOfflineCheckTime, 
				new JobItem("Servers", Servers));
		}

		void ClearOldDownloads()
		{
			List<string> files = Directory.GetFiles(Settings.Default.TempPath).ToList();

			foreach (XG.Model.Domain.File file in Files.All)
			{
				if (file.Enabled)
				{
					Files.Remove(file);
					Log.Info("Run() removing ready " + file);
				}
				else
				{
					string path = Settings.Default.TempPath + file.TmpName;
					files.Remove(path);
				}
			}

			foreach (string dir in files)
			{
				FileSystem.DeleteFile(dir);
			}
		}

		void LoadObjects()
		{
			_dao.Start(typeof(Dao).ToString(), false);

			Servers = _dao.Servers;
			Files = _dao.Files;
			Searches = _dao.Searches;
			ApiKeys = _dao.ApiKeys;

			FileActions.Files = Files;
			FileActions.Servers = Servers;

			Snapshots.Files = Files;
			Snapshots.Servers = Servers;

			Notifications = new Notifications();
		}

		public void Shutdown(object sender)
		{
			if (!ShutdownInProgress)
			{
				ShutdownInProgress = true;
				Log.Warn("OnShutdown() triggered by " + sender);
				Stop();
				FireShutdownComplete(sender, null);
			}
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			CreateJobs();
			_plugins.StartAll();

			Scheduler.Start();
		}

		protected override void StopRun()
		{
			Scheduler.Shutdown();
			_dao.Stop();
			_plugins.StopAll();

			FileActions.OnNotificationAdded -= NotificationAdded;
		}

		public void AddPlugin(APlugin aPlugin)
		{
			aPlugin.Servers = Servers;
			aPlugin.Files = Files;
			aPlugin.Searches = Searches;
			aPlugin.Notifications = Notifications;
			aPlugin.ApiKeys = ApiKeys;
			aPlugin.Scheduler = Scheduler;

			_plugins.Add(aPlugin);
		}

		#endregion
	}
}
