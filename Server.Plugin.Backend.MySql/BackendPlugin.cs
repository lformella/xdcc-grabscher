// 
//  BackendPlugin.cs
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
using System.Linq;
using System.Reflection;

using log4net;

using MySql.Data.MySqlClient;

using XG.Core;

namespace XG.Server.Plugin.Backend.MySql
{
	public class BackendPlugin : ABackendPlugin
	{
		#region VARIABLES
		
		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		MySqlConnection _dbConnection;
		object _locked = new object ();

		#endregion

		#region FUNCTIONS

		public BackendPlugin ()
		{
			string connectionString = "Server=" + Settings.Instance.MySqlBackendServer + ";Port=" + Settings.Instance.MySqlBackendPort + ";Database=" + Settings.Instance.MySqlBackendDatabase + ";User ID=" + Settings.Instance.MySqlBackendUser + ";Password=" + Settings.Instance.MySqlBackendPassword + ";Pooling=false";
			try
			{
				_dbConnection = new MySqlConnection (connectionString);
				_dbConnection.Open ();

				// cleanup database
				ExecuteNonQuery ("UPDATE servers SET connected = 0;", null);
				ExecuteNonQuery ("UPDATE channels SET connected = 0;", null);
				ExecuteNonQuery ("UPDATE bots SET connected = 0;", null);
				ExecuteNonQuery ("UPDATE packets SET connected = 0;", null);
			}
			catch (Exception ex)
			{
				_log.Fatal("MySqlBackend(" + connectionString + ") ", ex);
				throw ex;
			}
		}

		#endregion

		#region AWorker

		protected override void StopRun ()
		{
			try
			{
				_dbConnection.Close ();
			}
			catch (Exception ex)
			{
				_log.Fatal("CloseClient() ", ex);
			}
		}

		#endregion

		#region ABackendPlugin

		public override Core.Servers LoadServers ()
		{
			Core.Servers _servers = new Core.Servers();

			#region DUMP DATABASE

			Dictionary<string, object> dic = new Dictionary<string, object>();
			dic.Add ("guid", Guid.Empty);
			foreach(Core.Server serv in ExecuteQuery<Core.Server>("SELECT * FROM servers;", null))
			{
				serv.Connected = false;
				_servers.Add(serv);

				dic["guid"] = serv.Guid.ToString ();
				foreach(Channel chan in ExecuteQuery<Channel>("SELECT * FROM channels WHERE ParentGuid = @guid;", dic))
				{
					chan.Connected = false;
					serv.AddChannel(chan);

					dic["guid"] = chan.Guid.ToString ();
					foreach(Bot bot in ExecuteQuery<Bot>("SELECT * FROM bots WHERE ParentGuid = @guid;", dic))
					{
						bot.Connected = false;
						bot.State = Bot.States.Idle;
						chan.AddBot(bot);

						dic["guid"] = bot.Guid.ToString ();
						foreach(Packet pack in ExecuteQuery<Packet>("SELECT * FROM packets WHERE ParentGuid = @guid;", dic))
						{
							pack.Connected = false;
							bot.AddPacket(pack);
						}
					}
				}
			}

			#endregion

			#region import routine

			Importer importer = new Importer(_servers);

			importer.ObjectAddedEvent += new ObjectsDelegate (ObjectAdded);

			importer.Import("./import");

			importer.ObjectAddedEvent -= new ObjectsDelegate (ObjectAdded);

			#endregion

			return _servers;
		}

		public override Files LoadFiles ()
		{
			Files _files =  new Files();

			/*foreach(File file in ExecuteQuery<File>("SELECT * FROM files;", null))
			{
				_files.Add(file);
			}*/

			return _files;
		}

		public override Objects LoadSearches ()
		{
			Objects _searches =  new Objects();

			/*foreach(Core.Object obj in ExecuteQuery<Core.Object>("SELECT * FROM searches;", null))
			{
				_searches.Add(obj);
			}*/

			return _searches;
		}
		
		public override Snapshots LoadStatistics ()
		{
			Snapshots _snapshots =  new Snapshots();

			foreach(Snapshot snapshot in ExecuteQuery<Snapshot>("SELECT * FROM snapshots;", null))
			{
				var coreSnapshot = new Core.Snapshot();

				var properties = snapshot.GetType().GetProperties();
				foreach(var prop in properties)
				{
					var type = (SnapshotValue)Enum.Parse(typeof(SnapshotValue), prop.Name);
					coreSnapshot.Set(type, (Int64)prop.GetValue(snapshot, null));
				}

				_snapshots.Add(coreSnapshot);
			}

			return _snapshots;
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded (AObject aParentObj, AObject aObj)
		{
			string table = Table4Object (aObj);
			Dictionary<string, object> dic = Object2Dic (aObj);

			if (table != "")
			{
				string values1 = "";
				string values2 = "";
				foreach (var kcp in dic)
				{
					if (values1 != "")
					{
						values1 += ", ";
						values2 += ", ";
					}
					values1 += kcp.Key;
					values2 += "@" + kcp.Key;
				}

				ExecuteNonQuery ("INSERT INTO " + table + " (" + values1 + ") VALUES (" + values2 + ");", dic);
			}
		}

		protected override void ObjectChanged (AObject aObj)
		{
			string table = Table4Object (aObj);
			Dictionary<string, object> dic = Object2Dic (aObj);

			if (table != "")
			{
				string values1 = "";
				foreach (var kcp in dic)
				{
					if (values1 != "")
					{
						values1 += ", ";
					}
					values1 += kcp.Key + " = @" + kcp.Key;
				}

				ExecuteNonQuery ("UPDATE " + table + " SET " + values1 + " WHERE Guid = @Guid;", dic);
			}
		}

		protected override void ObjectRemoved (AObject aParentObj, AObject aObj)
		{
			string table = Table4Object (aObj);
			Dictionary<string, object> dic = new Dictionary<string, object> ();

			if (table != "")
			{
				dic.Add ("guid", aObj.Guid.ToString ());
				ExecuteNonQuery ("DELETE FROM " + table + " WHERE Guid = @Guid;", dic);
			}
		}
		
		protected override void SnapshotAdded(Core.Snapshot aSnap)
		{
			Dictionary<string, object> dic = new Dictionary<string, object>();

			for(int a = 0; a <= 24; a++)
			{
				string name = Enum.GetName(typeof(SnapshotValue), (SnapshotValue)a);
				dic.Add(name, aSnap.Get((SnapshotValue)a));
			}

			string values1 = "";
			string values2 = "";
			foreach (var kcp in dic)
			{
				if (values1 != "")
				{
					values1 += ", ";
					values2 += ", ";
				}
				values1 += kcp.Key;
				values2 += "@" + kcp.Key;
			}

			ExecuteNonQuery ("INSERT INTO snapshots (" + values1 + ") VALUES (" + values2 + ");", dic);
		}

		#endregion

		#region HELPER

		Dictionary<string, object> Object2Dic(object aObj)
		{
			Dictionary<string, object> dic = new Dictionary<string, object> ();

			var properties = aObj.GetType().GetProperties();
			foreach(var prop in properties)
			{
				if (Attribute.IsDefined(prop, typeof(MySqlAttribute)))
				{
					object value = prop.GetValue(aObj, null);
					if (prop.PropertyType == typeof(Guid))
					{
						value = value.ToString();
					}
					else if (prop.PropertyType == typeof(DateTime))
					{
						value = ((DateTime)value).ToTimestamp();
					}

					dic[prop.Name] = value;
				}
			}
			return dic;
		}

		T Dic2Object<T>(Dictionary<string, object> aDic)
		{
			var obj = (T)Activator.CreateInstance(typeof(T));
			var properties = typeof(T).GetProperties();
			foreach(var prop in properties)
			{
				if (Attribute.IsDefined(prop, typeof(MySqlAttribute)) && aDic.ContainsKey(prop.Name))
				{
					object value = aDic[prop.Name];
					if (prop.PropertyType == typeof(Guid))
					{
						if (value != "" && value.GetType() != typeof(DBNull))
						{
							value = new Guid(value.ToString());
						}
						else
						{
							value = Guid.Empty;
						}
					}
					else if (prop.PropertyType == typeof(DateTime))
					{
						value = ((Int64)value).ToDate();
					}
					else if (prop.PropertyType == typeof(Bot.States))
					{
						try
						{
							value = (Bot.States)value;
						}
						catch (InvalidCastException)
						{
							value = Bot.States.Idle;
						}
					}

					try
					{
						prop.SetValue(obj, value, null);
					}
					catch (Exception ex)
					{
						if (ex.InnerException == null || ex.InnerException.GetType() != typeof(NotSupportedException))
						{
							throw ex;
						}
					}
				}
			}
			return obj;
		}

		void ExecuteNonQuery (string aSql, Dictionary<string, object> aDic)
		{
			lock (_locked)
			{
				MySqlCommand cmd = new MySqlCommand (aSql, _dbConnection);
				using (cmd)
				{
					if (aDic != null)
					{
						foreach (var kcp in aDic)
						{
							cmd.Parameters.AddWithValue ("@" + kcp.Key, kcp.Value);
						}
					}
#if !UNSAFE
					try
					{
#endif
						cmd.ExecuteNonQuery ();
#if !UNSAFE
					}
					catch (MySqlException ex)
					{
						_log.Fatal("ExecuteQuery(" + ex.Number + ") '" + BuildSqlString (aSql, aDic) + "' ", ex);
					}
					catch (InvalidOperationException ex)
					{
						_log.Fatal("ExecuteQuery() '" + BuildSqlString (aSql, aDic) + "' ", ex);
						_log.Warn("ExecuteQuery() : stopping server plugin!");
						Stop ();
					}
					catch (Exception ex)
					{
						_log.Fatal("ExecuteQuery() '" + BuildSqlString (aSql, aDic) + "' ", ex);
					}
#endif
				}
			}
		}

		List<T> ExecuteQuery<T> (string aSql, Dictionary<string, object> aDic)
		{
			List<T> list = new List<T>();

			lock (_locked)
			{
				MySqlCommand cmd = new MySqlCommand (aSql, _dbConnection);
				using (cmd)
				{
					if (aDic != null)
					{
						foreach (var kcp in aDic)
						{
							cmd.Parameters.AddWithValue ("@" + kcp.Key, kcp.Value);
						}
					}
#if !UNSAFE
					try
					{
#endif
						MySqlDataReader myReader = cmd.ExecuteReader();
						while (myReader.Read())
						{
							Dictionary<string, object> dic = new Dictionary<string, object> ();
							for (int col = 0; col < myReader.FieldCount; col++)
							{
								dic.Add (myReader.GetName(col), myReader.GetValue(col));
							}
							T obj = Dic2Object<T>(dic);
							if (obj != null)
							{
								list.Add(obj);
							}
						}
						myReader.Close();
#if !UNSAFE
					}
					catch (MySqlException ex)
					{
						_log.Fatal("ExecuteReader(" + ex.Number + ") '" + BuildSqlString (aSql, aDic) + "' ", ex);
					}
					catch (InvalidOperationException ex)
					{
						_log.Fatal("ExecuteReader() '" + BuildSqlString (aSql, aDic) + "' ", ex);
						_log.Warn("ExecuteReader() : stopping server plugin!");
						Stop ();
					}
					catch (Exception ex)
					{
						_log.Fatal("ExecuteReader() '" + BuildSqlString (aSql, aDic) + "' ", ex);
					}
#endif
				}
			}

			return list;
		}

		string Table4Object(AObject aObj)
		{
			if (aObj is Core.Server)
			{
				return "servers";
			}
			else if (aObj is Channel)
			{
				return "channels";
			}
			else if (aObj is Bot)
			{
				return "bots";
			}
			else if (aObj is Packet)
			{
				return "packets";
			}
			return "";
		}

		string BuildSqlString (string aSql, Dictionary<string, object> aDic)
		{
			if (aDic != null)
			{
				foreach (var kcp in aDic)
				{
					aSql = aSql.Replace ("@" + kcp.Key, "" + kcp.Value);
				}
			}
			return aSql;
		}

		#endregion
	}
}
