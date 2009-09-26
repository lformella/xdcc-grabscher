using System;
using System.IO;
using System.Xml.Serialization;

namespace XG.Server
{
	[Serializable()]
	public class Settings
	{
		private static Settings instance = null;

		public static Settings Instance
		{
			get
			{
				if (instance == null)
				{
					try
					{
						XmlSerializer ser = new XmlSerializer(typeof(Settings));
						StreamReader sr = new StreamReader("./setings.xml");
						instance = (Settings)ser.Deserialize(sr);
						sr.Close();
					}
					catch (Exception) { instance = new Settings(); }
				}
				return instance;
			}
		}

		private Settings()
		{
			this.commandWaitTime = 15000;
			this.botWaitTime = 240000;
			this.channelWaitTime = 300000;
			this.channelWaitTimeLong = 900000;
			this.fileRollback = 512000;
			this.fileRollbackCheck = 409600;
			this.updateDownloadTime = 5000;
			this.downloadPerRead = 102400;
			this.iRCName = "Anonymous";
			this.tempPath = "./tmp/";
			this.readyPath = "./dl/";
			this.mutliDownloadMinimumTime = 300;
			this.timerSleepTime = 10000;
			this.serverTimeout = 60000;
			this.reconnectWaitTime = 45000;
			this.enableMultiDownloads = true;
			this.clearReadyDownloads = true;
			this.ircVersion = "mIRC v6.16 Khaled Mardam-Bey";
			this.ircRegisterEmail = "anon@ymous.org";
			this.ircRegisterPasswort = "password123";
			this.botOfflineCheckTime = 1200000;
			this.downloadTimeout = 30000;
			this.botOfflineTime = 7200000;
			this.samePacketRequestTime = 10000;

			this.port = 5555;
			this.parsingErrorFile = "./parsing_errors.txt";
			this.dataBinary = "./xg.bin";
			this.filesBinary = "./xgfiles.bin";
			this.password = "xgisgreat";
			this.backupDataTime = 900000;
			this.fileHandler = new string[] {""};
			this.startTCPServer = true;
			this.startWebServer = false;
		}
		
		#region PRIVATE

		private long commandWaitTime;
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

		#endregion
		
		#region PUBLIC

		string iRCName;
		public string IRCName 
		{
			get { return "XG-" + iRCName.Replace("XG-", ""); }
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

		int port;
		public int Port
		{
			get { return port; }
			set { port = value; }
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
		
		#endregion
	}
}
