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
using XG.Core;

namespace XG.Server
{
	[Serializable()]
	public class Settings
	{
		private static Settings instance = null;

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
		private static Settings Deserialize()
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
				XGHelper.Log("Settings.Instance: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				return new Settings();
			}
		}

		private static void Serialize()
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
				XGHelper.Log("Settings.Instance: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
		}

		/// <summary>
		/// All xg server settings are saved here
		/// some are writeable - others just readable
		/// </summary>
		private Settings()
		{
			Random rand = new Random();
			this.commandWaitTime = 15000;
			this.botWaitTime = 240000;
			this.channelWaitTime = 300000;
			this.channelWaitTimeLong = 900000;
			this.fileRollback = 512000;
			this.fileRollbackCheck = 409600;
			this.updateDownloadTime = 5000;
			this.downloadPerRead = 102400;
			this.iRCName = "Anonymous" + rand.Next(10000, 99999);
			this.tempPath = "./tmp/";
			this.readyPath = "./dl/";
			this.mutliDownloadMinimumTime = 300;
			this.timerSleepTime = 10000;
			this.serverTimeout = 60000;
			this.reconnectWaitTime = 45000;
			this.reconnectWaitTimeLong = 900000;
			this.reconnectWaitTimeReallyLong = 2700000;
			this.enableMultiDownloads = false;
			this.clearReadyDownloads = true;
			this.ircVersion = "mIRC v6.35 Khaled Mardam-Bey";
			this.xgVersion = "9";
			this.ircRegisterEmail = "anon@ymous.org";
			this.ircRegisterPasswort = "password123";
			this.botOfflineCheckTime = 1200000;
			this.downloadTimeout = 30000;
			this.botOfflineTime = 7200000;
			this.samePacketRequestTime = 10000;
			this.maxNoDataReceived = 5;
			this.backupStatisticTime = 60000;

			this.parsingErrorFile = "./parsing_errors.txt";
			this.dataBinary = "./xg.bin";
			this.filesBinary = "./xgfiles.bin";
			this.searchesBinary = "./xgsearches.bin";
			this.password = "xgisgreat";
			this.backupDataTime = 900000;
			this.fileHandler = new string[] { "" };

			this.startTCPServer = false;
			this.tcpServerPort = 5555;

			this.startWebServer = true;
			this.webServerPort = 5556;
			this.styleWebServer = "blitzer";
			this.autoJoinOnInvite = true;

			this.startJabberClient = false;
			this.jabberServer = "";
			this.jabberUser = "";
			this.jabberPassword = "";

			this.startMySqlBackend = false;

#if DEBUG
			this.logLevel = LogLevel.Notice;
#else
			this.logLevel = LogLevel.Warning;
#endif
		}

		#region PRIVATE

		private long commandWaitTime;
		public long CommandWaitTime
		{
			get { return this.commandWaitTime; }
		}

		private long botWaitTime;
		public long BotWaitTime
		{
			get { return this.botWaitTime; }
		}

		private long channelWaitTime;
		public long ChannelWaitTime
		{
			get { return this.channelWaitTime; }
		}

		private long channelWaitTimeLong;
		public long ChannelWaitTimeLong
		{
			get { return this.channelWaitTimeLong; }
		}

		private long fileRollback;
		public long FileRollback
		{
			get { return this.fileRollback; }
		}

		private long fileRollbackCheck;
		public long FileRollbackCheck
		{
			get { return this.fileRollbackCheck; }
		}

		private int updateDownloadTime;
		public int UpdateDownloadTime
		{
			get { return this.updateDownloadTime; }
		}

		private long downloadPerRead;
		public long DownloadPerRead
		{
			get { return this.downloadPerRead; }
		}

		private long botOfflineTime;
		public long BotOfflineTime
		{
			get { return this.botOfflineTime; }
		}

		private long samePacketRequestTime;
		public long SamePacketRequestTime
		{
			get { return this.samePacketRequestTime; }
		}

		private string ircVersion;
		public string IrcVersion
		{
			get { return this.ircVersion; }
		}

		private string xgVersion;
		public string XgVersion
		{
			get { return this.xgVersion; }
		}

		private int botOfflineCheckTime;
		public int BotOfflineCheckTime
		{
			get { return this.botOfflineCheckTime; }
		}

		private int downloadTimeout;
		public int DownloadTimeout
		{
			get { return this.downloadTimeout; }
		}
		
		private int serverTimeout;
		public int ServerTimeout
		{
			get { return this.serverTimeout; }
		}

		private int reconnectWaitTime;
		public int ReconnectWaitTime
		{
			get { return this.reconnectWaitTime; }
		}

		private int reconnectWaitTimeLong;
		public int ReconnectWaitTimeLong
		{
			get { return this.reconnectWaitTimeLong; }
		}

		private int reconnectWaitTimeReallyLong;
		public int ReconnectWaitTimeReallyLong
		{
			get { return this.reconnectWaitTimeReallyLong; }
		}

		private int mutliDownloadMinimumTime;
		public int MutliDownloadMinimumTime
		{
			get { return this.mutliDownloadMinimumTime; }
		}

		private long timerSleepTime;
		public long TimerSleepTime
		{
			get { return this.timerSleepTime; }
		}

		private string parsingErrorFile;
		public string ParsingErrorFile
		{
			get { return this.parsingErrorFile; }
		}

		private string dataBinary;
		public string DataBinary
		{
			get { return this.dataBinary; }
		}

		private string filesBinary;
		public string FilesBinary
		{
			get { return this.filesBinary; }
		}

		private string searchesBinary;
		public string SearchesBinary
		{
			get { return this.searchesBinary; }
		}

		private int maxNoDataReceived;
		public int MaxNoDataReceived
		{
			get { return this.maxNoDataReceived; }
		}

		private int backupStatisticTime;
		public int BackupStatisticTime
		{
			get { return this.backupStatisticTime; }
		}

		#endregion

		#region PUBLIC

		private string iRCName;
		public string IRCName
		{
			get { return this.iRCName; }
			set { this.iRCName = value; }
		}

		private string tempPath;
		public string TempPath
		{
			get { return this.tempPath; }
			set { this.tempPath = value; }
		}

		private string readyPath;
		public string ReadyPath
		{
			get { return this.readyPath; }
			set { this.readyPath = value; }
		}

		private string ircRegisterPasswort;
		public string IrcRegisterPasswort
		{
			get { return this.ircRegisterPasswort; }
			set { this.ircRegisterPasswort = value; }
		}

		private string ircRegisterEmail;
		public string IrcRegisterEmail
		{
			get { return this.ircRegisterEmail; }
			set { this.ircRegisterEmail = value; }
		}

		private bool enableMultiDownloads;
		public bool EnableMultiDownloads
		{
			get { return this.enableMultiDownloads; }
			set { this.enableMultiDownloads = value; }
		}

		private bool clearReadyDownloads;
		public bool ClearReadyDownloads
		{
			get { return this.clearReadyDownloads; }
			set { this.clearReadyDownloads = value; }
		}

		private int tcpServerPort;
		public int TcpServerPort
		{
			get { return this.tcpServerPort; }
			set { this.tcpServerPort = value; }
		}

		private int webServerPort;
		public int WebServerPort
		{
			get { return this.webServerPort; }
			set { this.webServerPort = value; }
		}

		private string password;
		public string Password
		{
			get { return this.password; }
			set { this.password = value; }
		}

		private long backupDataTime;
		public long BackupDataTime
		{
			get { return this.backupDataTime; }
			set { this.backupDataTime = value; }
		}

		private string[] fileHandler;
		public string[] FileHandler
		{
			get { return this.fileHandler; }
			set { this.fileHandler = value; }
		}

		private bool startTCPServer;
		public bool StartTCPServer
		{
			get { return this.startTCPServer; }
			set { this.startTCPServer = value; }
		}

		private bool startWebServer;
		public bool StartWebServer
		{
			get { return this.startWebServer; }
			set { this.startWebServer = value; }
		}

		private string styleWebServer;
		public string StyleWebServer
		{
			get { return this.styleWebServer; }
			set { this.styleWebServer = value; }
		}

		private bool startJabberClient;
		public bool StartJabberClient
		{
			get { return this.startJabberClient; }
			set { this.startJabberClient = value; }
		}

		private string jabberServer;
		public string JabberServer
		{
			get { return this.jabberServer; }
			set { this.jabberServer = value; }
		}

		private string jabberUser;
		public string JabberUser
		{
			get { return this.jabberUser; }
			set { this.jabberUser = value; }
		}

		private string jabberPassword;
		public string JabberPassword
		{
			get { return this.jabberPassword; }
			set { this.jabberPassword = value; }
		}

		private bool startMySqlBackend;
		public bool StartMySqlBackend
		{
			get { return this.startMySqlBackend; }
			set { this.startMySqlBackend = value; }
		}

		private bool autoJoinOnInvite;
		public bool AutoJoinOnInvite
		{
			get { return this.autoJoinOnInvite; }
			set { this.autoJoinOnInvite = value; }
		}

		private LogLevel logLevel;
		public LogLevel LogLevel
		{
			get { return this.logLevel; }
			set { this.logLevel = value; }
		}

		#endregion
	}
}
