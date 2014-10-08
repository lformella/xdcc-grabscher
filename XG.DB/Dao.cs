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
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Config.Encoding;
using Db4objects.Db4o.Ext;
using Db4objects.Db4o.TA;
using XG.Config.Properties;
using XG.Extensions;
using XG.Model.Domain;
using XG.Plugin;

namespace XG.DB
{
	public class Dao : APlugin
	{
		#region VARIABLES

		IObjectContainer _db;

		readonly object _lock = new object();

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			string dbPath = Settings.Default.GetAppDataPath() + "xgobjects.db4o";

			bool loadFromSqlite = false;
			if (!System.IO.File.Exists(dbPath) && System.IO.File.Exists(Settings.Default.GetAppDataPath() + "xgobjects.db"))
			{
				loadFromSqlite = true;
			}

			IEmbeddedConfiguration config = Db4oEmbedded.NewConfiguration();
			config.Common.StringEncoding = StringEncodings.Utf8();
			config.Common.Add(new TransparentActivationSupport());
			config.Common.Add(new TransparentPersistenceSupport());

			/*if (System.IO.File.Exists(dbPath))
			{
				DefragmentConfig defragmentConfig = new DefragmentConfig(dbPath);
				defragmentConfig.Db4oConfig(config);
				Defragment.Defrag(defragmentConfig);
			}*/

			_db = Db4oEmbedded.OpenFile(config, dbPath);

			if (loadFromSqlite)
			{
				var sqliteConverter = new SqliteConverter();
				sqliteConverter.Load();

				_db.Store(sqliteConverter.Servers);
				_db.Store(sqliteConverter.Files);
				_db.Store(sqliteConverter.Searches);
				_db.Store(sqliteConverter.ApiKeys);
				_db.Commit();
			}

			Load();
		}

		protected override void StopRun()
		{
			Servers = null;
			Files = null;
			Searches = null;
			ApiKeys = null;

			_db.Commit();
			_db.Close();
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void ObjectChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			TryCommit();
		}

		protected override void ObjectEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void FileAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void FileRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void FileChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			TryCommit();
		}

		protected override void SearchAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void SearchRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void ApiKeyChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			TryCommit();
		}

		protected override void ApiKeyAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void ApiKeyRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			TryCommit();
		}

		protected override void ApiKeyEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			TryCommit();
		}

		#endregion

		#region FUNCTIONS

		void Load()
		{
			try
			{
				Servers = _db.Query<Servers>(typeof(Servers))[0];
			}
			catch (InvalidOperationException) {}
			catch (Db4oRecoverableException) {}
			if (Servers == null)
			{
				Servers = new Servers();
				_db.Store(Servers);
			}

			try
			{
				Files = _db.Query<Files>(typeof(Files))[0];
			}
			catch (InvalidOperationException) {}
			catch (Db4oRecoverableException) {}
			if (Files == null)
			{
				Files = new Files();
				_db.Store(Files);
			}

			try
			{
				Searches = _db.Query<Searches>(typeof(Searches))[0];
			}
			catch (InvalidOperationException) {}
			catch (Db4oRecoverableException) {}
			if (Searches == null)
			{
				Searches = new Searches();
				_db.Store(Searches);
			}

			try
			{
				ApiKeys = _db.Query<ApiKeys>(typeof(ApiKeys))[0];
			}
			catch (InvalidOperationException) {}
			catch (Db4oRecoverableException) {}
			if (ApiKeys == null)
			{
				ApiKeys = new ApiKeys();
				_db.Store(ApiKeys);
			}

			TryCommit();
		}

		void TryCommit()
		{
			lock(_lock)
			{
				try
				{
					_db.Commit();
				}
				catch (DatabaseClosedException) {}
			}
		}

		#endregion
	}
}	
