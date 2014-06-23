// 
//  Rrd.cs
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
using SharpRobin.Core;
using XG.Config.Properties;

namespace XG.Business.Helper
{
	public class Rrd
	{
		public RrdDb GetDb ()
		{
			const int heartBeat = 5 * 60; // * 1000

			string _dbPath = Settings.Default.GetAppDataPath() + "xgsnapshots.db";
			try
			{
				var db = new RrdDb(_dbPath);
				var sourcesToAdd = new HashSet<int>();

				for (int a = 0; a <= 29; a++)
				{
					if (!db.containsDs(a + ""))
					{
						sourcesToAdd.Add(a);
					}
				}

				if (sourcesToAdd.Count > 0)
				{
					db.close();
					foreach (int a in sourcesToAdd)
					{
						var dsDef = new DsDef(a + "", DsTypes.DT_GAUGE, heartBeat * 2, 0, Double.MaxValue);
						RrdToolkit.addDatasource(_dbPath, _dbPath + ".new", dsDef);
						FileSystem.DeleteFile(_dbPath);
						FileSystem.MoveFile(_dbPath + ".new", _dbPath);
					}
					db = new RrdDb(_dbPath);
				}

				return db;
			}
			catch (FileNotFoundException)
			{
				var rrdDef = new RrdDef(_dbPath, heartBeat);
				for (int a = 0; a <= 29; a++)
				{
					rrdDef.addDatasource(a + "", DsTypes.DT_GAUGE, heartBeat * 2, 0, Double.MaxValue);
				}

				rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, 1, 12 * 24); // one day > 1 step = 5 minutes, 12 times per hour * 24 hours
				rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, 12, 24 * 7); // one week > 12 steps = 1 hour, 24 times per day * 7 days
				rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, 4 * 12, 6 * 31); // one month > 4 * 12 steps = 4 hours, 6 times per day * 31 days
				rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, 2 * 24 * 12, 183); // one year > 2 * 24 * 12 steps = 2 days, 183 days

				try
				{
					new RrdDb(rrdDef).close();
				}
				catch (NullReferenceException) {}
				return new RrdDb(_dbPath);
			}
		}
	}
}
