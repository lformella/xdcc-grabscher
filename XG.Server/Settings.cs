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
			this.enableMultiDownloads = true;
			this.clearReadyDownloads = true;
			this.ircVersion = "mIRC v6.35 Khaled Mardam-Bey";
			this.xgVersion = "8";
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

			this.startTCPServer = true;
			this.tcpServerPort = 5555;

			this.startWebServer = true;
			this.webServerPort = 5556;
			this.styleWebServer = "blitzer";
			this.autoJoinOnInvite = true;

			this.startJabberClient = false;
			this.jabberServer = "";
			this.jabberUser = "";
			this.jabberPassword = "";

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
			get { return commandWaitTime; }
		}

		private long botWaitTime;
		public long BotWaitTime
		{
			get { return botWaitTime; }
		}

		private long channelWaitTime;
		public long ChannelWaitTime
		{
			get { return channelWaitTime; }
		}

		private long channelWaitTimeLong;
		public long ChannelWaitTimeLong
		{
			get { return channelWaitTimeLong; }
		}

		private long fileRollback;
		public long FileRollback
		{
			get { return fileRollback; }
		}

		private long fileRollbackCheck;
		public long FileRollbackCheck
		{
			get { return fileRollbackCheck; }
		}

		private int updateDownloadTime;
		public int UpdateDownloadTime
		{
			get { return updateDownloadTime; }
		}

		private long downloadPerRead;
		public long DownloadPerRead
		{
			get { return downloadPerRead; }
		}

		private long botOfflineTime;
		public long BotOfflineTime
		{
			get { return botOfflineTime; }
		}

		private long samePacketRequestTime;
		public long SamePacketRequestTime
		{
			get { return samePacketRequestTime; }
		}

		private string ircVersion;
		public string IrcVersion
		{
			get { return ircVersion; }
		}

		private string xgVersion;
		public string XgVersion
		{
			get { return xgVersion; }
		}

		private int botOfflineCheckTime;
		public int BotOfflineCheckTime
		{
			get { return botOfflineCheckTime; }
		}

		private int downloadTimeout;
		public int DownloadTimeout
		{
			get { return downloadTimeout; }
		}
		
		private int serverTimeout;
		public int ServerTimeout
		{
			get { return serverTimeout; }
		}

		private int reconnectWaitTime;
		public int ReconnectWaitTime
		{
			get { return reconnectWaitTime; }
		}

		private int reconnectWaitTimeLong;
		public int ReconnectWaitTimeLong
		{
			get { return reconnectWaitTimeLong; }
		}

		private int mutliDownloadMinimumTime;
		public int MutliDownloadMinimumTime
		{
			get { return mutliDownloadMinimumTime; }
		}

		private long timerSleepTime;
		public long TimerSleepTime
		{
			get { return timerSleepTime; }
		}

		private string parsingErrorFile;
		public string ParsingErrorFile
		{
			get { return parsingErrorFile; }
		}

		private string dataBinary;
		public string DataBinary
		{
			get { return dataBinary; }
		}

		private string filesBinary;
		public string FilesBinary
		{
			get { return filesBinary; }
		}

		private string searchesBinary;
		public string SearchesBinary
		{
			get { return searchesBinary; }
		}

		private int maxNoDataReceived;
		public int MaxNoDataReceived
		{
			get { return maxNoDataReceived; }
		}

		private int backupStatisticTime;
		public int BackupStatisticTime
		{
			get { return backupStatisticTime; }
		}

		#endregion

		#region PUBLIC

		private string iRCName;
		public string IRCName
		{
			get { return iRCName; }
			set { iRCName = value; }
		}

		private string tempPath;
		public string TempPath
		{
			get { return tempPath; }
			set { tempPath = value; }
		}

		private string readyPath;
		public string ReadyPath
		{
			get { return readyPath; }
			set { readyPath = value; }
		}

		private string ircRegisterPasswort;
		public string IrcRegisterPasswort
		{
			get { return ircRegisterPasswort; }
			set { ircRegisterPasswort = value; }
		}

		private string ircRegisterEmail;
		public string IrcRegisterEmail
		{
			get { return ircRegisterEmail; }
			set { ircRegisterEmail = value; }
		}

		private bool enableMultiDownloads;
		public bool EnableMultiDownloads
		{
			get { return enableMultiDownloads; }
			set { enableMultiDownloads = value; }
		}

		private bool clearReadyDownloads;
		public bool ClearReadyDownloads
		{
			get { return clearReadyDownloads; }
			set { clearReadyDownloads = value; }
		}

		private int tcpServerPort;
		public int TcpServerPort
		{
			get { return tcpServerPort; }
			set { tcpServerPort = value; }
		}

		private int webServerPort;
		public int WebServerPort
		{
			get { return webServerPort; }
			set { webServerPort = value; }
		}

		private string password;
		public string Password
		{
			get { return password; }
			set { password = value; }
		}

		private long backupDataTime;
		public long BackupDataTime
		{
			get { return backupDataTime; }
			set { backupDataTime = value; }
		}

		private string[] fileHandler;
		public string[] FileHandler
		{
			get { return fileHandler; }
			set { fileHandler = value; }
		}

		private bool startTCPServer;
		public bool StartTCPServer
		{
			get { return startTCPServer; }
			set { startTCPServer = value; }
		}

		private bool startWebServer;
		public bool StartWebServer
		{
			get { return startWebServer; }
			set { startWebServer = value; }
		}

		private string styleWebServer;
		public string StyleWebServer
		{
			get { return styleWebServer; }
			set { styleWebServer = value; }
		}

		private bool startJabberClient;
		public bool StartJabberClient
		{
			get { return startJabberClient; }
			set { startJabberClient = value; }
		}

		private string jabberServer;
		public string JabberServer
		{
			get { return jabberServer; }
			set { jabberServer = value; }
		}

		private string jabberUser;
		public string JabberUser
		{
			get { return jabberUser; }
			set { jabberUser = value; }
		}

		private string jabberPassword;
		public string JabberPassword
		{
			get { return jabberPassword; }
			set { jabberPassword = value; }
		}

		private bool autoJoinOnInvite;
		public bool AutoJoinOnInvite
		{
			get { return autoJoinOnInvite; }
			set { autoJoinOnInvite = value; }
		}

		private LogLevel logLevel;
		public LogLevel LogLevel
		{
			get { return logLevel; }
			set { logLevel = value; }
		}

		#endregion
	}
}
