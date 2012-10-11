// 
//  Settings.cs
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
using System.IO;
using System.Xml.Serialization;
using log4net;

namespace XG.Server
{
	[Serializable]
	public class Settings
	{
		static readonly ILog _log = LogManager.GetLogger(typeof(Settings));

		static Settings _instance = null;

		/// <value>
		/// Returns an instance of the settings - just loaded once
		/// </value>
		public static Settings Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Deserialize();
					Serialize();
				}
				return _instance;
			}
		}

		/// <value>
		/// Returns an instance of the settings - loaded new every time you request it
		/// </value>
		public static Settings InstanceReload
		{
			get
			{
				_instance = Deserialize();
				return _instance;
			}
		}

		/// <summary>
		/// Deserializes a previously saved settings object from the file named setings.xml
		/// </summary>
		/// <returns>
		/// A <see cref="Settings"/>
		/// </returns>
		static Settings Deserialize()
		{
			if (File.Exists("./settings.xml"))
			{
				try
				{
					XmlSerializer ser = new XmlSerializer(typeof(Settings));
					StreamReader sr = new StreamReader("./settings.xml");
					Settings settings = (Settings)ser.Deserialize(sr);
					sr.Close();
					return settings;
				}
				catch (Exception ex)
				{
					_log.Fatal("Settings.Deserialize", ex);
				}
			}
			else
			{
				_log.Error("Settings.Deserialize found no settings file, using default one");
			}
			return new Settings();
		}

		static void Serialize()
		{
			try
			{
				XmlSerializer ser = new XmlSerializer(typeof(Settings));
				StreamWriter sw = new StreamWriter("./settings.xml");
				ser.Serialize(sw, _instance);
				sw.Close();
			}
			catch (Exception ex)
			{
				_log.Fatal("Settings.Serialize", ex);
			}
		}

		/// <summary>
		/// All xg server settings are saved here
		/// some are writeable - others just readable
		/// </summary>
		Settings()
		{
			IRCName = "Anonymous" + new Random().Next(10000, 99999);
			TempPath = "./tmp/";
			ReadyPath = "./dl/";
			EnableMultiDownloads = false;
			ClearReadyDownloads = true;
			IrcRegisterEmail = "anon@ymous.org";
			IrcRegisterPasswort = "password123";
			AutoRegisterNickserv = true;

			Password = "xgisgreat";
			BackupDataTime = 900000;
			FileHandlers = new FileHandler[0];

			StartWebServer = true;
			WebServerPort = 5556;
			AutoJoinOnInvite = true;

			StartJabberClient = false;
			JabberServer = "";
			JabberUser = "";
			JabberPassword = "";

			StartMySqlBackend = false;
			MySqlBackendServer = "127.0.0.1";
			MySqlBackendDatabase = "xg";
			MySqlBackendUser = "xg";
			MySqlBackendPassword = "xg";
		}

		#region PRIVATE

		public long CommandWaitTime
		{
			get { return 15000; }
		}

		public long BotWaitTime
		{
			get { return 240000; }
		}

		public long ChannelWaitTime
		{
			get { return 300000; }
		}

		public long ChannelWaitTimeLong
		{
			get { return 900000; }
		}

		public long FileRollback
		{
			get { return 512000; }
		}

		public long FileRollbackCheck
		{
			get { return 409600; }
		}

		public int UpdateDownloadTime
		{
			get { return 5000; }
		}

		public long DownloadPerRead
		{
			get { return 102400; }
		}

		public long BotOfflineTime
		{
			get { return 7200000; }
		}

		public long SamePacketRequestTime
		{
			get { return 10000; }
		}

		public string IrcVersion
		{
			get { return "mIRC v6.35 Khaled Mardam-Bey"; }
		}

		public string XgVersion
		{
			get { return "0.9.2"; }
		}

		public int BotOfflineCheckTime
		{
			get { return 1200000; }
		}

		public int DownloadTimeout
		{
			get { return 30000; }
		}
		
		public int ServerTimeout
		{
			get { return 60000; }
		}

		public int ReconnectWaitTime
		{
			get { return 45000; }
		}

		public int ReconnectWaitTimeLong
		{
			get { return 900000; }
		}

		public int ReconnectWaitTimeReallyLong
		{
			get { return 2700000; }
		}

		public int MutliDownloadMinimumTime
		{
			get { return 300; }
		}

		public long TimerSleepTime
		{
			get { return 5000; }
		}
		
		public long TimerSnapshotsSleepTime
		{
			get { return 600000; }
		}

		public string ParsingErrorFile
		{
			get { return "./parsing_errors.txt"; }
		}

		public string DataBinary
		{
			get { return "./xg.bin"; }
		}

		public string FilesBinary
		{
			get { return "./xgfiles.bin"; }
		}

		public string SearchesBinary
		{
			get { return "./xgsearches.bin"; }
		}
		
		public string SnapshotsBinary
		{
			get { return "./xgstatistics.bin"; }
		}

		public int MaxNoDataReceived
		{
			get { return 50; }
		}

		public int BackupStatisticTime
		{
			get { return 60000; }
		}

		#endregion

		#region PUBLIC

		public string IRCName { get; set; }

		public string TempPath { get; set; }

		public string ReadyPath { get; set; }

		public string IrcRegisterPasswort { get; set; }

		public string IrcRegisterEmail { get; set; }

		public bool EnableMultiDownloads { get; set; }

		public bool ClearReadyDownloads { get; set; }

		public int WebServerPort { get; set; }

		public string Password { get; set; }

		public long BackupDataTime { get; set; }

		public bool StartWebServer { get; set; }

		public bool StartJabberClient { get; set; }

		public string JabberServer { get; set; }

		public string JabberUser { get; set; }

		public string JabberPassword { get; set; }

		public bool StartMySqlBackend { get; set; }

		public string MySqlBackendServer { get; set; }

		public string MySqlBackendDatabase { get; set; }

		public string MySqlBackendUser { get; set; }

		public string MySqlBackendPassword { get; set; }

		public bool AutoJoinOnInvite { get; set; }

		public bool AutoRegisterNickserv { get; set; }

		public FileHandler[] FileHandlers { get; set; }

		#endregion
	}
}
