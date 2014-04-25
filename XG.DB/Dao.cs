// 
//  Dao.cs
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
using System.Linq;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using XG.Config.Properties;
using XG.Model.Domain;
using log4net;
using System.Collections.Generic;
using System.Threading;
using Quartz;

#if __MonoCS__
using Mono.Data.Sqlite;
#else
using System.Data.SQLite;
#endif

namespace XG.DB
{
	public class Dao : IDisposable
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		ISessionFactory _sessions;

		readonly int _version = 1;

		public Servers Servers { get; private set; }
		public Files Files { get; private set; }
		public Searches Searches { get; private set; }
		public ApiKeys ApiKeys { get; private set; }

		bool _writeInProgress { get; set; }
		public DateTime LastSave { get; private set; }
		List<AObject> _objectsAdded = new List<AObject>();
		List<AObject> _objectsChanged = new List<AObject>();
		List<AObject> _objectsRemoved = new List<AObject>();

		public Dao(IScheduler aScheduler)
		{
			bool useSqlite = false;

			var cfg = new Configuration();

			if (System.IO.File.Exists(XG.Config.Properties.Settings.Default.GetAppDataPath() + "hibernate.cfg.xml"))
			{
				try
				{
					cfg.Configure();
					cfg.AddAssembly(typeof(Dao).Assembly);
				}
				catch (HibernateConfigException)
				{
					useSqlite = true;
				}
			}
			else
			{
				useSqlite = true;
			}

			if (useSqlite)
			{
				cfg = CreateSqliteConfiguration();
			}

			_sessions = cfg.BuildSessionFactory();

			CheckIfDatabaseNeedsUpdate();
			LoadObjects();

			// start sync job
			var data = new JobDataMap();
			data.Add("Dao", this);
			data.Add("MaximalTimeBetweenSaves", 300);

			IJobDetail job = JobBuilder.Create<DaoSync>()
				.WithIdentity("DaoSync", "Dao")
				.UsingJobData(data)
				.Build();

			ITrigger trigger = TriggerBuilder.Create()
				.WithIdentity("DaoSync", "Dao")
				.WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
				.Build();

			aScheduler.ScheduleJob(job, trigger);
		}

		Configuration CreateSqliteConfiguration()
		{
			var cfg = new Configuration();

			cfg.Properties["connection.provider"] = "NHibernate.Connection.DriverConnectionProvider";
			cfg.Properties["dialect"] = "NHibernate.Dialect.SQLiteDialect";
			cfg.Properties["query.substitutions"] = "true=1;false=0";

			// mono needs a special driver wrapper
#if __MonoCS__
			cfg.Properties["connection.driver_class"] = "XG.DB.MonoSqliteDriver, XG.DB";
#else
			cfg.Properties["connection.driver_class"] = "NHibernate.Driver.SQLite20Driver";
#endif

			string db = Config.Properties.Settings.Default.GetAppDataPath() + "xgobjects.db";
			cfg.Properties["connection.connection_string"] = "Data Source=" + db + ";Version=3;BinaryGuid=False;synchronous=off;journal mode=memory";

			cfg.AddAssembly(typeof(Dao).Assembly);
			if (!System.IO.File.Exists(db))
			{
				new SchemaExport(cfg).Execute(false, true, false);
			}

			try
			{
#if __MonoCS__
				using (var con = new SqliteConnection(cfg.Properties["connection.connection_string"]))
				{
					con.Open();
					using (SqliteCommand command = con.CreateCommand())
					{
						command.CommandText = "vacuum;";
						command.ExecuteNonQuery();
					}
					con.Close();
				}
#else
				using (var con = new SQLiteConnection(cfg.Properties["connection.connection_string"]))
				{
					con.Open();
					using (SQLiteCommand command = con.CreateCommand())
					{
						command.CommandText = "vacuum;";
						command.ExecuteNonQuery();
					}
					con.Close();
				}
#endif
			}
			catch(Exception) {}

			return cfg;
		}

		void CheckIfDatabaseNeedsUpdate()
		{
			Domain.Version version;

			using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
			{
				version = session.CreateQuery("FROM Version").List<Domain.Version>().OrderByDescending(v => v.Number).FirstOrDefault();
				if (version == null)
				{
					version = new Domain.Version { Number = _version };
					session.Save(version);
					session.Flush();
				}
			}

			TryToUpdateDatabase(version.Number, _version);
		}

		void TryToUpdateDatabase(int aFrom, int aTo)
		{
			if (aFrom == aTo)
			{
				return;
			}

			// add in next version...
		}

		private void LoadObjects()
		{
			Servers = new Servers();
			Files = new Files();
			Searches = new Searches();
			ApiKeys = new ApiKeys();

			using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
			{
				var servers = session.CreateQuery("FROM Server").List<Server>();
				foreach (var server in servers)
				{
					Servers.Add(server);
				}

				var files = session.CreateQuery("FROM File").List<File>();
				foreach (var file in files)
				{
					Files.Add(file);
				}

				var searches = session.CreateQuery("FROM Search").List<Search>();
				foreach (var search in searches)
				{
					Searches.Add(search);
				}

				var apiKeys = session.CreateQuery("FROM ApiKey").List<ApiKey>();
				foreach (var apiKey in apiKeys)
				{
					ApiKeys.Add(apiKey);
				}
			}

			Servers.OnAdded += ObjectAdded;
			Servers.OnRemoved += ObjectRemoved;
			Servers.OnChanged += ObjectChanged;
			Servers.OnEnabledChanged += ObjectEnabledChanged;

			Files.OnAdded += ObjectAdded;
			Files.OnRemoved += ObjectRemoved;
			Files.OnChanged += ObjectChanged;
			Files.OnEnabledChanged += ObjectEnabledChanged;

			Searches.OnAdded += ObjectAdded;
			Searches.OnRemoved += ObjectRemoved;
			Searches.OnChanged += ObjectChanged;
			Searches.OnEnabledChanged += ObjectEnabledChanged;

			ApiKeys.OnAdded += ObjectAdded;
			ApiKeys.OnRemoved += ObjectRemoved;
			ApiKeys.OnChanged += ObjectChanged;
			ApiKeys.OnEnabledChanged += ObjectEnabledChanged;
		}

		void ObjectAdded(object sender, EventArgs<AObject, AObject> eventArgs)
		{
			if (eventArgs.Value1 != Servers && eventArgs.Value1 != Files && eventArgs.Value1 != Searches && eventArgs.Value1 != ApiKeys)
			{
				return;
			}
			if (_objectsChanged.Contains(eventArgs.Value2) || _objectsRemoved.Contains(eventArgs.Value2))
			{
				return;
			}

			TryAddToList(_objectsAdded, eventArgs.Value2);
			CheckIfWriteToDatabaseIsNeeded(eventArgs.Value2);
		}

		void ObjectRemoved(object sender, EventArgs<AObject, AObject> eventArgs)
		{
			if (eventArgs.Value1 != Servers && eventArgs.Value1 != Files && eventArgs.Value1 != Searches && eventArgs.Value1 != ApiKeys)
			{
				return;
			}

			TryAddToList(_objectsRemoved, eventArgs.Value2);
			TryRemoveFromList(_objectsAdded, eventArgs.Value2);
			TryRemoveFromList(_objectsChanged, eventArgs.Value2);
			CheckIfWriteToDatabaseIsNeeded(eventArgs.Value2);
		}

		void ObjectChanged(object sender, EventArgs<AObject, string[]> eventArgs)
		{
			if (_objectsAdded.Contains(eventArgs.Value1) || _objectsRemoved.Contains(eventArgs.Value1))
			{
				return;
			}

			TryAddToList(_objectsChanged, eventArgs.Value1);
		}

		void ObjectEnabledChanged(object sender, EventArgs<AObject> eventArgs)
		{
			TryAddToList(_objectsChanged, eventArgs.Value1);
			WriteToDatabase();
		}

		void TryAddToList(List<AObject> aList, AObject aObject)
		{
			lock (aList)
			{
				if (!aList.Contains(aObject))
				{
					aList.Add(aObject);
				}
			}
		}

		void TryRemoveFromList(List<AObject> aList, AObject aObject)
		{
			lock (aList)
			{
				if (aList.Contains(aObject))
				{
					aList.Remove(aObject);
				}
			}
		}

		void CheckIfWriteToDatabaseIsNeeded(AObject aObject)
		{
			if (aObject is Server || aObject is Channel || aObject is File || aObject is Search || aObject is ApiKey)
			{
				WriteToDatabase();
			}
		}

		internal void WriteToDatabase()
		{
			if (_writeInProgress)
			{
				return;
			}

			_writeInProgress = true;
			LastSave = DateTime.Now;

			try
			{
				lock (_objectsAdded)
				{
					if (_objectsAdded.Count > 0)
					{
						using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
						{
							foreach (AObject obj in _objectsAdded.ToArray())
							{
								session.SaveOrUpdate(obj);
								_objectsAdded.Remove(obj);
							}
							session.Flush();
						}
						_objectsAdded.Clear();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("WriteToDatabase() added ", ex);
			}

			try
			{
				lock (_objectsChanged)
				{
					if (_objectsChanged.Count > 0)
					{
						using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
						{
							foreach (AObject obj in _objectsChanged.ToArray())
							{
								session.SaveOrUpdate(obj);
								_objectsChanged.Remove(obj);
							}
							session.Flush();
						}
						_objectsChanged.Clear();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("WriteToDatabase() changed ", ex);
			}

			try
			{
				lock (_objectsRemoved)
				{
					if (_objectsRemoved.Count > 0)
					{
						using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
						{
							foreach (AObject obj in _objectsRemoved.ToArray())
							{
								session.Delete(obj);
								_objectsRemoved.Remove(obj);
							}
							session.Flush();
						}
						_objectsRemoved.Clear();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("WriteToDatabase() removed ", ex);
			}

			_writeInProgress = false;
			GC.Collect();
		}

		public void Dispose ()
		{
			Servers.OnAdded -= ObjectAdded;
			Servers.OnRemoved -= ObjectRemoved;
			Servers.OnChanged -= ObjectChanged;
			Servers.OnEnabledChanged -= ObjectEnabledChanged;

			Files.OnAdded -= ObjectAdded;
			Files.OnRemoved -= ObjectRemoved;
			Files.OnChanged -= ObjectChanged;
			Files.OnEnabledChanged -= ObjectEnabledChanged;

			Searches.OnAdded -= ObjectAdded;
			Searches.OnRemoved -= ObjectRemoved;
			Searches.OnChanged -= ObjectChanged;
			Searches.OnEnabledChanged -= ObjectEnabledChanged;

			ApiKeys.OnAdded -= ObjectAdded;
			ApiKeys.OnRemoved -= ObjectRemoved;
			ApiKeys.OnChanged -= ObjectChanged;
			ApiKeys.OnEnabledChanged -= ObjectEnabledChanged;

			while (_writeInProgress)
			{
				Thread.Sleep(500);
			}
			_sessions.Dispose();
		}
	}
}
