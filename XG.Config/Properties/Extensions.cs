// 
//  Extensions.cs
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
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Quartz;

namespace XG.Config.Properties
{
	public static class Extensions
	{
		public static string GetAppDataPath(this Settings aSettings)
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			appDataPath = appDataPath + (appDataPath.EndsWith("" + Path.DirectorySeparatorChar) ? "" : "" + Path.DirectorySeparatorChar) + "XG" + Path.DirectorySeparatorChar;
			return appDataPath;
		}

		public static IEnumerable<FileHandler> GetFileHandlers(this Settings aSettings)
		{
			var fileHandlers = JsonConvert.DeserializeObject<FileHandler[]>(aSettings.FileHandlers);

			return fileHandlers;
		}

		public static void SetFileHandlers(this Settings aSettings, IEnumerable<FileHandler> aFileHandlers)
		{
			var fileHandlers = JsonConvert.SerializeObject(aFileHandlers);

			aSettings.FileHandlers = fileHandlers;
		}

		public static void AddJob(this IScheduler aScheduler, Type aType, JobKey aKey, int aSecondsToSleep, params JobItem[] aItems)
		{
			var data = new JobDataMap();
			foreach (JobItem item in aItems)
			{
				data.Add(item.Key, item.Value);
			}

			IJobDetail job = JobBuilder.Create(aType)
				.WithIdentity(aKey)
				.UsingJobData(data)
				.Build();

			ITrigger trigger = TriggerBuilder.Create()
				.WithIdentity(aKey.Name, aKey.Group)
				.WithSimpleSchedule(x => x.WithIntervalInSeconds(aSecondsToSleep).RepeatForever())
				.Build();

			aScheduler.ScheduleJob(job, trigger);
		}
	}
}
