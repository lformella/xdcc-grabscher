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
using System.Text.RegularExpressions;

using XG.Core;
using XG.Server.Helper;
using XG.Server.Worker;

using Meebey.SmartIrc4net;
using log4net;

namespace XG.Server.Plugin.Core.Irc
{
	public class IrcConnection : AWorker
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public IrcClient Client { get; private set; }
		string _iam;
		
		readonly TimedList<Bot> _botQueue = new TimedList<Bot>();
		readonly TimedList<XG.Core.Channel> _channelQueue = new TimedList<XG.Core.Channel>();
		readonly TimedList<string> _latestPacketRequests = new TimedList<string>();
		readonly List<XdccListEntry> _xdccListQueue = new List<XdccListEntry>();
		readonly TimedList<string> _latestXdccListRequests = new TimedList<string>();
		readonly Queue<string> _userToAskForVersion = new Queue<string>();
		DateTime _lastAskForVersionTime = DateTime.Now;

		XG.Core.Server _server;
		public XG.Core.Server Server
		{
			get
			{
				return _server;
			}
			set
			{
				if (_server != null)
				{
					_server.OnAdded -= ObjectAdded;
					_server.OnRemoved -= ObjectRemoved;
					_server.OnEnabledChanged -= EnabledChanged;
				}
				_server = value;
				if (_server != null)
				{
					_server.OnAdded += ObjectAdded;
					_server.OnRemoved += ObjectRemoved;
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
					_parser.OnJoinChannel -= JoinChannel;
					_parser.OnJoinChannelsFromBot -= JoinChannelsFromBot;
					_parser.OnQueueRequestFromBot -= QueueRequestFromBot;
					_parser.OnSendMessage -= SendMessage;
					_parser.OnUnRequestFromBot -= UnRequestFromBot;
					_parser.OnWriteLine -= WriteLine;
					_parser.OnXdccList -= XdccList;
				}
				_parser = value;
				if (_parser != null)
				{
					_parser.OnJoinChannel += JoinChannel;
					_parser.OnJoinChannelsFromBot += JoinChannelsFromBot;
					_parser.OnQueueRequestFromBot += QueueRequestFromBot;
					_parser.OnSendMessage += SendMessage;
					_parser.OnUnRequestFromBot += UnRequestFromBot;
					_parser.OnWriteLine += WriteLine;
					_parser.OnXdccList += XdccList;
				}
			}
		}

		public FileActions FileActions { get; set; }

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<XG.Core.Server>> OnDisconnected;

		#endregion

		#region EVENTHANDLER

		void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			var aChan = aEventArgs.Value2 as XG.Core.Channel;
			if (aChan != null)
			{
				if (aChan.Enabled)
				{
					Client.RfcJoin(aChan.Name);
				}
			}
		}

		void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			var aChan = aEventArgs.Value2 as XG.Core.Channel;
			if (aChan != null)
			{
				var packets = (from bot in aChan.Bots from packet in bot.Packets select packet).ToArray();
				foreach (Packet tPack in packets)
				{
					tPack.Enabled = false;
				}

				Client.RfcPart(aChan.Name);
			}
		}

		void EnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			var aChan = aEventArgs.Value1 as XG.Core.Channel;
			if (aChan != null)
			{
				if (aChan.Enabled)
				{
					Client.RfcJoin(aChan.Name);
				}
				else
				{
					Client.RfcPart(aChan.Name);
				}
			}

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
							UnRequestFromBot(tBot);
						}
					}
				}
			}
		}

		#endregion

		#region IRC EVENTHANDLER

		void JoinChannel (object aSender, EventArgs<XG.Core.Server, string> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				_log.Info("JoinChannel(" + aEventArgs.Value2 + ")");
				Client.RfcJoin(aEventArgs.Value2);
			}
		}

		void JoinChannelsFromBot(object aSender, EventArgs<XG.Core.Server, Bot> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				var user = Client.GetIrcUser(aEventArgs.Value2.Name);
				if (user != null)
				{
					_log.Info("JoinChannelsFromBot(" + aEventArgs.Value2 + ")");
					Client.RfcJoin(user.JoinedChannels);
					AddBotToQueue(aEventArgs.Value2, Settings.Instance.CommandWaitTime);
				}
			}
		}

		void QueueRequestFromBot(object aSender, EventArgs<XG.Core.Server, Bot, int> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				AddBotToQueue(aEventArgs.Value2, aEventArgs.Value3);
			}
		}

		void UnRequestFromBot(object aSender, EventArgs<XG.Core.Server, Bot> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				UnRequestFromBot(aEventArgs.Value2);
			}
		}

		void Parse(object aSender, IrcEventArgs aEventArgs)
		{
			Parser.Parse(this, aEventArgs);

			// check if the bot sends a message and hold back xdcc list requests one more time
			var entry = _xdccListQueue.FirstOrDefault(x => x.User == aEventArgs.Data.Nick);
			if (entry != null)
			{
				entry.IncreaseTime();
			}
		}

		void XdccList (object aSender, EventArgs<XG.Core.Server, string, string> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
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
					if (!entry.Commands.Any(s => s == aEventArgs.Value3))
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

		void SendMessage(object aSender, EventArgs<XG.Core.Server, SendType, string, string> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				_log.Info("SendMessage(" + aEventArgs.Value3 + ", " + aEventArgs.Value4 + ")");
				Client.SendMessage(aEventArgs.Value2, aEventArgs.Value3, aEventArgs.Value4);
			}
		}

		void WriteLine(object aSender, EventArgs<XG.Core.Server, string> aEventArgs)
		{
			if (aEventArgs.Value1 == Server)
			{
				_log.Info("WriteLine(" + aEventArgs.Value2 + ")");
				Client.WriteLine(aEventArgs.Value2);
			}
		}

		#endregion

		#region IRC Stuff

		void RegisterIrcEvents()
		{
			Client.OnPing += (sender, e) => Client.RfcPong(e.Data.Message);

			Client.OnConnected += (sender, e) =>
			{
				Server.Connected = true;
				Server.Commit();
				_log.Info("connected " + Server);

				Client.Login(Settings.Instance.IrcNick, Settings.Instance.IrcNick, 0, Settings.Instance.IrcNick, Settings.Instance.IrcPasswort);

				var channels = (from channel in Server.Channels where channel.Enabled select channel.Name).ToArray();
				Client.RfcJoin(channels);
				Client.Listen();
			};

			Client.OnError += (sender, e) => _log.Info("error from " + Server + ": " + e.ErrorMessage);

			Client.OnConnectionError += (sender, e) => _log.Info("connection error from " + Server + ": " + e);

			Client.OnConnecting += (sender, e) =>
			{
				Server.Connected = false;
				Server.Commit();
				_log.Info("connecting to " + Server);
			};

			Client.OnDisconnected += (sender, e) =>
			{
				Server.Connected = false;
				Server.Commit();
				_log.Info("disconnected " + Server);
				OnDisconnected(this, new EventArgs<XG.Core.Server>(Server));
			};

			Client.OnJoin += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					if (_iam == e.Who)
					{
						channel.ErrorCode = 0;
						channel.Connected = true;
						_log.Info("joined " + channel);

						FireNotificationAdded(Notification.Types.ChannelJoined, channel);
					}
					else
					{
						var bot = channel.Bot(e.Who);
						if (bot != null)
						{
							bot.Connected = true;
							bot.LastMessage = "joined channel " + channel.Name;
							if (bot.State != Bot.States.Active)
							{
								bot.State = Bot.States.Idle;
							}
							UpdateBot(bot);
							RequestFromBot(bot);
						}
					}
					CheckIfUserShouldVersioned(e.Channel, e.Who);
					UpdateChannel(channel);
				}
			};

			Client.OnPart += (sender, e) =>
			{
				var channel = Server.Channel(e.Data.Channel);
				if (channel != null)
				{
					if (_iam == e.Who)
					{
						channel.Connected = false;
						channel.ErrorCode = 0;
						_log.Info("parted " + channel);

						FireNotificationAdded(Notification.Types.ChannelParted, channel);
					}
					else
					{
						var bot = channel.Bot(e.Who);
						if (bot != null)
						{
							bot.Connected = false;
							bot.LastMessage = "parted channel " + e.Channel;
							UpdateBot(bot);
						}
					}
					UpdateChannel(channel);
				}
			};

			Client.OnNickChange += (sender, e) =>
			{
				if (_iam == e.OldNickname)
				{
					_iam = e.NewNickname;
				}
				else
				{
					var bot = Server.Bot(e.OldNickname);
					if (bot != null)
					{
						bot.Name = e.NewNickname;
						UpdateBot(bot);
					}
				}
			};

			Client.OnBan += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					if (_iam == e.Who)
					{
						channel.Connected = false;
					}
					else
					{
						var bot = channel.Bot(e.Who);
						if (bot != null)
						{
							bot.Connected = false;
							bot.LastMessage = "banned from " + e.Data.Channel;
							UpdateBot(bot);
						}
					}
					UpdateChannel(channel);
				}
			};

			Client.OnKick += (sender, e) =>
			{
				var channel = Server.Channel(e.Data.Channel);
				if (channel != null)
				{
					if (_iam == e.Whom)
					{
						channel.Connected = false;
						_log.Warn("kicked from " + channel.Name + " (" + e.KickReason + ")");
						FireNotificationAdded(Notification.Types.ChannelKicked, channel);
					}
					else
					{
						var bot = channel.Bot(e.Whom);
						if (bot != null)
						{
							bot.Connected = false;
							bot.LastMessage = "kicked from " + e.Channel;
							UpdateBot(bot);
						}
					}
					UpdateChannel(channel);
				}
			};

			Client.OnQuit += (sender, e) =>
			{
				var bot = Server.Bot(e.Who);
				if (bot != null)
				{
					bot.Connected = false;
					bot.LastMessage = "quited";
					UpdateBot(bot);
					UpdateChannel(bot.Parent);
				}
			};

			Client.OnNames += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					foreach (string user in e.UserList)
					{
						var bot = channel.Bot(Regex.Replace(user, "^(@|!|%|\\+){1}", ""));
						if (bot != null)
						{
							bot.Connected = true;
							bot.LastMessage = "joined channel " + channel.Name;
							if (bot.State != Bot.States.Active)
							{
								bot.State = Bot.States.Idle;
							}
							bot.Commit();
							RequestFromBot(bot);
						}
						CheckIfUserShouldVersioned(e.Channel, user);
					}
					UpdateChannel(channel);
				}
			};

			Client.OnTopic += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					channel.Topic = Irc.Parser.Helper.RemoveSpecialIrcChars(e.Topic);
					channel.Commit();
				}
			};

			Client.OnTopicChange += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					channel.Topic = Irc.Parser.Helper.RemoveSpecialIrcChars(e.NewTopic);
					channel.Commit();
				}
			};

			Client.OnUnban += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					if (_iam == e.Who)
					{
						channel.ErrorCode = 0;
						channel.Commit();
						AddChannelToQueue(channel, Settings.Instance.CommandWaitTime);
					}
				}
			};

			Client.OnErrorMessage += (sender, e) =>
			{
				var channel = Server.Channel(e.Data.Channel);
				if (channel == null && e.Data.RawMessageArray.Length >= 4)
				{
					channel = Server.Channel(e.Data.RawMessageArray[3]);
				}
				if (channel != null)
				{
					int tWaitTime = 0;
					var notificationType = Notification.Types.ChannelJoinFailed;
					switch (e.Data.ReplyCode)
					{
						case ReplyCode.ErrorNoChannelModes:
						case ReplyCode.ErrorTooManyChannels:
						case ReplyCode.ErrorNotRegistered:
						case ReplyCode.ErrorChannelIsFull:
							tWaitTime = Settings.Instance.ChannelWaitTimeShort;
							break;

						case ReplyCode.ErrorInviteOnlyChannel:
						case ReplyCode.ErrorUniqueOpPrivilegesNeeded:
							tWaitTime = Settings.Instance.ChannelWaitTimeMedium;
							break;

						case ReplyCode.ErrorBannedFromChannel:
							tWaitTime = Settings.Instance.ChannelWaitTimeLong;
							break;
					}
					if (tWaitTime > 0)
					{
						channel.ErrorCode = (int)e.Data.ReplyCode;
						channel.Connected = false;
						_log.Warn("could not join " + channel + ": " + e.Data.ReplyCode);

						FireNotificationAdded(notificationType, channel);
						AddChannelToQueue(channel, tWaitTime);
					}

					channel.Commit();
				}
			};

			Client.OnQueryMessage += Parse;

			Client.OnQueryAction += (sender, e) => _log.Debug("OnQueryAction " + e.Data.Message);

			Client.OnChannelMessage += Parse;

			Client.OnChannelNotice += (sender, e) => _log.Debug("OnChannelNotice " + e.Data.Message);

			Client.OnQueryNotice += Parse;

			Client.OnCtcpReply += Parse;

			Client.OnCtcpRequest += Parse;

			Client.OnWriteLine += (sender, e) => _log.Debug("OnWriteLine " + e.Line);
		}

		void UpdateChannel(XG.Core.Channel aChannel)
		{
			var channel = (NonRfcChannel) Client.GetChannel(aChannel.Name);
			if (channel != null)
			{
				aChannel.UserCount = channel.Users.Count;
			}
			aChannel.Commit();
		}

		void UpdateBot(Bot aBot)
		{
			// dont hammer plugins with not needed information updates - 60 seconds are enough
			if ((DateTime.Now - aBot.LastContact).TotalSeconds > 60)
			{
				aBot.LastContact = DateTime.Now;
			}
			aBot.Commit();
		}

		void RequestFromBot(Bot aBot)
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
						tPacket = aBot.OldestActivePacket();
					}
					else
					{
						string name = XG.Core.Helper.ShrinkFileName(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, 0);
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
							Client.SendMessage(SendType.Message, aBot.Name, "XDCC SEND " + tPacket.Id);
							_latestPacketRequests.Add(name, DateTime.Now.AddSeconds(Settings.Instance.SamePacketRequestTime));

							FireNotificationAdded(Notification.Types.PacketRequested, tPacket);
						}

						// create a timer to re request if the bot didnt recognized the privmsg
						AddBotToQueue(aBot, Settings.Instance.BotWaitTime);
						break;
					}
				}
			}
		}

		void UnRequestFromBot(Bot aBot)
		{
			_log.Info("UnRequestFromBot(" + aBot + ")");
			Client.SendMessage(SendType.Message, aBot.Name, "XDCC REMOVE");

			AddBotToQueue(aBot, Settings.Instance.CommandWaitTime);

			FireNotificationAdded(Notification.Types.PacketRemoved, aBot);
		}

		void CheckIfUserShouldVersioned(string aChannel, string aUser)
		{
			var user = (NonRfcChannelUser) Client.GetChannelUser(aChannel, aUser);
			if (user != null)
			{
				// dont version ops!
				if (user.IsIrcOp || user.IsOwner || user.IsOp || user.IsHalfop)
				{
					return;
				}
				// just aks voiced users because they could be bots
				if (!user.IsVoice)
				{
					return;
				}
			}
			else
			{
				// just ask users who are named like bots
				if (!Irc.Parser.Helper.Match(aUser, ".*XDCC.*").Success)
				{
					return;
				}
			}

			_userToAskForVersion.Enqueue(aUser);
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_iam = Settings.Instance.IrcNick;

			Client = new IrcClient()
			{
				AutoNickHandling = true,
				ActiveChannelSyncing = true,
				AutoReconnect = true,
				AutoRetry = true,
				AutoJoinOnInvite = true,
				AutoRejoinOnKick = true,
				CtcpVersion = Settings.Instance.IrcVersion,
				SupportNonRfc = true
			};

			RegisterIrcEvents();

			try
			{
				Client.Connect(Server.Name, Server.Port);
			}
			catch(CouldNotConnectException ex)
			{
				_log.Fatal("StartRun() connection failed " + ex.Message);
				Server.Connected = false;
				Server.Commit();
				OnDisconnected(this, new EventArgs<XG.Core.Server>(Server));
			}
		}

		protected override void StopRun()
		{
			try
			{
				Client.Disconnect();
			}
			catch (NotConnectedException)
			{
				// this is ok
			}
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
				Client.RfcJoin(channel.Name);
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
			if (!Client.IsConnected)
			{
				return;
			}

			var entriesReady = (from e in _xdccListQueue where (e.WaitUntil - DateTime.Now).TotalSeconds < 0 && e.Commands.Count > 0 select e).ToArray();
			foreach (var entry in entriesReady)
			{
				string command = entry.Commands.Dequeue();
				_log.Info("TriggerXdccListRun(" + entry.User + ", " + command + ")");
				Client.SendMessage(SendType.Message, entry.User, command);
				_latestXdccListRequests.Add(entry.User + "@" + command, DateTime.Now.AddSeconds(Settings.Instance.ChannelWaitTimeLong));

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
			if (_lastAskForVersionTime.AddSeconds(Settings.Instance.CommandWaitTime) < DateTime.Now && _userToAskForVersion.Count > 0)
			{
				_lastAskForVersionTime = DateTime.Now;
				string user = _userToAskForVersion.Dequeue();

				_log.Info("AskForVersion(" + user + ")");
				Client.SendMessage(SendType.CtcpRequest, user, Rfc2812.Version());
			}
		}

		public void AddBotToQueue(Bot aBot, int aInt)
		{
			if (!_botQueue.Contains(aBot))
			{
				_botQueue.Add(aBot, DateTime.Now.AddSeconds(aInt));
			}
		}

		public void AddChannelToQueue(XG.Core.Channel aChannel, int aInt)
		{
			if (!_channelQueue.Contains(aChannel))
			{
				_channelQueue.Add(aChannel, DateTime.Now.AddSeconds(aInt));
			}
		}

		#endregion
	}
}
