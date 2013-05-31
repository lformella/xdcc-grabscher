using System;
using System.IO;

using SharpRobin.Core;

namespace XG.Server.Helper
{
	class Rrd
	{
		public RrdDb GetDb ()
		{
			string _dbPath = Settings.Instance.AppDataPath + Path.DirectorySeparatorChar + "xgsnapshots.db";
			try
			{
				return new RrdDb(_dbPath);
			}
			catch (FileNotFoundException)
			{
				int minutes = Settings.Instance.TakeSnapshotTimeInMinutes;
				int hour = 60 / minutes;

				RrdDef rrdDef = new RrdDef(_dbPath, 5);
				for (int a = 0; a <= 26; a++)
				{
					rrdDef.addDatasource(a + "", DsTypes.DT_GAUGE, 600, 0, Double.MaxValue);
				}

				rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, 1, hour * 24);
				rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, hour, hour * 24 * 7);
				rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, hour * 12, hour * 24 * 7 * 31);
				//rrdDef.addArchive(ConsolFuns.CF_AVERAGE, 0.5, hour * 24, hour * 24 * 7 * 31 * 365);

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
