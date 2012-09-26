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
using System.Threading;

using log4net;

using MySql.Data.MySqlClient;

using XG.Core;

namespace XG.Server.Plugin.Backend.MySql
{
	public class BackendPlugin : ABackendPlugin
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(BackendPlugin));

		MySqlConnection _dbConnection;
		Thread _serverThread;
		object _locked = new object ();

		#endregion

		public BackendPlugin ()
		{
			string connectionString = "Server=" + Settings.Instance.MySqlBackendServer + ";Database=" + Settings.Instance.MySqlBackendDatabase + ";User ID=" + Settings.Instance.MySqlBackendUser + ";Password=" + Settings.Instance.MySqlBackendPassword + ";Pooling=false";
			try
			{
				_dbConnection = new MySqlConnection (connectionString);
				_dbConnection.Open ();

				// cleanup database
				ExecuteNonQuery ("UPDATE server SET connected = 0;", null);
				ExecuteNonQuery ("UPDATE channel SET connected = 0;", null);
				ExecuteNonQuery ("UPDATE bot SET connected = 0;", null);
				ExecuteNonQuery ("UPDATE packet SET connected = 0;", null);
			}
			catch (Exception ex)
			{
				_log.Fatal("MySqlBackend(" + connectionString + ") ", ex);
				throw ex;
			}
		}

		#region ABackendPlugin

		public override Core.Servers LoadServers ()
		{
			Core.Servers _servers = new Core.Servers();

			#region DUMP DATABASE

			Dictionary<string, object> dic = new Dictionary<string, object>();
			dic.Add ("guid", Guid.Empty);
			foreach(Core.Server serv in ExecuteQuery ("SELECT * FROM server;", null, typeof(Core.Server)))
			{
				_servers.Add(serv);

				dic["guid"] = serv.Guid.ToString ();
				foreach(Channel chan in ExecuteQuery ("SELECT * FROM channel WHERE ParentGuid = @guid;", dic, typeof(Channel)))
				{
					serv.AddChannel(chan);

					dic["guid"] = chan.Guid.ToString ();
					foreach(Bot bot in ExecuteQuery ("SELECT * FROM bot WHERE ParentGuid = @guid;", dic, typeof(Bot)))
					{
						chan.AddBot(bot);

						dic["guid"] = bot.Guid.ToString ();
						foreach(Packet pack in ExecuteQuery ("SELECT * FROM packet WHERE ParentGuid = @guid;", dic, typeof(Packet)))
						{
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
			return new Files();
		}

		public override Objects LoadSearches ()
		{
			return new Objects();
		}

		#endregion

		#region RUN STOP
		
		public override void Start ()
		{
			// start the server thread
			_serverThread = new Thread (new ThreadStart (OpenClient));
			_serverThread.Start ();
		}
		
		public override void Stop ()
		{
			CloseClient ();
			_serverThread.Abort ();
		}
		
		#endregion

		#region SERVER

		void OpenClient ()
		{
		}

		void CloseClient ()
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

		#region EVENTHANDLER

		protected override void ObjectAdded (AObject aParentObj, AObject aObj)
		{
			string table = Table4Object (aObj);
			Dictionary<string, object> dic = Object2Dic (aObj);

			if (table != "")
			{
				dic.Add ("guid", aObj.Guid.ToString ());

				string values1 = "";
				string values2 = "";
				foreach (KeyValuePair<string, object> kcp in dic)
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
				foreach (KeyValuePair<string, object> kcp in dic)
				{
					if (values1 != "")
					{
						values1 += ", ";
					}
					values1 += kcp.Key + " = @" + kcp.Key;
				}

				dic.Add ("guid", aObj.Guid.ToString ());
				ExecuteNonQuery ("UPDATE " + table + " SET " + values1 + " WHERE Guid = @guid;", dic);
			}
		}

		protected override void ObjectRemoved (AObject aParentObj, AObject aObj)
		{
			string table = Table4Object (aObj);
			Dictionary<string, object> dic = new Dictionary<string, object> ();

			if (table != "")
			{
				dic.Add ("guid", aObj.Guid.ToString ());
				ExecuteNonQuery ("DELETE FROM " + table + " WHERE Guid = @guid;", dic);
			}
		}

		#endregion

		#region HELPER

		protected Dictionary<string, object> Object2Dic (AObject aObj)
		{
			Dictionary<string, object> dic = new Dictionary<string, object> ();
			dic.Add ("Name", aObj.Name);
			dic.Add ("Connected", aObj.Connected);
			dic.Add ("Enabled", aObj.Enabled);
			dic.Add ("LastModified", Date2Timestamp (aObj.EnabledTime));

			if (aObj is Core.Server)
			{
				Core.Server obj = (Core.Server)aObj;
				dic.Add ("Port", obj.Port);
				dic.Add ("ErrorCode", obj.ErrorCode);
				dic.Add ("ChannelCount", obj.Channels.Count());
			}
			else if (aObj is Channel)
			{
				Channel obj = (Channel)aObj;
				dic.Add ("ParentGuid", obj.ParentGuid);
				dic.Add ("ErrorCode", obj.ErrorCode);
				dic.Add ("BotCount", obj.Bots.Count());
			}
			else if (aObj is Bot)
			{
				Bot obj = (Bot)aObj;
				dic.Add ("ParentGuid", obj.ParentGuid);
				dic.Add ("Bot.BotState", obj.State);
				dic.Add ("InfoQueueCurrent", obj.InfoQueueCurrent);
				dic.Add ("InfoQueueTotal", obj.InfoQueueTotal);
				dic.Add ("InfoSlotCurrent", obj.InfoSlotCurrent);
				dic.Add ("InfoSlotTotal", obj.InfoSlotTotal);
				dic.Add ("InfoSpeedCurrent", obj.InfoSpeedCurrent);
				dic.Add ("InfoSpeedMax", obj.InfoSpeedMax);
				dic.Add ("LastContact", Date2Timestamp (obj.LastContact));
				dic.Add ("LastMessage", obj.LastMessage);
			}
			else if (aObj is Packet)
			{
				Packet obj = (Packet)aObj;
				dic.Add ("ParentGuid", obj.ParentGuid);
				dic.Add ("Id", obj.Id);
				dic.Add ("LastUpdated", Date2Timestamp (obj.LastUpdated));
				dic.Add ("LastMentioned", Date2Timestamp (obj.LastMentioned));
				dic.Add ("Size", obj.Size);
			}

			return dic;
		}

		protected AObject Dic2Object (Dictionary<string, object> aDic, Type aType)
		{
			if (aType == typeof(Core.Server))
			{
				Core.Server serv = new Core.Server();
				serv.Guid = new Guid((string)aDic ["Guid"]);
				serv.Name = (string)aDic ["Name"];
				serv.Connected = false;
				serv.Enabled = (bool)aDic ["Enabled"];
				serv.Port = (int)aDic ["Port"];
				return serv;
			}
			else if (aType == typeof(Channel))
			{
				Channel chan = new Channel();
				chan.Guid = new Guid((string)aDic ["Guid"]);
				chan.Name = (string)aDic ["Name"];
				chan.Connected = false;
				chan.Enabled = (bool)aDic ["Enabled"];
				chan.ErrorCode = (int)aDic ["ErrorCode"];
				return chan;
			}
			else if (aType == typeof(Bot))
			{
				Bot bot = new Bot();
				bot.Guid = new Guid((string)aDic ["Guid"]);
				bot.Name = (string)aDic ["Name"];
				bot.Connected = false;
				bot.Enabled = (bool)aDic ["Enabled"];
				bot.State = Bot.States.Idle;
				bot.InfoQueueCurrent = (int)aDic ["InfoQueueCurrent"];
				bot.InfoQueueTotal = (int)aDic ["InfoQueueTotal"];
				bot.InfoSlotCurrent = (int)aDic ["InfoSlotCurrent"];
				bot.InfoSlotTotal = (int)aDic ["InfoSlotTotal"];
				bot.InfoSpeedCurrent = (double)aDic ["InfoSpeedCurrent"];
				bot.InfoSpeedMax = (double)aDic ["InfoSpeedMax"];
				bot.LastMessage = (string)aDic ["LastMessage"];
				bot.LastContact = Timestamp2Date ((Int64)aDic ["LastContact"]);
				return bot;
			}
			else if (aType == typeof(Packet))
			{
				Packet pack = new Packet();
				pack.Guid = new Guid((string)aDic ["Guid"]);
				pack.Name = (string)aDic ["Name"];
				pack.Connected = false;
				pack.Enabled = (bool)aDic ["Enabled"];
				pack.Name = (string)aDic ["Name"];
				pack.Id = (int)aDic ["Id"];
				pack.LastUpdated = Timestamp2Date ((Int64)aDic ["LastUpdated"]);
				pack.LastMentioned = Timestamp2Date ((Int64)aDic ["LastMentioned"]);
				pack.Size = (Int64)aDic ["Size"];
				return pack;
			}

			return null;
		}

		protected void ExecuteNonQuery (string aSql, Dictionary<string, object> aDic)
		{
			//aSql = "START TRANSACTION;" + aSql + "COMMIT;";
			lock (_locked)
			{
				MySqlCommand cmd = new MySqlCommand (aSql, _dbConnection);
				using (cmd)
				{
					if (aDic != null)
					{
						foreach (KeyValuePair<string, object> kcp in aDic)
						{
							cmd.Parameters.AddWithValue ("@" + kcp.Key, kcp.Value);
						}
					}
					try
					{
						cmd.ExecuteNonQuery ();
					}
					catch (MySqlException ex)
					{
						_log.Fatal("ExecuteQuery(" + ex.Number + ") '" + SqlString (aSql, aDic) + "' ", ex);
					}
					catch (InvalidOperationException ex)
					{
						_log.Fatal("ExecuteQuery() '" + SqlString (aSql, aDic) + "' ", ex);
						_log.Warn("ExecuteQuery() : stopping server plugin!");
						Stop ();
					}
					catch (Exception ex)
					{
						_log.Fatal("ExecuteQuery() '" + SqlString (aSql, aDic) + "' ", ex);
					}
				}
			}
		}

		protected List<AObject> ExecuteQuery (string aSql, Dictionary<string, object> aDic, Type aType)
		{
			List<AObject> list = new List<AObject> ();

			//aSql = "START TRANSACTION;" + aSql + "COMMIT;";
			lock (_locked)
			{
				MySqlCommand cmd = new MySqlCommand (aSql, _dbConnection);
				using (cmd)
				{
					if (aDic != null)
					{
						foreach (KeyValuePair<string, object> kcp in aDic)
						{
							cmd.Parameters.AddWithValue ("@" + kcp.Key, kcp.Value);
						}
					}
					try
					{
						MySqlDataReader myReader = cmd.ExecuteReader ();
						while (myReader.Read())
						{
							Dictionary<string, object> dic = new Dictionary<string, object> ();
							for (int col = 0; col < myReader.FieldCount; col++)
							{
								dic.Add (myReader.GetName (col), myReader.GetValue (col));
							}
							AObject obj = Dic2Object (dic, aType);
							if(obj != null)
							list.Add (obj);
						}
						myReader.Close();
					}
					catch (MySqlException ex)
					{
						_log.Fatal("ExecuteReader(" + ex.Number + ") '" + SqlString (aSql, aDic) + "' ", ex);
					}
					catch (InvalidOperationException ex)
					{
						_log.Fatal("ExecuteReader() '" + SqlString (aSql, aDic) + "' ", ex);
						_log.Warn("ExecuteReader() : stopping server plugin!");
						Stop ();
					}
					catch (Exception ex)
					{
						_log.Fatal("ExecuteReader() '" + SqlString (aSql, aDic) + "' ", ex);
					}
				}
			}

			return list;
		}

		protected Int64 Date2Timestamp (DateTime aDate)
		{
			DateTime date = new DateTime (1970, 1, 1);
			TimeSpan ts = new TimeSpan (aDate.Ticks - date.Ticks);
			return (Convert.ToInt64 (ts.TotalSeconds));
		}

		protected DateTime Timestamp2Date (Int64 aTimestamp)
		{
			DateTime date = new DateTime (1970, 1, 1);
			date.AddSeconds (aTimestamp);
			return date;
		}

		protected string Table4Object (AObject aObj)
		{
			if (aObj is Core.Server)
			{
				return "server";
			}
			else if (aObj is Channel)
			{
				return "channel";
			}
			else if (aObj is Bot)
			{
				return "bot";
			}
			else if (aObj is Packet)
			{
				return "packet";
			}
			return "";
		}

		protected string SqlString (string aSql, Dictionary<string, object> aDic)
		{
			if (aDic != null)
			{
				foreach (KeyValuePair<string, object> kcp in aDic)
				{
					aSql = aSql.Replace ("@" + kcp.Key, "" + kcp.Value);
				}
			}
			return aSql;
		}

		#endregion
	}
}
