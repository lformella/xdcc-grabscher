//  
//  Copyright (C) 2010 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Threading;
using MySql.Data.MySqlClient;
using XG.Core;

namespace XG.Server.Backend.MySql
{
	public class MySqlBackend : IServerPlugin
	{
		#region VARIABLES

		private ServerRunner myRunner;
		private MySqlConnection myDbConnection;
		private Thread myServerThread;
		private object locked = new object ();

		#endregion

		public MySqlBackend (ServerRunner aParent)
		{
			string connectionString = "Server=" + Settings.Instance.MySqlBackendServer + ";Database=" + Settings.Instance.MySqlBackendDatabase + ";User ID=" + Settings.Instance.MySqlBackendUser + ";Password=" + Settings.Instance.MySqlBackendPassword + ";Pooling=false";
			try
			{
				this.myDbConnection = new MySqlConnection (connectionString);
				this.myDbConnection.Open ();
			}
			catch (Exception ex)
			{
				this.Log ("MySqlBackend(" + connectionString + ") : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);

				// stop it
				this.Stop ();
				return;
			}

			#region DUMP DATABASE

			this.ExecuteNonQuery ("UPDATE server SET connected = 0;", null);
			this.ExecuteNonQuery ("UPDATE channel SET connected = 0;", null);
			this.ExecuteNonQuery ("UPDATE bot SET connected = 0;", null);
			this.ExecuteNonQuery ("UPDATE packet SET connected = 0;", null);

			Dictionary<string, object> dic = new Dictionary<string, object>();
			dic.Add ("guid", Guid.Empty);
			foreach(XGServer serv in this.ExecuteQuery ("SELECT * FROM server;", null, typeof(XGServer)))
			{
				aParent.RootObject.AddServer(serv);

				dic["guid"] = serv.Guid.ToString ();
				foreach(XGChannel chan in this.ExecuteQuery ("SELECT * FROM channel WHERE ParentGuid = @guid;", dic, typeof(XGChannel)))
				{
					serv.AddChannel(chan);

					dic["guid"] = chan.Guid.ToString ();
					foreach(XGBot bot in this.ExecuteQuery ("SELECT * FROM bot WHERE ParentGuid = @guid;", dic, typeof(XGBot)))
					{
						chan.AddBot(bot);

						dic["guid"] = bot.Guid.ToString ();
						foreach(XGPacket pack in this.ExecuteQuery ("SELECT * FROM packet WHERE ParentGuid = @guid;", dic, typeof(XGPacket)))
						{
							bot.addPacket(pack);
						}
					}
				}
			}

			#endregion

			/**/
			// import routine
			Importer importer = new Importer(aParent.RootObject);

			importer.ObjectAddedEvent += new ObjectObjectDelegate (myRunner_ObjectAddedEventHandler);

			importer.Import("./import");

			importer.ObjectAddedEvent -= new ObjectObjectDelegate (myRunner_ObjectAddedEventHandler);
			/**/
		}

		#region RUN STOP
		
		public void Start (ServerRunner aParent)
		{
			this.myRunner = aParent;

			// start the server thread
			this.myServerThread = new Thread (new ThreadStart (OpenClient));
			this.myServerThread.Start ();

			this.myRunner.ObjectAddedEvent += new ObjectObjectDelegate (myRunner_ObjectAddedEventHandler);
			this.myRunner.ObjectChangedEvent += new ObjectDelegate (myRunner_ObjectChangedEventHandler);
			this.myRunner.ObjectRemovedEvent += new ObjectObjectDelegate (myRunner_ObjectRemovedEventHandler);
		}
		
		public void Stop ()
		{
			this.myRunner.ObjectAddedEvent -= new ObjectObjectDelegate (myRunner_ObjectAddedEventHandler);
			this.myRunner.ObjectChangedEvent -= new ObjectDelegate (myRunner_ObjectChangedEventHandler);
			this.myRunner.ObjectRemovedEvent -= new ObjectObjectDelegate (myRunner_ObjectRemovedEventHandler);

			this.CloseClient ();
			this.myServerThread.Abort ();
		}
		
		#endregion

		#region SERVER

		private void OpenClient ()
		{
		}

		private void CloseClient ()
		{
			try
			{
				this.myDbConnection.Close ();
			}
			catch (Exception ex)
			{
				this.Log ("CloseClient() : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);
			}
		}

		#endregion

		#region EVENTS

		protected void myRunner_ObjectAddedEventHandler (XGObject aParentObj, XGObject aObj)
		{
			string table = this.GetTable4Object (aObj);
			Dictionary<string, object> dic = this.Object2Dic (aObj);

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

				this.ExecuteNonQuery ("INSERT INTO " + table + " (" + values1 + ") VALUES (" + values2 + ");", dic);
			}
		}

		protected void myRunner_ObjectChangedEventHandler (XGObject aObj)
		{
			string table = this.GetTable4Object (aObj);
			Dictionary<string, object> dic = this.Object2Dic (aObj);

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
				this.ExecuteNonQuery ("UPDATE " + table + " SET " + values1 + " WHERE Guid = @guid;", dic);
			}
		}

		protected void myRunner_ObjectRemovedEventHandler (XGObject aParentObj, XGObject aObj)
		{
			string table = this.GetTable4Object (aObj);
			Dictionary<string, object> dic = new Dictionary<string, object> ();

			if (table != "")
			{
				dic.Add ("guid", aObj.Guid.ToString ());
				this.ExecuteNonQuery ("DELETE FROM " + table + " WHERE Guid = @guid;", dic);
			}
		}

		#endregion

		#region HELPER

		protected Dictionary<string, object> Object2Dic (XGObject aObj)
		{
			Dictionary<string, object> dic = new Dictionary<string, object> ();
			dic.Add ("Name", aObj.Name);
			dic.Add ("Connected", aObj.Connected);
			dic.Add ("Enabled", aObj.Enabled);
			dic.Add ("LastModified", this.Date2Timestamp (aObj.LastModified));

			if (aObj.GetType () == typeof(XGServer))
			{
				XGServer obj = (XGServer)aObj;
				dic.Add ("Port", obj.Port);
				dic.Add ("ErrorCode", obj.ErrorCode);
				dic.Add ("ChannelCount", obj.Children.Length);
			}
			else if (aObj.GetType () == typeof(XGChannel))
			{
				XGChannel obj = (XGChannel)aObj;
				dic.Add ("ParentGuid", obj.ParentGuid);
				dic.Add ("ErrorCode", obj.ErrorCode);
				dic.Add ("BotCount", obj.Children.Length);
			}
			else if (aObj.GetType () == typeof(XGBot))
			{
				XGBot obj = (XGBot)aObj;
				dic.Add ("ParentGuid", obj.ParentGuid);
				dic.Add ("BotState", obj.BotState);
				dic.Add ("InfoQueueCurrent", obj.InfoQueueCurrent);
				dic.Add ("InfoQueueTotal", obj.InfoQueueTotal);
				dic.Add ("InfoSlotCurrent", obj.InfoSlotCurrent);
				dic.Add ("InfoSlotTotal", obj.InfoSlotTotal);
				dic.Add ("InfoSpeedCurrent", obj.InfoSpeedCurrent);
				dic.Add ("InfoSpeedMax", obj.InfoSpeedMax);
				dic.Add ("LastContact", this.Date2Timestamp (obj.LastContact));
				dic.Add ("LastMessage", obj.LastMessage);
			}
			else if (aObj.GetType () == typeof(XGPacket))
			{
				XGPacket obj = (XGPacket)aObj;
				dic.Add ("ParentGuid", obj.ParentGuid);
				dic.Add ("Id", obj.Id);
				dic.Add ("LastUpdated", this.Date2Timestamp (obj.LastUpdated));
				dic.Add ("LastMentioned", this.Date2Timestamp (obj.LastMentioned));
				dic.Add ("Size", obj.Size);
			}

			return dic;
		}

		protected XGObject Dic2Object (Dictionary<string, object> aDic, Type aType)
		{
			if (aType == typeof(XGServer))
			{
				XGServer serv = new XGServer();
				serv.SetGuid(new Guid((string)aDic ["Guid"]));
				serv.Name = (string)aDic ["Name"];
				serv.Connected = false;
				serv.Enabled = (bool)aDic ["Enabled"];
				serv.Port = (int)aDic ["Port"];
				return serv;
			}
			else if (aType == typeof(XGChannel))
			{
				XGChannel chan = new XGChannel();
				chan.SetGuid(new Guid((string)aDic ["Guid"]));
				chan.Name = (string)aDic ["Name"];
				chan.Connected = false;
				chan.Enabled = (bool)aDic ["Enabled"];
				chan.ErrorCode = (int)aDic ["ErrorCode"];
				return chan;
			}
			else if (aType == typeof(XGBot))
			{
				XGBot bot = new XGBot();
				bot.SetGuid(new Guid((string)aDic ["Guid"]));
				bot.Name = (string)aDic ["Name"];
				bot.Connected = false;
				bot.Enabled = (bool)aDic ["Enabled"];
				bot.BotState = BotState.Idle;
				bot.InfoQueueCurrent = (int)aDic ["InfoQueueCurrent"];
				bot.InfoQueueTotal = (int)aDic ["InfoQueueTotal"];
				bot.InfoSlotCurrent = (int)aDic ["InfoSlotCurrent"];
				bot.InfoSlotTotal = (int)aDic ["InfoSlotTotal"];
				bot.InfoSpeedCurrent = (double)aDic ["InfoSpeedCurrent"];
				bot.InfoSpeedMax = (double)aDic ["InfoSpeedMax"];
				bot.LastMessage = (string)aDic ["LastMessage"];
				bot.LastContact = this.Timestamp2Date ((Int64)aDic ["LastContact"]);
				return bot;
			}
			else if (aType == typeof(XGPacket))
			{
				XGPacket pack = new XGPacket();
				pack.SetGuid(new Guid((string)aDic ["Guid"]));
				pack.Name = (string)aDic ["Name"];
				pack.Connected = false;
				pack.Enabled = (bool)aDic ["Enabled"];
				pack.Name = (string)aDic ["Name"];
				pack.Id = (int)aDic ["Id"];
				pack.LastUpdated = this.Timestamp2Date ((Int64)aDic ["LastUpdated"]);
				pack.LastMentioned = this.Timestamp2Date ((Int64)aDic ["LastMentioned"]);
				pack.Size = (Int64)aDic ["Size"];
				return pack;
			}

			return null;
		}

		protected void ExecuteNonQuery (string aSql, Dictionary<string, object> aDic)
		{
			//aSql = "START TRANSACTION;" + aSql + "COMMIT;";
			lock (locked)
			{
				MySqlCommand cmd = new MySqlCommand (aSql, this.myDbConnection);
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
						this.Log ("ExecuteQuery(" + ex.Number + ") '" + this.GetSqlString (aSql, aDic) + "' : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);
					}
					catch (InvalidOperationException ex)
					{
						this.Log ("ExecuteQuery() '" + this.GetSqlString (aSql, aDic) + "' : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);
						this.Log ("ExecuteQuery() : stopping server plugin!", LogLevel.Warning);
						this.Stop ();
					}
					catch (Exception ex)
					{
						this.Log ("ExecuteQuery() '" + this.GetSqlString (aSql, aDic) + "' : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);
					}
				}
			}
		}

		protected List<XGObject> ExecuteQuery (string aSql, Dictionary<string, object> aDic, Type aType)
		{
			List<XGObject> list = new List<XGObject> ();

			//aSql = "START TRANSACTION;" + aSql + "COMMIT;";
			lock (locked)
			{
				MySqlCommand cmd = new MySqlCommand (aSql, this.myDbConnection);
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
							XGObject obj = this.Dic2Object (dic, aType);
							if(obj != null)
							list.Add (obj);
						}
						myReader.Close();
					}
					catch (MySqlException ex)
					{
						this.Log ("ExecuteReader(" + ex.Number + ") '" + this.GetSqlString (aSql, aDic) + "' : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);
					}
					catch (InvalidOperationException ex)
					{
						this.Log ("ExecuteReader() '" + this.GetSqlString (aSql, aDic) + "' : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);
						this.Log ("ExecuteReader() : stopping server plugin!", LogLevel.Warning);
						this.Stop ();
					}
					catch (Exception ex)
					{
						this.Log ("ExecuteReader() '" + this.GetSqlString (aSql, aDic) + "' : " + XGHelper.GetExceptionMessage (ex), LogLevel.Exception);
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

		protected string GetTable4Object (XGObject aObj)
		{
			if (aObj.GetType () == typeof(XGServer))
			{
				return "server";
			}
			else if (aObj.GetType () == typeof(XGChannel))
			{
				return "channel";
			}
			else if (aObj.GetType () == typeof(XGBot))
			{
				return "bot";
			}
			else if (aObj.GetType () == typeof(XGPacket))
			{
				return "packet";
			}
			return "";
		}

		protected string GetSqlString (string aSql, Dictionary<string, object> aDic)
		{
			if (aDic != null)
			{
				foreach (KeyValuePair<string, object> kcp in aDic)
				{
					aSql.Replace ("@" + kcp.Key, "" + kcp.Value);
				}
			}
			return aSql;
		}

		#endregion

		#region LOG

		/// <summary>
		/// Calls XGHelper.Log()
		/// </summary>
		/// <param name="aData"></param>
		/// <param name="aLevel"></param>
		private void Log (string aData, LogLevel aLevel)
		{
			XGHelper.Log ("MySqlBackend." + aData, aLevel);
		}

		#endregion
	}
}
