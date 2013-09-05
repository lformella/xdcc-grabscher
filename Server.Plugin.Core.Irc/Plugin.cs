// 
//  Plugin.cs
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
using System.Linq;
using System.Net;
using System.Threading;

using log4net;

using XG.Core;
using XG.Server.Plugin;

namespace XG.Server.Plugin.Core.Irc
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(Plugin));

		readonly HashSet<IrcConnection> _connections = new HashSet<IrcConnection>();
		readonly HashSet<Download> _downloads = new HashSet<Download>();
		readonly XdccListWatchdog _listWatchdog = new XdccListWatchdog();

		readonly Parser.Notice _noticeParser = new Parser.Notice();
		readonly Parser.Message _messageParser = new Parser.Message();
		readonly Parser.Ctcp _ctcpParser = new Parser.Ctcp();
		readonly Parser.Nickserv _nickservParser = new Parser.Nickserv();

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_ctcpParser.FileActions = FileActions;

			_ctcpParser.OnAddDownload += (aPack, aChunk, aIp, aPort) => {
				BotConnect(aPack, aChunk, aIp, aPort);
			};

			_ctcpParser.OnNotificationAdded += (aObj) => {
				AddNotification(aObj);
			};

			_noticeParser.OnRemoveDownload += (aServer, aBot) => {
				BotDisconnect(aBot);
			};

			_messageParser.OnNotificationAdded += (aObj) => {
				AddNotification(aObj);
			};

			foreach (XG.Core.Server server in Servers.All)
			{
				if (server.Enabled)
				{
					ServerConnect(server);
				}
			}

			_listWatchdog.IrcConnections = _connections;
			_listWatchdog.Start();

			DateTime _last = DateTime.Now;
			while (AllowRunning)
			{
				if (_last.AddSeconds(Settings.Instance.RunLoopTime) < DateTime.Now)
				{
					foreach (var server in _connections.ToArray())
					{
						server.TriggerTimerRun();
					}
				}

				Thread.Sleep(500);
			}
		}

		protected override void StopRun()
		{
			_listWatchdog.Stop();

			foreach (var connection in _connections)
			{
				connection.Stop();
			}

			foreach (var download in _downloads)
			{
				download.Stop();
			}
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded(AObject aParent, AObject aObj)
		{
			if (aObj is XG.Core.Server)
			{
				var aServer = aObj as XG.Core.Server;
				ServerConnect(aServer);
			}
		}

		protected override void ObjectRemoved(AObject aParent, AObject aObj)
		{
			if (aObj is XG.Core.Server)
			{
				var aServer = aObj as XG.Core.Server;
				aServer.Enabled = false;
				ServerDisconnect(aServer);
			}
		}

		protected override void ObjectEnabledChanged(AObject aObj)
		{
			if (aObj is XG.Core.Server)
			{
				var aServer = aObj as XG.Core.Server;

				if (aObj.Enabled)
				{
					ServerConnect(aServer);
				}
				else
				{
					ServerDisconnect(aServer);
				}
			}
		}

		#endregion

		#region SERVER

		void ServerConnect(XG.Core.Server aServer)
		{
			if (!aServer.Enabled)
			{
				_log.Error("ServerConnect(" + aServer + ") is not enabled");
				return;
			}

			IrcConnection connection = _connections.SingleOrDefault(c => c.Server == aServer);
			if (connection == null)
			{
				connection = new IrcConnection
				{
					Server = aServer,
					Notice = _noticeParser,
					Message = _messageParser,
					Nickserv =  _nickservParser,
					Ctcp = _ctcpParser,
					FileActions = FileActions
				};
				_connections.Add(connection);

				connection.OnDisconnected += ServerDisconnected;
				connection.OnNotificationAdded += AddNotification;
				connection.Start();
			}
			else
			{
				_log.Error("ConnectServer(" + aServer + ") is already in the list");
			}
		}

		void ServerDisconnect(XG.Core.Server aServer)
		{
			IrcConnection connection = _connections.SingleOrDefault(c => c.Server == aServer);
			if (connection != null)
			{
				connection.Stop();
			}
			else
			{
				_log.Error("DisconnectServer(" + aServer + ") is not in the list");
			}
		}

		void ServerDisconnected(XG.Core.Server aServer)
		{
			IrcConnection connection = _connections.SingleOrDefault(c => c.Server == aServer);
			if (connection != null)
			{
				if (!aServer.Enabled)
				{
					connection.OnDisconnected -= ServerDisconnected;
					connection.OnNotificationAdded -= AddNotification;

					connection.Server = null;
					connection.Notice = null;
					connection.Message = null;
					connection.Nickserv = null;
					connection.Ctcp = null;
					connection.FileActions = null;

					_connections.Remove(connection);
				}
			}
			else
			{
				_log.Error("ServerDisconnected(" + aServer + ") is not in the list");
			}
		}

		#endregion

		#region BOT

		void BotConnect(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort)
		{
			Download download = _downloads.SingleOrDefault(c => c.Packet == aPack);
			if (download == null)
			{
				download = new Download
				{
					FileActions = FileActions,
					Packet = aPack,
					StartSize = aChunk,
					Hostname = aIp.ToString(),
					Port = aPort,
					MaxData = aPack.RealSize - aChunk
				};

				download.OnDisconnected += BotDisconnected;
				download.OnNotificationAdded += AddNotification;

				_downloads.Add(download);
				download.Start();
			}
			else
			{
				// uhh - that should not happen
				_log.Error("BotConnect(" + aPack + ") is already downloading");
			}
		}

		void BotDisconnect(Bot aBot)
		{
			Download download = _downloads.SingleOrDefault(c => c.Packet.Parent == aBot);
			if (download != null)
			{
				download.Stop();
			}
		}

		void BotDisconnected(Packet aPacket)
		{
			Download download = _downloads.SingleOrDefault(c => c.Packet == aPacket);
			if (download != null)
			{
				download.Packet = null;

				download.OnDisconnected -= BotDisconnected;
				download.OnNotificationAdded -= AddNotification;
				_downloads.Remove(download);

				try
				{
					// if the connection never connected, there will be no part!
					// and if we manually killed stopped the packet there will be no parent of the part
					if (download.Part != null && download.Part.Parent != null)
					{
						// do this here because the bothandler sets the part state and after this we can check the file
						FileActions.CheckFile(download.Part.Parent);
					}
				}
				catch (Exception ex)
				{
					_log.Fatal("BotDisconnected()", ex);
				}

				try
				{
					IrcConnection connection = _connections.SingleOrDefault(c => c.Server == aPacket.Parent.Parent.Parent);
					if (connection != null)
					{
						connection.AddBotToQueue(aPacket.Parent, Settings.Instance.CommandWaitTime);
					}
				}
				catch (Exception ex)
				{
					_log.Fatal("BotDisconnected() request", ex);
				}
			}
		}

		#endregion

		#region Functions

		void AddNotification (Notification aNotification)
		{
			Notifications.Add(aNotification);
		}

		#endregion
	}
}
