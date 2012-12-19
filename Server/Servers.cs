// 
//  Servers.cs
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
	public class Servers
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

		readonly Dictionary<Core.Server, ServerConnection> _servers;
		readonly Dictionary<Packet, BotConnection> _downloads;

		public bool AllowRunning { set; get; }

		#endregion

		#region INIT

		public Servers()
		{
			AllowRunning = true;

			_servers = new Dictionary<Core.Server, ServerConnection>();
			_downloads = new Dictionary<Packet, BotConnection>();

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
			if (!_servers.ContainsKey(aServer))
			{
				var con = new ServerConnection
				{
					FileActions = FileActions,
					Server = aServer,
					IrcParser = _ircParser,
					Connection = new Connection.Connection {Hostname = aServer.Name, Port = aServer.Port, MaxData = 0}
				};

				_servers.Add(aServer, con);

				con.Connected += ServerConnected;
				con.Disconnected += ServerDisconnected;

				// start a new thread wich connects to the given server
				new Thread(() => con.Connection.Connect()).Start();
			}
			else
			{
				Log.Error("ConnectServer(" + aServer + ") is already in the dictionary");
			}
		}

		void ServerConnected(Core.Server aServer)
		{
			// nom nom nom ...
		}

		/// <summary>
		/// 	Disconnects the given server
		/// </summary>
		/// <param name="aServer"> </param>
		public void ServerDisconnect(Core.Server aServer)
		{
			if (_servers.ContainsKey(aServer))
			{
				ServerConnection con = _servers[aServer];

				if (con.Connection != null)
				{
					con.Connection.Disconnect();
				}
			}
			else
			{
				Log.Error("DisconnectServer(" + aServer + ") is not in the dictionary");
			}
		}

		void ServerDisconnected(Core.Server aServer, SocketErrorCode aValue)
		{
			if (_servers.ContainsKey(aServer))
			{
				ServerConnection con = _servers[aServer];

				if (aServer.Enabled)
				{
					// disable the server if the host was not found
					// this is also triggered if we have no internet connection and disables all channels
					/*if(	aValue == SocketErrorCode.HostNotFound ||
						aValue == SocketErrorCode.HostNotFoundTryAgain)
					{
						aServer.Enabled = false;
					}
					else*/
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
								//							case SocketErrorCode.HostNotFound:
								//							case SocketErrorCode.HostNotFoundTryAgain:
								//								time = Settings.Instance.ReconnectWaitTimeReallyLong;
								//								break;
						}
						new Timer(ServerReconnect, aServer, time * 1000, Timeout.Infinite);
					}
				}
				else
				{
					con.Connected -= ServerConnected;
					con.Disconnected -= ServerDisconnected;

					con.Server = null;
					con.IrcParser = null;

					_servers.Remove(aServer);
				}

				con.Connection = null;
			}
			else
			{
				Log.Error("ServerConnectionDisconnected(" + aServer + ", " + aValue + ") is not in the dictionary");
			}
		}

		void ServerReconnect(object aServer)
		{
			var tServer = aServer as Core.Server;

			if (tServer != null && _servers.ContainsKey(tServer))
			{
				ServerConnection con = _servers[tServer];

				if (tServer.Enabled)
				{
					Log.Error("ReconnectServer(" + tServer + ")");

					// TODO do we need a new connection here?
					con.Connection = new Connection.Connection {Hostname = tServer.Name, Port = tServer.Port, MaxData = 0};

					con.Connection.Connect();
				}
			}
			else if (tServer != null)
			{
				Log.Error("ReconnectServer(" + tServer + ") is not in the dictionary");
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
			if (!_downloads.ContainsKey(aPack))
			{
				new Thread(() =>
				{
					var con = new BotConnection
					{
						FileActions = FileActions,
						Packet = aPack,
						StartSize = aChunk,
						Connection = new Connection.Connection {Hostname = aIp.ToString(), Port = aPort, MaxData = aPack.RealSize - aChunk}
					};

					con.Connected += BotConnected;
					con.Disconnected += BotDisconnected;

					_downloads.Add(aPack, con);
					con.Connection.Connect();
				}).Start();
			}
			else
			{
				// uhh - that should not happen
				Log.Error("IrcParserAddDownload(" + aPack + ") is already downloading");
			}
		}

		void BotConnected(Packet aPack, BotConnection aCon) {}

		void BotDisconnect(Bot aBot)
		{
			foreach (var kvp in _downloads)
			{
				if (kvp.Key.Parent == aBot)
				{
					kvp.Value.Connection.Disconnect();
					break;
				}
			}
		}

		void BotDisconnected(Packet aPacket, BotConnection aCon)
		{
			aCon.Packet = null;
			aCon.Connection = null;

			if (_downloads.ContainsKey(aPacket))
			{
				aCon.Connected -= BotConnected;
				aCon.Disconnected -= BotDisconnected;
				_downloads.Remove(aPacket);

				try
				{
					// if the connection never connected, there will be no part!
					// and if we manually killed stopped the packet there will be no parent of the part
					if (aCon.Part != null && aCon.Part.Parent != null)
					{
						// do this here because the bothandler sets the part state and after this we can check the file
						FileActions.CheckFile(aCon.Part.Parent);
					}
				}
				catch (Exception ex)
				{
					Log.Fatal("bot_Disconnected()", ex);
				}

				try
				{
					ServerConnection sc = _servers[aPacket.Parent.Parent.Parent];
					sc.CreateTimer(aPacket.Parent, Settings.Instance.CommandWaitTime, false);
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
					foreach (var kvp in _servers)
					{
						ServerConnection sc = kvp.Value;
						if (sc.IsRunning)
						{
							sc.TriggerTimerRun();
						}
					}
				}

				Thread.Sleep(500);
			}
		}

		#endregion
	}
}
