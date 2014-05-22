// 
//  SqliteConverter.cs
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
using System.Reflection;
using XG.Config.Properties;
using XG.Model.Domain;
using log4net;
using System.Data.Common;

#if __MonoCS__
using Mono.Data.Sqlite;
#else
using System.Data.SQLite;
#endif

namespace XG.DB
{
	public class SqliteConverter
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public Servers Servers = new Servers();
		public Files Files = new Files();
		public Searches Searches = new Searches();
		public ApiKeys ApiKeys = new ApiKeys();

#if __MonoCS__
		SqliteConnection _con;
#else
		SQLiteConnection _con;
#endif

		public void Load()
		{
			string db = Settings.Default.GetAppDataPath() + "xgobjects.db";
			string connection = "Data Source=" + db + ";Version=3;BinaryGuid=False;synchronous=off;journal mode=memory";

#if __MonoCS__
			_con = new SqliteConnection(connection);
#else
			_con = new SQLiteConnection(connection);
#endif
			_con.Open();

			LoadServers();
			LoadFiles();
			LoadSearches();
			LoadApiKeys();

			_con.Close();
			_con.Dispose();
		}

		DbCommand GetCommand()
		{
			return _con.CreateCommand();
		}

		void LoadServers()
		{
			Log.Info("Load() servers");
			DbCommand command = GetCommand();
			command.CommandText = "SELECT * FROM Server;";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var server = CreateServer(reader);
				Servers.Add(server);
				LoadChannels(server);
			}
		}
			
		void LoadChannels(Server server)
		{
			Log.Info("Load() channels from " + server);
			DbCommand command = GetCommand();
			command.CommandText = "SELECT * FROM Channel WHERE ParentGuid = '" + server.Guid + "';";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var channel = CreateChannel(reader);
				server.AddChannel(channel);
				LoadBots(channel);
			}
		}

		void LoadBots(Channel channel)
		{
			Log.Info("Load() bots from " + channel);
			DbCommand command = GetCommand();
			command.CommandText = "SELECT * FROM Bot WHERE ParentGuid = '" + channel.Guid + "';";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var bot = CreateBot(reader);
				channel.AddBot(bot);
				LoadPackets(bot);
			}
		}

		void LoadPackets(Bot bot)
		{
			Log.Info("Load() packets from " + bot);
			DbCommand command = GetCommand();
			command.CommandText = "SELECT * FROM Packet WHERE ParentGuid = '" + bot.Guid + "';";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var packet = CreatePacket(reader);
				bot.AddPacket(packet);
			}
		}

		void LoadFiles()
		{
			Log.Info("Load() files");
			DbCommand command = GetCommand();
			command.CommandText = "SELECT * FROM File;";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				Files.Add(CreateFile(reader));
			}
		}

		void LoadSearches()
		{
			Log.Info("Load() searches");
			DbCommand command = GetCommand();
			command.CommandText = "SELECT * FROM Search;";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				Searches.Add(CreateSearch(reader));
			}
		}

		void LoadApiKeys()
		{
			Log.Info("Load() apikeys");
			DbCommand command = GetCommand();
			command.CommandText = "SELECT * FROM ApiKey;";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				ApiKeys.Add(CreateApiKey(reader));
			}
		}

		Server CreateServer(DbDataReader reader)
		{
			var obj = new Server
			{
				Guid = (Guid)reader["Guid"],
				Name = (string)reader["Name"],
				Connected = (bool)reader["Connected"],
				Enabled = (bool)reader["Enabled"]
			};
			try
			{
				obj.Port = (int)reader["Port"];
			}
			catch(Exception) {}
			try
			{
				obj.ErrorCode = (SocketErrorCode)(int)reader["ErrorCode"];
			}
			catch(Exception) {}

			return obj;
		}

		Channel CreateChannel(DbDataReader reader)
		{
			var obj = new Channel
			{
				Guid = (Guid)reader["Guid"],
				Name = (string)reader["Name"],
				Connected = (bool)reader["Connected"],
				Enabled = (bool)reader["Enabled"]
			};
			try
			{
				obj.ErrorCode = (int)reader["ErrorCode"];
			}
			catch(Exception) {}
			try
			{
				obj.Topic = (string)reader["Topic"];
			}
			catch(Exception) {}
			try
			{
				obj.UserCount = (int)reader["UserCount"];
			}
			catch(Exception) {}

			return obj;
		}

		Bot CreateBot(DbDataReader reader)
		{
			var obj = new Bot
			{
				Guid = (Guid)reader["Guid"],
				Name = (string)reader["Name"],
				Connected = (bool)reader["Connected"],
				Enabled = (bool)reader["Enabled"]
			};
			try
			{
				obj.State = (Bot.States)(int)reader["State"];
			}
			catch(Exception) {}
			try
			{
				obj.LastMessage = (string)reader["LastMessage"];
			}
			catch(Exception) {}
			try
			{
				obj.LastMessageTime = (DateTime)reader["LastMessageTime"];
			}
			catch(Exception) {}
			try
			{
				obj.LastContact = (DateTime)reader["LastContact"];
			}
			catch(Exception) {}
			try
			{
				obj.QueuePosition = (int)reader["QueuePosition"];
			}
			catch(Exception) {}
			try
			{
				obj.QueueTime = (int)reader["QueueTime"];
			}
			catch(Exception) {}
			try
			{
				obj.InfoSpeedMax = (long)reader["InfoSpeedMax"];
			}
			catch(Exception) {}
			try
			{
				obj.InfoSpeedCurrent = (long)reader["InfoSpeedCurrent"];
			}
			catch(Exception) {}
			try
			{
				obj.InfoSlotTotal = (int)reader["InfoSlotTotal"];
			}
			catch(Exception) {}
			try
			{
				obj.InfoSlotCurrent = (int)reader["InfoSlotCurrent"];
			}
			catch(Exception) {}
			try
			{
				obj.InfoQueueTotal = (int)reader["InfoQueueTotal"];
			}
			catch(Exception) {}
			try
			{
				obj.InfoQueueCurrent = (int)reader["InfoQueueCurrent"];
			}
			catch(Exception) {}
			try
			{
				obj.HasNetworkProblems = (int)reader["HasNetworkProblems"] == 1;
			}
			catch(Exception) {}

			return obj;
		}

		Packet CreatePacket(DbDataReader reader)
		{
			var obj = new Packet
			{
				Guid = (Guid)reader["Guid"],
				Name = (string)reader["Name"],
				Connected = (bool)reader["Connected"],
				Enabled = (bool)reader["Enabled"]
			};
			try
			{
				obj.Id = (int)reader["Id"];
			}
			catch(Exception) {}
			try
			{
				obj.Size = (long)reader["Size"];
			}
			catch(Exception) {}
			try
			{
				obj.RealSize = (long)reader["RealSize"];
			}
			catch(Exception) {}
			try
			{
				obj.RealName = (string)reader["RealName"];
			}
			catch(Exception) {}
			try
			{
				obj.LastUpdated = (DateTime)reader["LastUpdated"];
			}
			catch(Exception) {}
			try
			{
				obj.LastMentioned =  (DateTime)reader["LastMentioned"];
			}
			catch(Exception) {}

			return obj;
		}

		File CreateFile(DbDataReader reader)
		{
			return new File((string)reader["Name"], (long)reader["Size"])
			{
				Guid = (Guid)reader["Guid"],
				Connected = (bool)reader["Connected"],
				Enabled = (bool)reader["Enabled"],
				CurrentSize = (long)reader["CurrentSize"]
			};
		}

		Search CreateSearch(DbDataReader reader)
		{
			return new Search
			{
				Guid = (Guid)reader["Guid"],
				Name = (string)reader["Name"]
			};
		}

		ApiKey CreateApiKey(DbDataReader reader)
		{
			return new ApiKey
			{
				Guid = (Guid)reader["Guid"],
				Name = (string)reader["Name"],
				Enabled = (bool)reader["Enabled"],
				ErrorCount = (int)reader["ErrorCount"],
				SuccessCount = (int)reader["SuccessCount"]
			};
		}
	}
}	
