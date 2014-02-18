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

#if __MonoCS__
using Mono.Data.Sqlite;
#else
using System.Data.SQLite;
#endif

namespace XG.DB
{
	public sealed class Dao : IDisposable
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		ISessionFactory _sessions;
		readonly int SecondsToSleep = 300;

		readonly int _version = 1;

		public Servers Servers { get; private set; }
		public Files Files { get; private set; }
		public Searches Searches { get; private set; }
		public ApiKeys ApiKeys { get; private set; }

		bool _allowRunning { get; set; }
		DateTime _lastSave;
		List<AObject> _objectsAdded = new List<AObject>();
		List<AObject> _objectsChanged = new List<AObject>();
		List<AObject> _objectsRemoved = new List<AObject>();

		public Dao()
		{
			bool insertVersion = false;

			var cfg = new Configuration();

			try
			{
				cfg.Configure();
				cfg.AddAssembly(typeof(Dao).Assembly);
			}
			catch (HibernateConfigException)
			{
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

				cfg.AddAssembly(typeof(Dao).Assembly);
				if (!System.IO.File.Exists(db))
				{
					new SchemaExport(cfg).Execute(false, true, false);
					insertVersion = true;
				}
			}

			_sessions = cfg.BuildSessionFactory();

			CheckIfDatabaseNeedsUpdate(insertVersion);
			LoadObjects();

			var thread = new Thread(DatabaseSync);
			thread.Name = "DatabaseSync";
			thread.Start();
		}

		void CheckIfDatabaseNeedsUpdate(bool insertVersion)
		{
			var version = new Domain.Version { Number = _version };

			using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
			{
				if (insertVersion)
				{
					session.Save(new Domain.Version { Number = _version });
					session.Flush();
				}
				else
				{
					version = session.CreateQuery("FROM Version").List<Domain.Version>().OrderByDescending(v => v.Number).First();
				}
			}

			UpdateDatabase(version.Number, _version);
		}

		void UpdateDatabase(int aFrom, int aTo)
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

			lock (_objectsAdded)
			{
				if (!_objectsAdded.Contains(eventArgs.Value2))
				{
					_objectsAdded.Add(eventArgs.Value2);
				}
			}

			if (eventArgs.Value2 is Server || eventArgs.Value2 is Channel || eventArgs.Value2 is File || eventArgs.Value2 is Search || eventArgs.Value2 is ApiKey)
			{
				WriteToDatabase();
			}
		}

		void ObjectRemoved(object sender, EventArgs<AObject, AObject> eventArgs)
		{
			if (eventArgs.Value1 != Servers && eventArgs.Value1 != Files && eventArgs.Value1 != Searches && eventArgs.Value1 != ApiKeys)
			{
				return;
			}

			lock (_objectsRemoved)
			{
				if (!_objectsRemoved.Contains(eventArgs.Value2))
				{
					_objectsRemoved.Add(eventArgs.Value2);
				}
			}

			if (eventArgs.Value2 is Server || eventArgs.Value2 is Channel || eventArgs.Value2 is File || eventArgs.Value2 is Search || eventArgs.Value2 is ApiKey)
			{
				WriteToDatabase();
			}
		}

		void ObjectChanged(object sender, EventArgs<AObject, string[]> eventArgs)
		{
			lock (_objectsChanged)
			{
				if (!_objectsChanged.Contains(eventArgs.Value1))
				{
					_objectsChanged.Add(eventArgs.Value1);
				}
			}
		}

		void ObjectEnabledChanged(object sender, EventArgs<AObject> eventArgs)
		{
			lock (_objectsChanged)
			{
				if (!_objectsChanged.Contains(eventArgs.Value1))
				{
					_objectsChanged.Add(eventArgs.Value1);
				}
			}

			WriteToDatabase();
		}

		void DatabaseSync()
		{
			_lastSave = DateTime.Now;
			_allowRunning = true;
			while (_allowRunning)
			{
				if (_lastSave.AddSeconds(SecondsToSleep) < DateTime.Now)
				{
					WriteToDatabase();
				}

				Thread.Sleep(500);
			}
		}

		void WriteToDatabase()
		{
			_lastSave = DateTime.Now;
			AObject currentObj;

			try
			{
				lock (_objectsAdded)
				{
					if (_objectsAdded.Count > 0)
					{
						using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
						{
							foreach (AObject obj in _objectsAdded)
							{
								currentObj = obj;
								session.SaveOrUpdate(obj);
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
							foreach (AObject obj in _objectsChanged)
							{
								currentObj = obj;
								session.SaveOrUpdate(obj);
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
							foreach (AObject obj in _objectsRemoved)
							{
								currentObj = obj;
								session.Delete(obj);
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

			GC.Collect();
		}

		public void Dispose ()
		{
			_allowRunning = false;
			WriteToDatabase();

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

			_sessions.Dispose();
		}
	}
}
