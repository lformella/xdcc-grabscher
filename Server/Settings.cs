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
using System.Reflection;
using System.Xml.Serialization;

using XG.Server.Helper;

using log4net;

namespace XG.Server
{
	[Serializable]
	public class Settings
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		static Settings _instance;

		/// <value> Returns an instance of the settings - just loaded once </value>
		public static Settings Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Deserialize();
					if (_instance == null)
					{
						_instance = new Settings();
						Serialize();
					}
				}
				return _instance;
			}
		}

		/// <value> Returns an instance of the settings - loaded new every time you request it </value>
		public static Settings InstanceReload
		{
			get
			{
				_instance = Deserialize();
				if (_instance == null)
				{
					_instance = new Settings();
					Serialize();
				}
				return _instance;
			}
		}

		/// <summary>
		/// 	Deserializes a previously saved settings object from the file named setings.xml
		/// </summary>
		/// <returns> A <see cref="Settings" /> </returns>
		static Settings Deserialize()
		{
			if (File.Exists(AppDataPathStatic + "settings.xml"))
			{
				try
				{
					var ser = new XmlSerializer(typeof (Settings));
					var sr = new StreamReader(AppDataPathStatic + "settings.xml");
					var settings = (Settings) ser.Deserialize(sr);
					sr.Close();
					return settings;
				}
				catch (Exception ex)
				{
					Log.Fatal("Settings.Deserialize", ex);
				}
			}
			else
			{
				Log.Error("Settings.Deserialize found no settings file");
			}
			return null;
		}

		static void Serialize()
		{
			try
			{
				var ser = new XmlSerializer(typeof (Settings));
				var sw = new StreamWriter(AppDataPathStatic + "settings.xml");
				ser.Serialize(sw, _instance);
				sw.Close();
			}
			catch (Exception ex)
			{
				Log.Fatal("Settings.Serialize", ex);
			}
		}

		Settings()
		{
			IrcNick = "Anonymous" + new Random().Next(10000, 99999);
			IrcPasswort = "password123";
			IrcRegisterEmail = "anon@ymous.org";
			AutoRegisterNickserv = false;
			AutoJoinOnInvite = true;

			TempPath = AppDataPath + "tmp" + Path.DirectorySeparatorChar;
			ReadyPath = AppDataPath + "dl" + Path.DirectorySeparatorChar;
			EnableMultiDownloads = false;
			ClearReadyDownloads = true;

			Password = "xgisgreat";

			UseWebServer = true;
			WebServerPort = 5556;

			UseJabberClient = false;
			JabberServer = "";
			JabberUser = "";
			JabberPassword = "";

			UseMySqlBackend = false;
			MySqlBackendServer = "127.0.0.1";
			MySqlBackendPort = 3306;
			MySqlBackendDatabase = "xg";
			MySqlBackendUser = "xg";
			MySqlBackendPassword = "xg";

			FileHandlers = new FileHandler[0];
		}

		#region PRIVATE

		public string AppDataPath
		{
			get
			{
				string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				return folder + (folder.EndsWith("" + Path.DirectorySeparatorChar) ? "" : "" + Path.DirectorySeparatorChar) + "XG" + Path.DirectorySeparatorChar;
			}
		}

		public static string AppDataPathStatic
		{
			get
			{
				string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				return folder + (folder.EndsWith("" + Path.DirectorySeparatorChar) ? "" : "" + Path.DirectorySeparatorChar) + "XG" + Path.DirectorySeparatorChar;
			}
		}

		public string XgVersion
		{
			get { return "1.0.0.0"; }
		}

		public int CommandWaitTime
		{
			get { return 15; }
		}

		public int BotWaitTime
		{
			get { return 240; }
		}

		public int ChannelWaitTime
		{
			get { return 300; }
		}

		public int ChannelWaitTimeLong
		{
			get { return 900; }
		}

		public int FileRollbackBytes
		{
			get { return 512000; }
		}

		public int FileRollbackCheckBytes
		{
			get { return 409600; }
		}

		public int UpdateDownloadTime
		{
			get { return 3; }
		}

		public int DownloadPerReadBytes
		{
			get { return 102400; }
		}

		public int BotOfflineTime
		{
			get { return 7200; }
		}

		public int SamePacketRequestTime
		{
			get { return 10; }
		}

		public string IrcVersion
		{
			get { return "mIRC v6.35 Khaled Mardam-Bey"; }
		}

		public int BotOfflineCheckTime
		{
			get { return 1200; }
		}

		public int DownloadTimeoutTime
		{
			get { return 30; }
		}

		public int ServerTimeoutTime
		{
			get { return 60; }
		}

		public int ReconnectWaitTime
		{
			get { return 45; }
		}

		public int ReconnectWaitTimeLong
		{
			get { return 900; }
		}

		public int MutliDownloadMinimumTime
		{
			get { return 300; }
		}

		public int RunLoopTime
		{
			get { return 5; }
		}

		public long TakeSnapshotTime
		{
			get { return 600; }
		}

		public string ParsingErrorFile
		{
			get { return AppDataPath + "parsing_errors.txt"; }
		}

		public int MaxNoDataReceived
		{
			get { return 50; }
		}

		public int BackupStatisticTime
		{
			get { return 60; }
		}

		#endregion

		#region PUBLIC

		public string IrcNick { get; set; }
		public string IrcPasswort { get; set; }
		public string IrcRegisterEmail { get; set; }
		public bool AutoRegisterNickserv { get; set; }
		public bool AutoJoinOnInvite { get; set; }

		string _tempPath;

		public string TempPath
		{
			get { return _tempPath; }
			set
			{
				_tempPath = value;
				if (!_tempPath.EndsWith("" + Path.DirectorySeparatorChar))
				{
					_tempPath += Path.DirectorySeparatorChar;
				}
			}
		}

		string _readPath;
		public string ReadyPath
		{
			get { return _readPath; }
			set
			{
				_readPath = value;
				if (!_readPath.EndsWith("" + Path.DirectorySeparatorChar))
				{
					_readPath += Path.DirectorySeparatorChar;
				}
			}
		}

		public bool EnableMultiDownloads { get; set; }
		public bool ClearReadyDownloads { get; set; }

		public string Password { get; set; }

		public bool UseWebServer { get; set; }
		public int WebServerPort { get; set; }

		public bool UseJabberClient { get; set; }
		public string JabberServer { get; set; }
		public string JabberUser { get; set; }
		public string JabberPassword { get; set; }

		public bool UseMySqlBackend { get; set; }
		public string MySqlBackendServer { get; set; }
		public int MySqlBackendPort { get; set; }
		public string MySqlBackendDatabase { get; set; }
		public string MySqlBackendUser { get; set; }
		public string MySqlBackendPassword { get; set; }

		public FileHandler[] FileHandlers { get; set; }

		#endregion
	}
}
