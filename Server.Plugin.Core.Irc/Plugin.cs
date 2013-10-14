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
using System.Reflection;
using System.Threading;

using XG.Core;
using XG.Server.Helper;
using XG.Server.Plugin;

using log4net;
using Meebey.SmartIrc4net;

namespace XG.Server.Plugin.Core.Irc
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(Plugin));

		readonly HashSet<IrcConnection> _connections = new HashSet<IrcConnection>();
		readonly HashSet<BotDownload> _botDownloads = new HashSet<BotDownload>();
		readonly HashSet<Download> _xdccListDownloads = new HashSet<Download>();

		readonly Parser.Parser _parser = new Parser.Parser();

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_parser.FileActions = FileActions;
			_parser.OnAddDownload += BotConnect;
			_parser.OnDownloadXdccList += DownloadXdccList;
			_parser.OnNotificationAdded += AddNotification;
			_parser.OnRemoveDownload += (aSender, aEventArgs) => BotDisconnect(aEventArgs.Value2);
			_parser.Initialize();

			foreach (XG.Core.Server server in Servers.All)
			{
				if (server.Enabled)
				{
					ServerConnect(server);
				}
			}

			DateTime _last = DateTime.Now;
			while (AllowRunning)
			{
				if (_last.AddSeconds(Settings.Instance.RunLoopTime) < DateTime.Now)
				{
					foreach (var connection in _connections.ToArray())
					{
						connection.TriggerTimerRun();
					}
				}

				Thread.Sleep(500);
			}
		}

		protected override void StopRun()
		{
			foreach (var connection in _connections)
			{
				connection.Stop();
			}

			foreach (var download in _botDownloads)
			{
				download.Stop();
			}
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			if (aEventArgs.Value2 is XG.Core.Server)
			{
				var aServer = aEventArgs.Value2 as XG.Core.Server;
				ServerConnect(aServer);
			}
		}

		protected override void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			if (aEventArgs.Value2 is XG.Core.Server)
			{
				var aServer = aEventArgs.Value2 as XG.Core.Server;
				aServer.Enabled = false;
				ServerDisconnect(aServer);
			}
		}

		protected override void ObjectEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			if (aEventArgs.Value1 is XG.Core.Server)
			{
				var aServer = aEventArgs.Value1 as XG.Core.Server;

				if (aEventArgs.Value1.Enabled)
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
					Parser = _parser,
					FileActions = FileActions
				};
				_connections.Add(connection);

				connection.OnDisconnected += ServerDisconnected;
				connection.OnNotificationAdded += AddNotification;
				connection.Start(aServer.ToString());
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

		void ServerDisconnected(object aSender, EventArgs<XG.Core.Server> aEventArgs)
		{
			IrcConnection connection = _connections.SingleOrDefault(c => c.Server == aEventArgs.Value1);
			if (connection != null)
			{
				if (!aEventArgs.Value1.Enabled)
				{
					connection.OnDisconnected -= ServerDisconnected;
					connection.OnNotificationAdded -= AddNotification;

					connection.Server = null;
					connection.Parser = null;
					connection.FileActions = null;

					_connections.Remove(connection);
				}
			}
			else
			{
				_log.Error("ServerDisconnected(" + aEventArgs.Value1 + ") is not in the list");
			}
		}

		#endregion

		#region BOT

		void BotConnect(object aSender, EventArgs<Packet, Int64, IPAddress, int> aEventArgs)
		{
			var download = _botDownloads.SingleOrDefault(c => c.Packet == aEventArgs.Value1);
			if (download == null)
			{
				download = new BotDownload
				{
					FileActions = FileActions,
					Packet = aEventArgs.Value1,
					StartSize = aEventArgs.Value2,
					IP = aEventArgs.Value3,
					Port = aEventArgs.Value4,
					MaxData = aEventArgs.Value1.RealSize - aEventArgs.Value2
				};

				download.OnDisconnected += BotDisconnected;
				download.OnNotificationAdded += AddNotification;

				_botDownloads.Add(download);
				download.Start(aEventArgs.Value3 + ":" + aEventArgs.Value4);
			}
			else
			{
				// uhh - that should not happen
				_log.Error("BotConnect(" + aEventArgs.Value1 + ") is already downloading");
			}
		}

		void BotDisconnect(Bot aBot)
		{
			var download = _botDownloads.SingleOrDefault(c => c.Packet.Parent == aBot);
			if (download != null)
			{
				download.Stop();
			}
		}

		void BotDisconnected(object aSender, EventArgs<Packet> aEventArgs)
		{
			var download = _botDownloads.SingleOrDefault(c => c.Packet == aEventArgs.Value1);
			if (download != null)
			{
				download.Packet = null;

				download.OnDisconnected -= BotDisconnected;
				download.OnNotificationAdded -= AddNotification;
				_botDownloads.Remove(download);

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
					IrcConnection connection = _connections.SingleOrDefault(c => c.Server == aEventArgs.Value1.Parent.Parent.Parent);
					if (connection != null)
					{
						connection.AddBotToQueue(aEventArgs.Value1.Parent, Settings.Instance.CommandWaitTime);
					}
				}
				catch (Exception ex)
				{
					_log.Fatal("BotDisconnected() request", ex);
				}
			}
		}

		#endregion

		#region XDCC List Download

		void DownloadXdccList(object aSender, EventArgs<XG.Core.Server, string, Int64, IPAddress, int> aEventArgs)
		{
			var download = _xdccListDownloads.SingleOrDefault(c => c.Bot == aEventArgs.Value2);
			if (download == null)
			{
				download = new Download
				{
					FileActions = FileActions,
					Server = aEventArgs.Value1,
					Bot = aEventArgs.Value2,
					Size = aEventArgs.Value3,
					IP = aEventArgs.Value4,
					Port = aEventArgs.Value5,
					FileName = CalculateXdccListFileName(aEventArgs.Value1, aEventArgs.Value2)
				};

				download.OnDisconnected += DownloadXdccDisconnected;
				download.OnReady += DownloadXdccReady;

				_xdccListDownloads.Add(download);
				download.Start(aEventArgs.Value4 + ":" + aEventArgs.Value5);
			}
			else
			{
				// uhh - that should not happen
				_log.Error("DownloadXdccList(" + aEventArgs.Value2 + ") is already downloading");
			}
		}

		void DownloadXdccDisconnected(object aSender, EventArgs<XG.Core.Server, string> aEventArgs)
		{
			var download = _xdccListDownloads.SingleOrDefault(c => c.Bot == aEventArgs.Value2);
			if (download != null)
			{
				download.OnDisconnected -= DownloadXdccDisconnected;
				download.OnReady -= DownloadXdccReady;

				_xdccListDownloads.Remove(download);
			}
		}

		void DownloadXdccReady(object aSender, EventArgs<XG.Core.Server, string> aEventArgs)
		{
			string file = CalculateXdccListFileName(aEventArgs.Value1, aEventArgs.Value2);
			var lines = System.IO.File.ReadAllLines(file);
			FileSystem.DeleteFile(file);

			var connection = _connections.FirstOrDefault(c => c.Server == aEventArgs.Value1);
			if (connection != null)
			{
				XG.Core.Channel tChan = null;
				var user = connection.Client.GetIrcUser(aEventArgs.Value2);
				if (user != null)
				{
					foreach (string channel in user.JoinedChannels)
					{
						tChan = aEventArgs.Value1.Channel(channel);
						if (tChan != null)
						{
							break;
						}
					}
				}

				if (tChan == null)
				{
					_log.Error(".DownloadXdccReady(" + aEventArgs.Value2 + ") cant find channel");
				}

				foreach (var line in lines)
				{
					IrcMessageData data = new IrcMessageData(connection.Client, "", aEventArgs.Value2, "", "", tChan.Name, line, line, ReceiveType.QueryNotice, ReplyCode.Null);

					// damn internal contructors...
					// uhh, this is evil - dont try this @ home kids!
					IrcEventArgs args = (IrcEventArgs)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(IrcEventArgs));
					FieldInfo[] EventFields = typeof(IrcEventArgs).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					EventFields[0].SetValue(args, data);

					_parser.Parse(connection, args);
				}
			}
			else
			{
				_log.Error(".DownloadXdccReady(" + aEventArgs.Value2 + ") cant find connection");
			}
		}

		string CalculateXdccListFileName(XG.Core.Server aServer, string aBot)
		{
			return Settings.Instance.TempPath + aServer.Name + "." + aBot;
		}

		#endregion

		#region Functions

		void AddNotification(object aSender, EventArgs<Notification> aEventArgs)
		{
			Notifications.Add(aEventArgs.Value1);
		}

		#endregion
	}
}
