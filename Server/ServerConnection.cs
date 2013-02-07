// 
//  ServerConnection.cs
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
using System.Reflection;

using XG.Core;
using XG.Server.Connection;
using XG.Server.Irc;

using log4net;

namespace XG.Server
{
	public delegate void ServerSocketErrorDelegate(Core.Server aServer, SocketErrorCode aValue);

	/// <summary>
	/// 	This class describes the connection to a single irc server
	/// 	it does the following things
	/// 	- creating and removing bots on the fly
	/// 	- creating and removing packets on the fly (if the bot posts them into the channel)
	/// 	- communicate with the bot to handle downloads
	/// </summary>
	public class ServerConnection : AIrcConnection
	{
		#region VARIABLES

		ILog _log;

		Core.Server _server;

		public Core.Server Server
		{
			set
			{
				if (_server != null)
				{
					_server.Added -= ObjectAdded;
					_server.Removed -= ObjectRemoved;
					_server.EnabledChanged -= EnabledChanged;
				}
				_server = value;
				if (_server != null)
				{
					_server.Added += ObjectAdded;
					_server.Removed += ObjectRemoved;
					_server.EnabledChanged += EnabledChanged;

					_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + _server.Name + ")");
				}
			}
		}

		Parser _ircParser;

		public Parser IrcParser
		{
			set
			{
				if (_ircParser != null)
				{
					_ircParser.SendData -= IrcParserSendData;
					_ircParser.JoinChannel -= IrcParserJoinChannel;
					_ircParser.CreateTimer -= IrcParserCreateTimer;

					_ircParser.RequestFromBot -= IrcParserRequestFromBot;
					_ircParser.UnRequestFromBot -= IrcParserUnRequestFromBot;
				}
				_ircParser = value;
				if (_ircParser != null)
				{
					_ircParser.SendData += IrcParserSendData;
					_ircParser.JoinChannel += IrcParserJoinChannel;
					_ircParser.CreateTimer += IrcParserCreateTimer;

					_ircParser.RequestFromBot += IrcParserRequestFromBot;
					_ircParser.UnRequestFromBot += IrcParserUnRequestFromBot;
				}
			}
		}

		bool _isRunning;

		public bool IsRunning
		{
			get { return _isRunning; }
		}

		readonly Dictionary<AObject, DateTime> _timedObjects = new Dictionary<AObject, DateTime>();
		readonly Dictionary<string, DateTime> _latestPacketRequests = new Dictionary<string, DateTime>();

		#endregion

		#region EVENTS

		public event ServerDelegate Connected;
		public event ServerSocketErrorDelegate Disconnected;

		#endregion

		#region CONNECTION

		protected override void ConnectionConnected()
		{
			SendData("NICK " + Settings.Instance.IrcNick);
			SendData("USER " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcNick + " " + _server.Name + " :root");

			_timedObjects.Clear();
			_latestPacketRequests.Clear();
			_isRunning = true;

			_server.ErrorCode = SocketErrorCode.None;
			_server.Commit();

			Connected(_server);
		}

		protected override void ConnectionDisconnected(SocketErrorCode aValue)
		{
			_isRunning = false;

			_server.ErrorCode = aValue;
			_server.Connected = false;
			_server.Commit();

			_timedObjects.Clear();
			_latestPacketRequests.Clear();

			Disconnected(_server, aValue);
		}

		void SendData(string aData)
		{
			if (Connection != null)
			{
				Connection.SendData(aData);
			}
		}

		protected override void ConnectionDataReceived(string aData)
		{
			_log.Debug("con_DataReceived(" + aData + ")");

			_ircParser.ParseData(_server, aData);
		}

		#endregion

		#region BOT

		void RequestFromBot(Bot aBot)
		{
			if (aBot != null)
			{
				if (aBot.State == Bot.States.Idle)
				{
					// check if the packet is already downloaded, or active - than disable it and get the next one
					Packet tPacket = aBot.OldestActivePacket();
					while (tPacket != null)
					{
						Int64 tChunk = FileActions.NextAvailablePartSize(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name,
						                                                 tPacket.RealSize != 0 ? tPacket.RealSize : tPacket.Size);
						if (tChunk == -1)
						{
							_log.Warn("RequestFromBot(" + aBot + ") packet #" + tPacket.Id + " (" + tPacket.Name + ") is already in use");
							tPacket.Enabled = false;
							tPacket.Commit();
							tPacket = aBot.OldestActivePacket();
						}
						else
						{
							string name = Core.Helper.ShrinkFileName(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, 0);
							if (_latestPacketRequests.ContainsKey(name))
							{
								double time = (_latestPacketRequests[name] - DateTime.Now).TotalSeconds;
								if (time > 0)
								{
									_log.Warn("RequestFromBot(" + aBot + ") packet name " + tPacket.Name + " is blocked for " + time + "ms");
									CreateTimer(aBot, (int) time + 1, false);
									return;
								}
							}

							if (_server.Connected)
							{
								_log.Info("RequestFromBot(" + aBot + ") requesting packet #" + tPacket.Id + " (" + tPacket.Name + ")");
								SendData("PRIVMSG " + aBot.Name + " :\u0001XDCC SEND " + tPacket.Id + "\u0001");

								if (_latestPacketRequests.ContainsKey(name))
								{
									_latestPacketRequests.Remove(name);
								}
								_latestPacketRequests.Add(name, DateTime.Now.AddSeconds(Settings.Instance.SamePacketRequestTime));

								// statistics
								Statistic.Instance.Increase(StatisticType.PacketsRequested);
							}

							// create a timer to re request if the bot didnt recognized the privmsg
							CreateTimer(aBot, Settings.Instance.BotWaitTime, false);
							break;
						}
					}
				}
			}
		}

		void UnRequestFromBot(Bot aBot)
		{
			if (aBot != null) // && myServer[aBot.Name] != null)
			{
				_log.Info("UnregisterFromBot(" + aBot + ")");
				SendData("PRIVMSG " + aBot.Name + " :\u0001XDCC REMOVE\u0001");
				CreateTimer(aBot, Settings.Instance.CommandWaitTime, false);

				// statistics
				Statistic.Instance.Increase(StatisticType.PacketsRemoved);
			}
		}

		#endregion

		#region CHANNEL

		void JoinChannel(Channel aChan)
		{
			// only join if the channel isnt connected
			if (aChan != null && _server.Channel(aChan.Name) != null && !aChan.Connected)
			{
				_log.Info("JoinChannel(" + aChan + ")");
				SendData("JOIN " + aChan.Name);

				// TODO maybe set a time to resend the command if the channel is not connected
				// it happend to me, that some available channels werent joined because no confirm messaes appeared

				// statistics
				Statistic.Instance.Increase(StatisticType.ChannelsJoined);
			}
		}

		void PartChannel(Channel aChan)
		{
			if (aChan != null)
			{
				_log.Info("PartChannel(" + aChan + ")");
				SendData("PART " + aChan.Name);

				// statistics
				Statistic.Instance.Increase(StatisticType.ChannelsParted);
			}
		}

		#endregion

		#region EVENTHANDLER

		void ObjectAdded(AObject aParent, AObject aObj)
		{
			if (aObj is Channel)
			{
				var aChan = aObj as Channel;

				if (aChan.Enabled)
				{
					JoinChannel(aChan);
				}
			}
		}

		void ObjectRemoved(AObject aParent, AObject aObj)
		{
			if (aObj is Channel)
			{
				var aChan = aObj as Channel;

				foreach (Bot tBot in aChan.Bots)
				{
					foreach (Packet tPack in tBot.Packets)
					{
						tPack.Enabled = false;
						tPack.Commit();
					}
				}

				PartChannel(aChan);
			}
		}

		void EnabledChanged(AObject aObj)
		{
			if (aObj is Channel)
			{
				var tChan = aObj as Channel;

				if (tChan.Enabled)
				{
					JoinChannel(tChan);
				}
				else
				{
					PartChannel(tChan);
				}
			}

			if (aObj is Packet)
			{
				var tPack = aObj as Packet;
				Bot tBot = tPack.Parent;

				if (tPack.Enabled)
				{
					if (tBot.OldestActivePacket() == tPack)
					{
						RequestFromBot(tBot);
					}
				}
				else
				{
					if (tBot.State == Bot.States.Waiting || tBot.State == Bot.States.Active)
					{
						Packet tmp = tBot.CurrentQueuedPacket;
						if (tmp == tPack)
						{
							UnRequestFromBot(tBot);
						}
					}
				}
			}
		}

		void IrcParserSendData(Core.Server aServer, string aData)
		{
			if (_server == aServer)
			{
				SendData(aData);
			}
		}

		void IrcParserJoinChannel(Core.Server aServer, Channel aChannel)
		{
			if (_server == aServer)
			{
				JoinChannel(aChannel);
			}
		}

		void IrcParserCreateTimer(Core.Server aServer, AObject aObject, int aTime, bool aOverride)
		{
			if (_server == aServer)
			{
				CreateTimer(aObject, aTime, aOverride);
			}
		}

		void IrcParserRequestFromBot(Core.Server aServer, Bot aBot)
		{
			if (_server == aServer)
			{
				RequestFromBot(aBot);
			}
		}

		void IrcParserUnRequestFromBot(Core.Server aServer, Bot aBot)
		{
			if (_server == aServer)
			{
				UnRequestFromBot(aBot);
			}
		}

		#endregion

		#region TIMER

		/// <summary>
		/// 	Is called from the parent onject ServerHandler (to have a single loop which triggers all ServerConnects)
		/// </summary>
		public void TriggerTimerRun()
		{
			var remove = new List<AObject>();
			foreach (var kvp in _timedObjects)
			{
				DateTime time = kvp.Value;
				if ((time - DateTime.Now).TotalSeconds < 0)
				{
					remove.Add(kvp.Key);
				}
			}
			foreach (AObject obj in remove)
			{
				_timedObjects.Remove(obj);

				if (obj is Channel)
				{
					JoinChannel(obj as Channel);
				}
				else if (obj is Bot)
				{
					RequestFromBot(obj as Bot);
				}
			}

			//SendData("PING " + myServer.Name);
		}

		public void CreateTimer(AObject aObject, int aTime, bool aOverride)
		{
			if (aObject == null)
			{
				_log.Fatal("CreateTimer(null, " + aTime + ", " + aOverride + ") object is null!");
				return;
			}
			if (aOverride && _timedObjects.ContainsKey(aObject))
			{
				_timedObjects.Remove(aObject);
			}

			if (!_timedObjects.ContainsKey(aObject))
			{
				_timedObjects.Add(aObject, DateTime.Now.AddSeconds(aTime));
			}
		}

		#endregion
	}
}
