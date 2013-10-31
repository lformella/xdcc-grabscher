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

namespace XG.DB
{
	public class Dao : IDisposable
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		ISession _session;

		readonly int _version = 1;

		public Dao()
		{
			var cfg = new Configuration();
			cfg.Configure();
			cfg.AddAssembly(typeof(Dao).Assembly);

			// mono needs a special driver wrapper
#if __MonoCS__
			cfg.Properties["connection.driver_class"] = "XG.DB.MonoSqliteDriver, XG.DB";
#else
			cfg.Properties["connection.driver_class"] = "NHibernate.Driver.SQLite20Driver";
#endif

			string db = Config.Properties.Settings.Default.GetAppDataPath() + "xgobjects.db";
			cfg.Properties["connection.connection_string"] = "Data Source=" + db + ";Version=3";

			bool insertVersion = false;
			if (!System.IO.File.Exists(db))
			{
				new SchemaExport(cfg).Execute(false, true, false);
				insertVersion = true;
			}

			var sessions = cfg.BuildSessionFactory();
			_session = sessions.OpenSession();

			if (insertVersion)
			{
				_session.Save (new Domain.Version { Number = _version });
			}
			else
			{
				var version = _session.CreateQuery("FROM Version").List<Domain.Version>().OrderByDescending(v => v.Number).First();
				UpdateDatabase (version.Number, _version);
			}
		}

		public Servers Servers()
		{
			var servers = new Servers();
			
			try
			{
				var list = _session.CreateQuery("FROM Server").List<Server>();
				foreach (var server in list)
				{
					servers.Add(server);
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("Servers() cant load ", ex);
			}

			servers.OnAdded += ObjectAdded;
			servers.OnRemoved += ObjectRemoved;
			return servers;
		}

		public Files Files()
		{
			var files = new Files();

			try
			{
				var list = _session.CreateQuery("FROM File").List<File>();
				foreach (var file in list)
				{
					files.Add(file);
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("Files() cant load ", ex);
			}

			files.OnAdded += ObjectAdded;
			files.OnRemoved += ObjectRemoved;
			return files;
		}

		public Searches Searches()
		{
			var searches = new Searches();
			
			try
			{
				var list = _session.CreateQuery("FROM Search").List<Search>();
				foreach (var search in list)
				{
					searches.Add(search);
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("Searches() cant load ", ex);
			}

			searches.OnAdded += ObjectAdded;
			searches.OnRemoved += ObjectRemoved;
			return searches;
		}

		void ObjectAdded(object sender, EventArgs<AObject, AObject> eventArgs)
		{
			if (eventArgs.Value2 is Server || eventArgs.Value2 is File || eventArgs.Value2 is Search)
			{
				_session.Save(eventArgs.Value2);
				_session.Flush();
			}
		}

		void ObjectRemoved(object sender, EventArgs<AObject, AObject> eventArgs)
		{
			if (eventArgs.Value2 is Server || eventArgs.Value2 is File || eventArgs.Value2 is Search)
			{
				_session.Delete(eventArgs.Value2);
				_session.Flush();
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
			_session.Flush();
			_session.Close();
		}
	}
}
