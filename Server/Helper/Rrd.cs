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
				int heartBeat = 5 * 60; // * 1000

				RrdDef rrdDef = new RrdDef(_dbPath, heartBeat);
				for (int a = 0; a <= 26; a++)
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
