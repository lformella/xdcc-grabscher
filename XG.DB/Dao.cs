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
using XG.Plugin;

#if __MonoCS__
using Mono.Data.Sqlite;
#else
using System.Data.SQLite;
#endif

namespace XG.DB
{
	public class Dao : APlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		ISessionFactory _sessions;

		readonly int _version = 1;

		bool _writeInProgress;
		DateTime _lastSave;
		readonly List<AObject> _objectsAdded = new List<AObject>();
		readonly List<AObject> _objectsChanged = new List<AObject>();
		readonly List<AObject> _objectsRemoved = new List<AObject>();

		bool _writeNecessary;
		internal bool WriteNecessary
		{
			get
			{
				if (_writeInProgress)
				{
					return false;
				}
				if (_objectsAdded.Count == 0 && _objectsChanged.Count == 0 && _objectsRemoved.Count == 0)
				{
					return false;
				}
				if (_writeNecessary)
				{
					return true;
				}
				if (_lastSave.AddSeconds(300) > DateTime.Now)
				{
					return false;
				}
				return true;
			}
		}

		#endregion

		#region AWorker

		protected override void StartRun()
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

			// create sync job
			AddRepeatingJob(typeof(DaoSync), "DaoSync", "Dao", 1, 
				new JobItem("Dao", this));
		}

		protected override void StopRun()
		{
			Servers = null;
			Files = null;
			Searches = null;
			ApiKeys = null;

			while (_writeInProgress)
			{
				Thread.Sleep(500);
			}
			_sessions.Dispose();
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			if (aEventArgs.Value1 != Servers && aEventArgs.Value1 != Files && aEventArgs.Value1 != Searches && aEventArgs.Value1 != ApiKeys)
			{
				return;
			}
			if (_objectsChanged.Contains(aEventArgs.Value2) || _objectsRemoved.Contains(aEventArgs.Value2))
			{
				return;
			}

			TryAddToList(_objectsAdded, aEventArgs.Value2);
			UpdateWriteNecesary(aEventArgs.Value2);
		}

		protected override void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			if (aEventArgs.Value1 != Servers && aEventArgs.Value1 != Files && aEventArgs.Value1 != Searches && aEventArgs.Value1 != ApiKeys)
			{
				return;
			}

			TryAddToList(_objectsRemoved, aEventArgs.Value2);
			TryRemoveFromList(_objectsAdded, aEventArgs.Value2);
			TryRemoveFromList(_objectsChanged, aEventArgs.Value2);
			UpdateWriteNecesary(aEventArgs.Value2);
		}

		protected override void ObjectChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			if (_objectsAdded.Contains(aEventArgs.Value1) || _objectsRemoved.Contains(aEventArgs.Value1))
			{
				return;
			}

			TryAddToList(_objectsChanged, aEventArgs.Value1);
		}

		protected override void ObjectEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			TryAddToList(_objectsChanged, aEventArgs.Value1);
			_writeNecessary = true;
		}

		#endregion

		#region FUNCTIONS

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

		void LoadObjects()
		{
			var _servers = new Servers();
			var _files = new Files();
			var _searches = new Searches();
			var _apiKeys = new ApiKeys();

			using (ISession session = _sessions.OpenSession(new TrackingNumberInterceptor()))
			{
				var servers = session.CreateQuery("FROM Server").List<Server>();
				foreach (var server in servers)
				{
					_servers.Add(server);
				}

				var files = session.CreateQuery("FROM File").List<File>();
				foreach (var file in files)
				{
					_files.Add(file);
				}

				var searches = session.CreateQuery("FROM Search").List<Search>();
				foreach (var search in searches)
				{
					_searches.Add(search);
				}

				var apiKeys = session.CreateQuery("FROM ApiKey").List<ApiKey>();
				foreach (var apiKey in apiKeys)
				{
					_apiKeys.Add(apiKey);
				}
			}

			Servers = _servers;
			Files = _files;
			Searches = _searches;
			ApiKeys = _apiKeys;

			_lastSave = DateTime.Now;
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

		void UpdateWriteNecesary(AObject aObject)
		{
			if (aObject is Server || aObject is Channel || aObject is File || aObject is Search || aObject is ApiKey)
			{
				_writeNecessary = true;
			}
		}

		internal void WriteToDatabase()
		{
			_writeInProgress = true;
			_writeNecessary = false;
			_lastSave = DateTime.Now;

			Log.Info("WriteToDatabase() running ");

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
			Log.Info("WriteToDatabase() ready ");

			GC.Collect();
		}

		#endregion
	}
}	
