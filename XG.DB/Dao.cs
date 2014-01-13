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

		readonly ISession _session;
		readonly int SecondsToSleep = 10;
		DateTime _lastFlush;

		readonly int _version = 1;

		public Servers Servers { get; private set; }
		public Files Files { get; private set; }
		public Searches Searches { get; private set; }
		public ApiKeys ApiKeys { get; private set; }

		public Dao()
		{
			// load assembly by creating new object
			// otherwise there will occur weird assembly loading errors
#if __MonoCS__
			new SqliteConnection();
#else
			new SQLiteConnection();
#endif
			bool insertVersion = false;

			var cfg = new Configuration();

			try
			{
				cfg.Configure();
				cfg.AddAssembly(typeof(Dao).Assembly);
			}
			catch (Exception ex)
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

				cfg.AddAssembly(typeof(Dao).Assembly);
				if (!System.IO.File.Exists(db))
				{
					new SchemaExport(cfg).Execute(false, true, false);
					insertVersion = true;
				}
			}

			var sessions = cfg.BuildSessionFactory();
			_session = sessions.OpenSession(new TrackingNumberInterceptor());
			_session.FlushMode = FlushMode.Never;

			CheckIfDatabaseNeedsUpdate(insertVersion);
			LoadObjects();
		}

		void CheckIfDatabaseNeedsUpdate(bool insertVersion)
		{
			var version = new Domain.Version { Number = _version };

			if (insertVersion)
			{
				_session.Save(new Domain.Version { Number = _version });
				_session.Flush();
			}
			else
			{
				version = _session.CreateQuery("FROM Version").List<Domain.Version>().OrderByDescending(v => v.Number).First();
			}

			UpdateDatabase(version.Number, _version);
		}

		private void LoadObjects()
		{
			Servers = new Servers();
			Files = new Files();
			Searches = new Searches();
			ApiKeys = new ApiKeys();

			var servers = _session.CreateQuery("FROM Server").List<Server>();
			foreach (var server in servers)
			{
				Servers.Add(server);
			}

			var files = _session.CreateQuery("FROM File").List<File>();
			foreach (var file in files)
			{
				Files.Add(file);
			}

			var searches = _session.CreateQuery("FROM Search").List<Search>();
			foreach (var search in searches)
			{
				Searches.Add(search);
			}

			var apiKeys = _session.CreateQuery("FROM ApiKey").List<ApiKey>();
			foreach (var apiKey in apiKeys)
			{
				ApiKeys.Add(apiKey);
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

			try
			{
				_session.Save(eventArgs.Value2);
			}
			catch (Exception ex)
			{
				Log.Fatal("cant save object " + eventArgs.Value2, ex);
			}
			finally
			{
				TryFlush();
			}
		}

		void ObjectRemoved(object sender, EventArgs<AObject, AObject> eventArgs)
		{
			if (eventArgs.Value1 != Servers && eventArgs.Value1 != Files && eventArgs.Value1 != Searches && eventArgs.Value1 != ApiKeys)
			{
				return;
			}

			try
			{
				_session.Delete(eventArgs.Value2);
			}
			catch (Exception ex)
			{
				Log.Fatal("cant remove object " + eventArgs.Value2, ex);
			}
			finally
			{
				TryFlush();
			}
		}

		void ObjectChanged(object sender, EventArgs<AObject, string[]> eventArgs)
		{
			TryFlush();
		}

		void ObjectEnabledChanged(object sender, EventArgs<AObject> eventArgs)
		{
			TryFlush();
		}

		void TryFlush ()
		{
			if (_lastFlush.AddSeconds(SecondsToSleep) < DateTime.Now)
			{
				_lastFlush = DateTime.Now;

				lock (Servers) lock(Files) lock(Searches) lock(ApiKeys)
				{
					try
					{
						_session.Flush();
					}
					catch (InvalidOperationException)
					{
						// this is ok
					}
					catch (Exception ex)
					{
						Log.Fatal("TryFlush()", ex);
					}
				}
			}
		}

		void UpdateDatabase(int aFrom, int aTo)
		{
			if (aFrom == aTo)
			{
				return;
			}

			// add in next version...
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

			_session.Close();
		}
	}
}
