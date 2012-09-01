//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
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
using System.IO;
using System.Xml.Serialization;
using log4net;
using XG.Core;

namespace XG.Server
{
	[Serializable()]
	public class Settings
	{
		static readonly ILog myLog = LogManager.GetLogger(typeof(Settings));

		static Settings instance = null;

		/// <value>
		/// Returns an instance of the settings - just loaded once
		/// </value>
		public static Settings Instance
		{
			get
			{
				if (instance == null)
				{
					instance = Deserialize();
					Serialize();
				}
				return instance;
			}
		}

		/// <value>
		/// Returns an instance of the settings - loaded new every time you request it
		/// </value>
		public static Settings InstanceReload
		{
			get
			{
				instance = Deserialize();
				return instance;
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
				myLog.Fatal("Settings.Instance", ex);
				return new Settings();
			}
		}

		static void Serialize()
		{
			try
			{
				XmlSerializer ser = new XmlSerializer(typeof(Settings));
				StreamWriter sw = new StreamWriter("./settings.xml");
				ser.Serialize(sw, instance);
				sw.Close();
			}
			catch (Exception ex)
			{
				myLog.Fatal("Settings.Instance", ex);
			}
		}

		/// <summary>
		/// All xg server settings are saved here
		/// some are writeable - others just readable
		/// </summary>
		Settings()
		{
			Random rand = new Random();
			commandWaitTime = 15000;
			botWaitTime = 240000;
			channelWaitTime = 300000;
			channelWaitTimeLong = 900000;
			fileRollback = 512000;
			fileRollbackCheck = 409600;
			updateDownloadTime = 5000;
			downloadPerRead = 102400;
			iRCName = "Anonymous" + rand.Next(10000, 99999);
			tempPath = "./tmp/";
			readyPath = "./dl/";
			mutliDownloadMinimumTime = 300;
			timerSleepTime = 5000;
			serverTimeout = 60000;
			reconnectWaitTime = 45000;
			reconnectWaitTimeLong = 900000;
			reconnectWaitTimeReallyLong = 2700000;
			enableMultiDownloads = false;
			clearReadyDownloads = true;
			ircVersion = "mIRC v6.35 Khaled Mardam-Bey";
			xgVersion = "0.9.2";
			ircRegisterEmail = "anon@ymous.org";
			ircRegisterPasswort = "password123";
			AutoRegisterNickserv = true;
			botOfflineCheckTime = 1200000;
			downloadTimeout = 30000;
			botOfflineTime = 7200000;
			samePacketRequestTime = 10000;
			maxNoDataReceived = 50;
			backupStatisticTime = 60000;

			parsingErrorFile = "./parsing_errors.txt";
			dataBinary = "./xg.bin";
			filesBinary = "./Files.bin";
			searchesBinary = "./xgsearches.bin";
			password = "xgisgreat";
			backupDataTime = 900000;
			fileHandler = new string[] { "" };

			startTCPServer = false;
			tcpServerPort = 5555;

			startWebServer = true;
			webServerPort = 5556;
			AutoJoinOnInvite = true;

			startJabberClient = false;
			jabberServer = "";
			jabberUser = "";
			jabberPassword = "";

			startMySqlBackend = false;
			mySqlBackendServer = "127.0.0.1";
			MySqlBackendDatabase = "xg";
			MySqlBackendUser = "xg";
			MySqlBackendPassword = "xg";
		}

		#region PRIVATE

		long commandWaitTime;
		public long CommandWaitTime
		{
			get { return commandWaitTime; }
		}

		long botWaitTime;
		public long BotWaitTime
		{
			get { return botWaitTime; }
		}

		long channelWaitTime;
		public long ChannelWaitTime
		{
			get { return channelWaitTime; }
		}

		long channelWaitTimeLong;
		public long ChannelWaitTimeLong
		{
			get { return channelWaitTimeLong; }
		}

		long fileRollback;
		public long FileRollback
		{
			get { return fileRollback; }
		}

		long fileRollbackCheck;
		public long FileRollbackCheck
		{
			get { return fileRollbackCheck; }
		}

		int updateDownloadTime;
		public int UpdateDownloadTime
		{
			get { return updateDownloadTime; }
		}

		long downloadPerRead;
		public long DownloadPerRead
		{
			get { return downloadPerRead; }
		}

		long botOfflineTime;
		public long BotOfflineTime
		{
			get { return botOfflineTime; }
		}

		long samePacketRequestTime;
		public long SamePacketRequestTime
		{
			get { return samePacketRequestTime; }
		}

		string ircVersion;
		public string IrcVersion
		{
			get { return ircVersion; }
		}

		string xgVersion;
		public string XgVersion
		{
			get { return xgVersion; }
		}

		int botOfflineCheckTime;
		public int BotOfflineCheckTime
		{
			get { return botOfflineCheckTime; }
		}

		int downloadTimeout;
		public int DownloadTimeout
		{
			get { return downloadTimeout; }
		}
		
		int serverTimeout;
		public int ServerTimeout
		{
			get { return serverTimeout; }
		}

		int reconnectWaitTime;
		public int ReconnectWaitTime
		{
			get { return reconnectWaitTime; }
		}

		int reconnectWaitTimeLong;
		public int ReconnectWaitTimeLong
		{
			get { return reconnectWaitTimeLong; }
		}

		int reconnectWaitTimeReallyLong;
		public int ReconnectWaitTimeReallyLong
		{
			get { return reconnectWaitTimeReallyLong; }
		}

		int mutliDownloadMinimumTime;
		public int MutliDownloadMinimumTime
		{
			get { return mutliDownloadMinimumTime; }
		}

		long timerSleepTime;
		public long TimerSleepTime
		{
			get { return timerSleepTime; }
		}

		string parsingErrorFile;
		public string ParsingErrorFile
		{
			get { return parsingErrorFile; }
		}

		string dataBinary;
		public string DataBinary
		{
			get { return dataBinary; }
		}

		string filesBinary;
		public string FilesBinary
		{
			get { return filesBinary; }
		}

		string searchesBinary;
		public string SearchesBinary
		{
			get { return searchesBinary; }
		}

		int maxNoDataReceived;
		public int MaxNoDataReceived
		{
			get { return maxNoDataReceived; }
		}

		int backupStatisticTime;
		public int BackupStatisticTime
		{
			get { return backupStatisticTime; }
		}

		#endregion

		#region PUBLIC

		string iRCName;
		public string IRCName
		{
			get { return iRCName; }
			set { iRCName = value; }
		}

		string tempPath;
		public string TempPath
		{
			get { return tempPath; }
			set { tempPath = value; }
		}

		string readyPath;
		public string ReadyPath
		{
			get { return readyPath; }
			set { readyPath = value; }
		}

		string ircRegisterPasswort;
		public string IrcRegisterPasswort
		{
			get { return ircRegisterPasswort; }
			set { ircRegisterPasswort = value; }
		}

		string ircRegisterEmail;
		public string IrcRegisterEmail
		{
			get { return ircRegisterEmail; }
			set { ircRegisterEmail = value; }
		}

		bool enableMultiDownloads;
		public bool EnableMultiDownloads
		{
			get { return enableMultiDownloads; }
			set { enableMultiDownloads = value; }
		}

		bool clearReadyDownloads;
		public bool ClearReadyDownloads
		{
			get { return clearReadyDownloads; }
			set { clearReadyDownloads = value; }
		}

		int tcpServerPort;
		public int TcpServerPort
		{
			get { return tcpServerPort; }
			set { tcpServerPort = value; }
		}

		int webServerPort;
		public int WebServerPort
		{
			get { return webServerPort; }
			set { webServerPort = value; }
		}

		string password;
		public string Password
		{
			get { return password; }
			set { password = value; }
		}

		long backupDataTime;
		public long BackupDataTime
		{
			get { return backupDataTime; }
			set { backupDataTime = value; }
		}

		string[] fileHandler;
		public string[] FileHandler
		{
			get { return fileHandler; }
			set { fileHandler = value; }
		}

		bool startTCPServer;
		public bool StartTCPServer
		{
			get { return startTCPServer; }
			set { startTCPServer = value; }
		}

		bool startWebServer;
		public bool StartWebServer
		{
			get { return startWebServer; }
			set { startWebServer = value; }
		}

		bool startJabberClient;
		public bool StartJabberClient
		{
			get { return startJabberClient; }
			set { startJabberClient = value; }
		}

		string jabberServer;
		public string JabberServer
		{
			get { return jabberServer; }
			set { jabberServer = value; }
		}

		string jabberUser;
		public string JabberUser
		{
			get { return jabberUser; }
			set { jabberUser = value; }
		}

		string jabberPassword;
		public string JabberPassword
		{
			get { return jabberPassword; }
			set { jabberPassword = value; }
		}

		bool startMySqlBackend;
		public bool StartMySqlBackend
		{
			get { return startMySqlBackend; }
			set { startMySqlBackend = value; }
		}

		string mySqlBackendServer;
		public string MySqlBackendServer
		{
			get { return mySqlBackendServer; }
			set { mySqlBackendServer = value; }
		}

		public string MySqlBackendDatabase
		{
			get;
			set;
		}

		public string MySqlBackendUser
		{
			get;
			set;
		}

		public string MySqlBackendPassword
		{
			get;
			set;
		}

		public bool AutoJoinOnInvite
		{
			get;
			set;
		}


		public bool AutoRegisterNickserv
		{
			get;
			set;
		}

		#endregion
	}
}
