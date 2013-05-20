// 
//  Servers.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

using XG.Core;
using XG.Server.Helper;
using XG.Server.Irc;

using log4net;

namespace XG.Server
{
	public delegate void DownloadDelegate(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort);

	/// <summary>
	/// 	This class describes a irc server connection handler
	/// 	it does the following things
	/// 	- connect to or disconnect from an irc server
	/// 	- handling of global bot downloads
	/// 	- splitting and merging the files to download
	/// 	- writing files to disk
	/// 	- timering some clean up tasks
	/// </summary>
	public class Servers : ANotificationSender
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		Parser _ircParser;

		public Parser IrcParser
		{
			set
			{
				if (_ircParser != null)
				{
					_ircParser.AddDownload -= BotConnect;
					_ircParser.RemoveDownload -= BotDisconnect;
				}
				_ircParser = value;
				if (_ircParser != null)
				{
					_ircParser.AddDownload += BotConnect;
					_ircParser.RemoveDownload += BotDisconnect;
				}
			}
		}

		public FileActions FileActions { get; set; }

		readonly List<ServerConnection> _servers;
		readonly List<BotConnection> _downloads;

		public bool AllowRunning { set; get; }

		#endregion

		#region INIT

		public Servers()
		{
			AllowRunning = true;

			_servers = new List<ServerConnection>();
			_downloads = new List<BotConnection>();

			// create my stuff if its not there
			new DirectoryInfo(Settings.Instance.ReadyPath).Create();
			new DirectoryInfo(Settings.Instance.TempPath).Create();

			// start the timed tasks
			new Thread(RunTimer).Start();
		}

		#endregion

		#region SERVER

		/// <summary>
		/// 	Connects to the given server by using a new ServerConnnect class
		/// </summary>
		/// <param name="aServer"> </param>
		public void ServerConnect(Core.Server aServer)
		{
			if (!aServer.Enabled)
			{
				Log.Error("ConnectServer(" + aServer + ") is not enabled");
				return;
			}

			ServerConnection con = _servers.SingleOrDefault(c => c.Server == aServer);
			if (con == null)
			{
				con = new ServerConnection
				{
					FileActions = FileActions,
					Server = aServer,
					IrcParser = _ircParser,
					Connection = new Connection.Connection {Hostname = aServer.Name, Port = aServer.Port, MaxData = 0}
				};

				_servers.Add(con);

				con.Disconnected += ServerDisconnected;
				con.NotificationAdded += FireNotificationAdded;

				// start a new thread wich connects to the given server
				new Thread(() => con.Connection.Connect()).Start();
			}
			else
			{
				Log.Error("ConnectServer(" + aServer + ") is already in the list");
			}
		}

		/// <summary>
		/// 	Disconnects the given server
		/// </summary>
		/// <param name="aServer"> </param>
		public void ServerDisconnect(Core.Server aServer)
		{
			ServerConnection con = _servers.SingleOrDefault(c => c.Server == aServer);
			if (con != null)
			{
				if (con.Connection != null)
				{
					con.Connection.Disconnect();
				}
			}
			else
			{
				Log.Error("DisconnectServer(" + aServer + ") is not in the list");
			}
		}

		void ServerDisconnected(Core.Server aServer, SocketErrorCode aValue)
		{
			ServerConnection con = _servers.SingleOrDefault(c => c.Server == aServer);
			if (con != null)
			{
				if (aServer.Enabled)
				{
					int time = Settings.Instance.ReconnectWaitTime;
					switch (aValue)
					{
						case SocketErrorCode.HostIsDown:
						case SocketErrorCode.HostUnreachable:
						case SocketErrorCode.ConnectionTimedOut:
						case SocketErrorCode.ConnectionRefused:
							time = Settings.Instance.ReconnectWaitTimeLong;
							break;
					}
					new Timer(ServerReconnect, aServer, time * 1000, Timeout.Infinite);
				}
				else
				{
					con.Disconnected -= ServerDisconnected;
					con.NotificationAdded -= FireNotificationAdded;

					con.Server = null;
					con.IrcParser = null;

					_servers.Remove(con);
				}

				con.Connection = null;
			}
			else
			{
				Log.Error("ServerConnectionDisconnected(" + aServer + ", " + aValue + ") is not in the list");
			}
		}

		void ServerReconnect(object aServer)
		{
			var tServer = aServer as Core.Server;

			ServerConnection con = _servers.SingleOrDefault(c => c.Server == aServer);
			if (con != null)
			{
				if (tServer.Enabled)
				{
					Log.Info("ReconnectServer(" + tServer + ")");

					// TODO do we need a new connection here?
					con.Connection = new Connection.Connection {Hostname = tServer.Name, Port = tServer.Port, MaxData = 0};

					con.Connection.Connect();
				}
				else
				{
					ServerDisconnected(tServer, SocketErrorCode.NetworkReset);
				}
			}
			else if (tServer != null)
			{
				Log.Error("ReconnectServer(" + tServer + ") is not in the list");
			}
		}

		#endregion

		#region BOT

		/// <summary>
		/// </summary>
		/// <param name="aPack"> </param>
		/// <param name="aChunk"> </param>
		/// <param name="aIp"> </param>
		/// <param name="aPort"> </param>
		void BotConnect(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort)
		{
			BotConnection con = _downloads.SingleOrDefault(c => c.Packet == aPack);
			if (con == null)
			{
				new Thread(() =>
				{
					con = new BotConnection
					{
						FileActions = FileActions,
						Packet = aPack,
						StartSize = aChunk,
						Connection = new Connection.Connection {Hostname = aIp.ToString(), Port = aPort, MaxData = aPack.RealSize - aChunk}
					};

					con.Disconnected += BotDisconnected;
					con.NotificationAdded += FireNotificationAdded;

					_downloads.Add(con);
					con.Connection.Connect();
				}).Start();
			}
			else
			{
				// uhh - that should not happen
				Log.Error("IrcParserAddDownload(" + aPack + ") is already downloading");
			}
		}

		void BotDisconnect(Bot aBot)
		{
			BotConnection con = _downloads.SingleOrDefault(c => c.Packet.Parent == aBot);
			if (con != null)
			{
				con.Connection.Disconnect();
			}
		}

		void BotDisconnected(Packet aPacket)
		{

			BotConnection con = _downloads.SingleOrDefault(c => c.Packet == aPacket);
			if (con != null)
			{
				con.Packet = null;
				con.Connection = null;

				con.Disconnected -= BotDisconnected;
				con.NotificationAdded -= FireNotificationAdded;
				_downloads.Remove(con);

				try
				{
					// if the connection never connected, there will be no part!
					// and if we manually killed stopped the packet there will be no parent of the part
					if (con.Part != null && con.Part.Parent != null)
					{
						// do this here because the bothandler sets the part state and after this we can check the file
						FileActions.CheckFile(con.Part.Parent);
					}
				}
				catch (Exception ex)
				{
					Log.Fatal("bot_Disconnected()", ex);
				}

				try
				{
					ServerConnection scon = _servers.SingleOrDefault(c => c.Server == aPacket.Parent.Parent.Parent);
					if (scon != null)
					{
						scon.CreateTimer(aPacket.Parent, Settings.Instance.CommandWaitTime, false);
					}
				}
				catch (Exception ex)
				{
					Log.Fatal("bot_Disconnected() request", ex);
				}
			}
		}

		#endregion

		#region TIMER TASKS

		void RunTimer()
		{
			DateTime _last = DateTime.Now;
			while (AllowRunning)
			{
				if (_last.AddSeconds(Settings.Instance.RunLoopTime) < DateTime.Now)
				{
					foreach (var con in _servers.ToArray())
					{
						if (con.IsRunning)
						{
							con.TriggerTimerRun();
						}
					}
				}

				Thread.Sleep(500);
			}
		}

		#endregion
	}
}
