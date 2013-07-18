using System;
using System.Collections.Generic;
using System.IO;

using XG.Server.Worker;

using SharpRobin.Core;

namespace XG.Server.Helper
{
	class Rrd
	{
		public RrdDb GetDb ()
		{
			int heartBeat = 5 * 60; // * 1000

			string _dbPath = Settings.Instance.AppDataPath + Path.DirectorySeparatorChar + "xgsnapshots.db";
			try
			{
				var db = new RrdDb(_dbPath);
				HashSet<int> sourcesToAdd = new HashSet<int>();

				for (int a = 0; a <= Snapshot.SnapshotCount; a++)
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
				RrdDef rrdDef = new RrdDef(_dbPath, heartBeat);
				for (int a = 0; a <= Snapshot.SnapshotCount; a++)
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
