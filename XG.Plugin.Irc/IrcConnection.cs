//
//  IrcServer.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
//
//  Copyright (c) 2013 Lars Formella
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
using System.Reflection;
using log4net;
using Meebey.SmartIrc4net;
using XG.Model.Domain;
using XG.Config.Properties;
using XG.Business.Helper;

namespace XG.Plugin.Irc
{
	public class IrcConnection : Connection
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		XdccClient _client;
		
		readonly TimedList<Bot> _botQueue = new TimedList<Bot>();
		readonly TimedList<Model.Domain.Channel> _channelQueue = new TimedList<Model.Domain.Channel>();
		readonly TimedList<string> _latestPacketRequests = new TimedList<string>();
		readonly List<XdccListEntry> _xdccListQueue = new List<XdccListEntry>();
		readonly TimedList<string> _latestXdccListRequests = new TimedList<string>();
		readonly Queue<string> _userToAskForVersion = new Queue<string>();
		DateTime _lastAskForVersionTime = DateTime.Now;

		Server _server;
		public Server Server
		{
			get
			{
				return _server;
			}
			set
			{
				if (_server != null)
				{
					_server.OnEnabledChanged -= EnabledChanged;
				}
				_server = value;
				if (_server != null)
				{
					Name = _server.ToString();
					_server.OnEnabledChanged += EnabledChanged;
				}
			}
		}

		Parser.Parser _parser;
		public Parser.Parser Parser
		{
			get
			{
				return _parser;
			}
			set
			{
				if (_parser != null)
				{
					_parser.OnJoinChannel -= ParserOnJoinChannel;
					_parser.OnJoinChannelsFromBot -= ParserOnJoinChannelsFromBot;
					_parser.OnQueueRequestFromBot -= ParserOnQueueRequestFromBot;
					_parser.OnSendMessage -= ParserOnSendMessage;
					_parser.OnUnRequestFromBot -= ParserOnUnRequestFromBot;
					_parser.OnWriteLine -= ParserOnWriteLine;
					_parser.OnXdccList -= ParserOnXdccList;
				}
				_parser = value;
				if (_parser != null)
				{
					_parser.OnJoinChannel += ParserOnJoinChannel;
					_parser.OnJoinChannelsFromBot += ParserOnJoinChannelsFromBot;
					_parser.OnQueueRequestFromBot += ParserOnQueueRequestFromBot;
					_parser.OnSendMessage += ParserOnSendMessage;
					_parser.OnUnRequestFromBot += ParserOnUnRequestFromBot;
					_parser.OnWriteLine += ParserOnWriteLine;
					_parser.OnXdccList += ParserOnXdccList;
				}
			}
		}

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<Server>> OnDisconnected;

		#endregion

		#region EVENTHANDLER SERVER

		void EnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			var tPack = aEventArgs.Value1 as Packet;
			if (tPack != null)
			{
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
							ParserOnUnRequestFromBot(tBot);
						}
					}
				}
			}
		}

		#endregion

		#region EVENTHANDLER CLIENT

		void ClientOnConnected(object sender, EventArgs<Server> e)
		{
			StartWatch(Settings.Default.ChannelWaitTimeMedium, Server.ToString());
		}

		void ClientOnDisconnected(object sender, EventArgs<Server> e)
		{
			Stop();
		}

		void ClientOnMessage(object sender, EventArgs<Model.Domain.Channel, string, string> e)
		{
			Parser.Parse(e.Value1, e.Value2, e.Value3);

			// check if the bot sends a message and hold back xdcc list requests one more time
			var entry = _xdccListQueue.FirstOrDefault(x => x.User == e.Value2);
			if (entry != null)
			{
				entry.IncreaseTime();
			}
		}

		void ClientOnBotJoined(object sender, EventArgs<Bot> e)
		{
			RequestFromBot(e.Value1);
		}

		void ClientOnUserJoined(object sender, EventArgs<XG.Model.Domain.Channel, string> e)
		{
			CheckIfUserShouldVersioned(e.Value1, e.Value2);
		}

		void ClientOnQueueChannel(object sender, EventArgs<XG.Model.Domain.Channel, int> e)
		{
			AddChannelToQueue(e.Value1, e.Value2);
		}

		#endregion

		#region EVENTHANDLER PARSER

		void ParserOnJoinChannel (object aSender, EventArgs<Server, string> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				_log.Info("JoinChannel(" + aEventArgs.Value2 + ")");
				_client.Join(aEventArgs.Value2);
			}
		}

		void ParserOnJoinChannelsFromBot(object aSender, EventArgs<Bot> aEventArgs)
		{
			if (aEventArgs.Value1.Parent.Parent == Server)
			{
				_client.TryJoinBotChannels(aEventArgs.Value1);

				AddBotToQueue(aEventArgs.Value1, Settings.Default.CommandWaitTime);
			}
		}

		void ParserOnQueueRequestFromBot(object aSender, EventArgs<Bot, int> aEventArgs)
		{
			if (aEventArgs.Value1.Parent.Parent == Server)
			{
				AddBotToQueue(aEventArgs.Value1, aEventArgs.Value2);
			}
		}

		void ParserOnSendMessage(object aSender, EventArgs<Server, SendType, string, string> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				_client.SendMessage(aEventArgs.Value2, aEventArgs.Value3, aEventArgs.Value4);
			}
		}

		void ParserOnUnRequestFromBot(object aSender, EventArgs<Bot> aEventArgs)
		{
			if (aEventArgs.Value1.Parent.Parent == Server)
			{
				ParserOnUnRequestFromBot(aEventArgs.Value1);
			}
		}

		void ParserOnXdccList (object aSender, EventArgs<Model.Domain.Channel, string, string> aEventArgs)
		{
			if (aEventArgs.Value1.Parent == Server)
			{
				// dont send the same request to often
				_latestXdccListRequests.RemoveExpiredItems();
				if (_latestXdccListRequests.Contains(aEventArgs.Value2 + "@" + aEventArgs.Value3))
				{
					double seconds = _latestXdccListRequests.GetMissingSeconds(aEventArgs.Value2 + "@" + aEventArgs.Value3);
					_log.Info("XdccList(" + aEventArgs.Value2 + ", " + aEventArgs.Value3 + ") blocked for " + seconds + " seconds");
					return;
				}

				var entry = _xdccListQueue.FirstOrDefault(x => x.User == aEventArgs.Value2);
				if (entry == null)
				{
					_log.Info("XdccList(" + aEventArgs.Value2 + ", " + aEventArgs.Value3 + ") adding");
					entry = new XdccListEntry(aEventArgs.Value2, aEventArgs.Value3);
					_xdccListQueue.Add(entry);
				}
				else
				{
					entry.IncreaseTime();
					if (entry.Commands.All(s => s != aEventArgs.Value3))
					{
						_log.Info("XdccList(" + aEventArgs.Value2 + ", " + aEventArgs.Value3 + ") enqueuing");
						entry.Commands.Enqueue(aEventArgs.Value3);
					}
					else
					{
						_log.Info("XdccList(" + aEventArgs.Value2 + ", " + aEventArgs.Value3 + ") skipping");
					}
				}
			}
		}

		void ParserOnWriteLine(object aSender, EventArgs<Server, string> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				_log.Info("WriteLine(" + aEventArgs.Value2 + ")");
				_client.WriteLine(aEventArgs.Value2);
			}
		}

		#endregion

		#region IRC Stuff

		void RequestFromBot(Bot aBot)
		{
			if (aBot.State == Bot.States.Idle)
			{
				// check if the packet is already downloaded, or active - than disable it and get the next one
				Packet tPacket = aBot.OldestActivePacket();
				while (tPacket != null)
				{
					File tFile = FileActions.TryGetFile(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, tPacket.RealSize != 0 ? tPacket.RealSize : tPacket.Size);
					if (tFile != null && tFile.Connected)
					{
						_log.Warn("RequestFromBot(" + aBot + ") packet " + tPacket + " is already in use");
						tPacket.Enabled = false;
						tPacket = aBot.OldestActivePacket();
					}
					else
					{
						string name = Helper.ShrinkFileName(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, 0);
						_latestPacketRequests.RemoveExpiredItems();
						if (_latestPacketRequests.Contains(name))
						{
							double time = _latestPacketRequests.GetMissingSeconds(name);
							_log.Warn("RequestFromBot(" + aBot + ") packet name " + tPacket.Name + " is blocked for " + time + "ms");
							AddBotToQueue(aBot, (int) time + 1);
							return;
						}

						if (_server.Connected)
						{
							_log.Info("RequestFromBot(" + aBot + ") requesting packet #" + tPacket.Id + " (" + tPacket.Name + ")");
							_client.XdccSend(tPacket);
							_latestPacketRequests.Add(name, DateTime.Now.AddSeconds(Settings.Default.SamePacketRequestTime));

							FireNotificationAdded(Notification.Types.PacketRequested, tPacket);
						}

						// create a timer to re request if the bot didnt recognized the privmsg
						AddBotToQueue(aBot, Settings.Default.BotWaitTime);
						break;
					}
				}
			}
		}

		void ParserOnUnRequestFromBot(Bot aBot)
		{
			_log.Info("UnRequestFromBot(" + aBot + ")");
			_client.XdccRemove(aBot);

			AddBotToQueue(aBot, Settings.Default.CommandWaitTime);

			FireNotificationAdded(Notification.Types.PacketRemoved, aBot.CurrentQueuedPacket);
		}

		void CheckIfUserShouldVersioned(Model.Domain.Channel aChannel, string aUser)
		{
			if (_client.IsUserMaybeeXdccBot(aChannel.Name, aUser))
			{
				_userToAskForVersion.Enqueue(aUser);
			}
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_log.Info("StartRun(" + Server + ")");

			_client = new XdccClient
			{
				Server = Server
			};

			_client.OnConnected += ClientOnConnected;
			_client.OnDisconnected += ClientOnDisconnected;
			_client.OnMessage += ClientOnMessage;
			_client.OnBotJoined += ClientOnBotJoined;
			_client.OnUserJoined += ClientOnUserJoined;
			_client.OnQueueChannel += ClientOnQueueChannel;
			_client.OnNotificationAdded += FireNotificationAdded;

			_client.Connect();
		}

		protected override void StopRun()
		{
			_log.Info("StopRun(" + Server + ")");

			Stopwatch();
			_client.Disconnect();

			_client.OnConnected -= ClientOnConnected;
			_client.OnDisconnected -= ClientOnDisconnected;
			_client.OnMessage -= ClientOnMessage;
			_client.OnBotJoined -= ClientOnBotJoined;
			_client.OnUserJoined -= ClientOnUserJoined;
			_client.OnQueueChannel -= ClientOnQueueChannel;
			_client.OnNotificationAdded -= FireNotificationAdded;

			OnDisconnected(this, new EventArgs<Server>(Server));
		}

		public void ParseXdccFile(string aNick, string[] aLines)
		{
			Model.Domain.Channel tChan = null;
			var user = _client.GetIrcUser(aNick);
			if (user != null)
			{
				foreach (string channel in user.JoinedChannels)
				{
					tChan = Server.Channel(channel);
					if (tChan != null)
					{
						break;
					}
				}
			}

			if (tChan == null)
			{
				_log.Error(".ParseXdccFile() cant find channel for nick " + aNick);
				return;
			}

			foreach (var line in aLines)
			{
				_parser.Parse(tChan, aNick, line);
			}
		}

		public void Disconnect()
		{
			_client.Disconnect();
		}

		#endregion

		#region TIMER

		public void TriggerTimerRun()
		{
			TriggerChannelRun();
			TriggerBotRun();
			TriggerXdccListRun();
			TriggerVersionRun();
		}

		void TriggerChannelRun()
		{
			var channelsReady = _channelQueue.GetExpiredItems();
			foreach (var channel in channelsReady)
			{
				_client.Join(channel);
			}
		}

		void TriggerBotRun()
		{
			var botsReady = _botQueue.GetExpiredItems();
			foreach (var bot in botsReady)
			{
				RequestFromBot(bot);
			}
		}

		void TriggerXdccListRun()
		{
			if (!_client.IsConnected)
			{
				return;
			}

			var entriesReady = (from e in _xdccListQueue where (e.WaitUntil - DateTime.Now).TotalSeconds < 0 && e.Commands.Count > 0 select e).ToArray();
			foreach (var entry in entriesReady)
			{
				string command = entry.Commands.Dequeue();
				_log.Info("TriggerXdccListRun(" + entry.User + ", " + command + ")");
				_client.SendMessage(entry.User, command);
				_latestXdccListRequests.Add(entry.User + "@" + command, DateTime.Now.AddSeconds(Settings.Default.ChannelWaitTimeLong));

				if (entry.Commands.Count == 0)
				{
					_log.Info("TriggerXdccListRun(" + entry.User + ") removing entry");
					_xdccListQueue.Remove(entry);
				}
				else
				{
					entry.IncreaseTime();
				}
			}
		}

		void TriggerVersionRun()
		{
			if (_lastAskForVersionTime.AddSeconds(Settings.Default.CommandWaitTime) < DateTime.Now && _userToAskForVersion.Count > 0)
			{
				_lastAskForVersionTime = DateTime.Now;
				string user = _userToAskForVersion.Dequeue();

				_log.Info("AskForVersion(" + user + ")");
				_client.Version(user);
			}
		}

		public void AddBotToQueue(Bot aBot, int aInt)
		{
			if (!_botQueue.Contains(aBot))
			{
				_botQueue.Add(aBot, DateTime.Now.AddSeconds(aInt));
			}
		}

		public void AddChannelToQueue(Model.Domain.Channel aChannel, int aInt)
		{
			if (!_channelQueue.Contains(aChannel))
			{
				_channelQueue.Add(aChannel, DateTime.Now.AddSeconds(aInt));
			}
		}

		#endregion
	}
}
