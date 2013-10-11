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
using XG.Server.Plugin.Core.Irc.Parser;

using Meebey.SmartIrc4net;
using log4net;

namespace XG.Server.Plugin.Core.Irc
{
	public delegate void BotDelegate(Bot aBot);

	public class IrcConnection : AWorker
	{
		#region VARIABLES

		ILog _log = LogManager.GetLogger(typeof(Plugin));

		readonly IrcClient _irc = new IrcClient();
		string _iam;
		
		readonly Dictionary<Bot, DateTime> _botQueue = new Dictionary<Bot, DateTime>();
		readonly Dictionary<XG.Core.Channel, DateTime> _channelQueue = new Dictionary<XG.Core.Channel, DateTime>();
		readonly Dictionary<string, DateTime> _latestPacketRequests = new Dictionary<string, DateTime>();

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

					_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + _server.Name + ")");
				}
			}
		}

		Notice _notice;
		public Notice Notice
		{
			get
			{
				return _notice;
			}
			set
			{
				if (_notice != null)
				{
					_notice.OnJoinChannel -= JoinChannel;
					_notice.OnJoinChannelsFromBot -= JoinChannelsFromBot;
					_notice.OnQueueRequestFromBot -= QueueRequestFromBot;
					_notice.OnUnRequestFromBot -= UnRequestFromBot;
					_notice.OnXdccListEnabled -= XdccListEnabled;
				}
				_notice = value;
				if (_notice != null)
				{
					_notice.OnJoinChannel += JoinChannel;
					_notice.OnJoinChannelsFromBot += JoinChannelsFromBot;
					_notice.OnQueueRequestFromBot += QueueRequestFromBot;
					_notice.OnUnRequestFromBot += UnRequestFromBot;
					_notice.OnXdccListEnabled += XdccListEnabled;
				}
			}
		}

		Message _message;
		public Message Message
		{
			get
			{
				return _message;
			}
			set
			{
				if (_message != null)
				{
					_message.OnQueueRequestFromBot -= QueueRequestFromBot;
					_message.OnJoinChannel -= JoinChannel;
				}
				_message = value;
				if (_message != null)
				{
					_message.OnQueueRequestFromBot += QueueRequestFromBot;
					_message.OnJoinChannel += JoinChannel;
				}
			}
		}

		Ctcp _ctpc;
		public Ctcp Ctcp
		{
			get
			{
				return _ctpc;
			}
			set
			{
				if (_ctpc != null)
				{
					_ctpc.OnSendPrivateMessage -= SendPrivateMessage;
					_ctpc.OnUnRequestFromBot -= UnRequestFromBot;
				}
				_ctpc = value;
				if (_ctpc != null)
				{
					_ctpc.OnSendPrivateMessage += SendPrivateMessage;
					_ctpc.OnUnRequestFromBot += UnRequestFromBot;
				}
			}
		}

		Nickserv _nickServ;
		public Nickserv Nickserv
		{
			get
			{
				return _nickServ;
			}
			set
			{
				if (_nickServ != null)
				{
					_nickServ.OnSendData -= SendData;
				}
				_nickServ = value;
				if (_nickServ != null)
				{
					_nickServ.OnSendData += SendData;
				}
			}
		}

		public FileActions FileActions { get; set; }

		#endregion

		#region EVENTS

		public event ServerDelegate OnDisconnected;

		#endregion

		#region EVENTHANDLER
		
		void ObjectAdded(AObject aParent, AObject aObj)
		{
			var aChan = aObj as XG.Core.Channel;
			if (aChan != null)
			{
				if (aChan.Enabled)
				{
					_irc.RfcJoin(aChan.Name);
				}
			}
		}

		void ObjectRemoved(AObject aParent, AObject aObj)
		{
			var aChan = aObj as XG.Core.Channel;
			if (aChan != null)
			{
				var packets = (from bot in aChan.Bots from packet in bot.Packets select packet).ToArray();
				foreach (Packet tPack in packets)
				{
					tPack.Enabled = false;
				}

				_irc.RfcPart(aChan.Name);
			}
		}

		void EnabledChanged(AObject aObj)
		{
			var aChan = aObj as XG.Core.Channel;
			if (aChan != null)
			{
				if (aChan.Enabled)
				{
					_irc.RfcJoin(aChan.Name);
				}
				else
				{
					_irc.RfcPart(aChan.Name);
				}
			}
			
			var tPack = aObj as Packet;
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

		void JoinChannel (XG.Core.Server aServer, string aData)
		{
			if (aServer == Server)
			{
				_irc.RfcJoin(aData);
			}
		}

		void JoinChannelsFromBot (XG.Core.Server aServer, Bot aBot)
		{
			if (aServer == Server)
			{
				var user = _irc.GetIrcUser(aBot.Name);
				if (user != null)
				{
					_irc.RfcJoin(user.JoinedChannels);
					AddBotToQueue(aBot, Settings.Instance.CommandWaitTime);
				}
			}
		}

		void QueueRequestFromBot (XG.Core.Server aServer, Bot aBot, int aInt)
		{
			if (aServer == Server)
			{
				AddBotToQueue(aBot, aInt);
			}
		}

		void UnRequestFromBot (XG.Core.Server aServer, Bot aBot)
		{
			if (aServer == Server)
			{
				UnRequestFromBot(aBot);
			}
		}

		void XdccListEnabled (XG.Core.Server aServer, string aBot)
		{
			if (aServer == Server)
			{
				_irc.RfcPrivmsg(aBot, "XDCC LIST");
			}
		}

		void SendPrivateMessage (XG.Core.Server aServer, Bot aBot, string aData)
		{
			if (aServer == Server)
			{
				_irc.RfcPrivmsg(aBot.Name, aData);
			}
		}

		void SendData (XG.Core.Server aServer, string aData)
		{
			if (aServer == Server)
			{
				_irc.WriteLine(aData);
			}
		}

		#endregion

		#region IRC Stuff

		public Meebey.SmartIrc4net.Channel GetChannelInfo(string aChannel)
		{
			return _irc.GetChannel(aChannel);
		}

		void RegisterIrcEvents()
		{
			_irc.OnPing += (sender, e) => _irc.RfcPong(e.Data.Message);

			_irc.OnConnected += (sender, e) =>
			{
				Server.Connected = true;
				Server.Commit();
				_log.Info("connected " + Server);

				_irc.Login(Settings.Instance.IrcNick, Settings.Instance.IrcNick);

				var channels = (from channel in Server.Channels where channel.Enabled select channel.Name).ToArray();
				_irc.RfcJoin(channels);
				_irc.Listen();
			};

			_irc.OnError += (sender, e) => _log.Info("error from " + Server + ": " + e.ErrorMessage);

			_irc.OnConnectionError += (sender, e) => _log.Info("connection error from " + Server + ": " + e);

			_irc.OnConnecting += (sender, e) =>
			{
				Server.Connected = false;
				Server.Commit();
				_log.Info("connecting to " + Server);
			};

			_irc.OnDisconnected += (sender, e) =>
			{
				Server.Connected = false;
				Server.Commit();
				_log.Info("disconnected " + Server);
				OnDisconnected(Server);
			};

			_irc.OnJoin += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					if (_iam == e.Who)
					{
						channel.ErrorCode = 0;
						channel.Connected = true;
						_log.Info("joined " + channel);

						FireNotificationAdded(new Notification(Notification.Types.ChannelJoined, channel));
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
					UpdateChannel(channel);
				}
			};

			_irc.OnPart += (sender, e) =>
			{
				var channel = Server.Channel(e.Data.Channel);
				if (channel != null)
				{
					if (_iam == e.Who)
					{
						channel.Connected = false;
						channel.ErrorCode = 0;
						_log.Info("parted " + channel);

						FireNotificationAdded(new Notification(Notification.Types.ChannelParted, channel));
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

			_irc.OnNickChange += (sender, e) =>
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

			_irc.OnBan += (sender, e) =>
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

			_irc.OnKick += (sender, e) =>
			{
				var channel = Server.Channel(e.Data.Channel);
				if (channel != null)
				{
					if (_iam == e.Whom)
					{
						channel.Connected = false;
						_log.Warn("kicked from " + channel.Name + " (" + e.KickReason + ")");
						FireNotificationAdded(new Notification(Notification.Types.ChannelKicked, channel));
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

			_irc.OnQuit += (sender, e) =>
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

			_irc.OnNames += (sender, e) =>
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
					}
					UpdateChannel(channel);
				}
			};

			_irc.OnTopic += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					channel.Topic = Parser.Helper.RemoveSpecialIrcChars(e.Topic);
					channel.Commit();
				}
			};

			_irc.OnTopicChange += (sender, e) =>
			{
				var channel = Server.Channel(e.Channel);
				if (channel != null)
				{
					channel.Topic = Parser.Helper.RemoveSpecialIrcChars(e.NewTopic);
					channel.Commit();
				}
			};

			_irc.OnUnban += (sender, e) =>
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

			_irc.OnErrorMessage += (sender, e) =>
			{
				var channel = Server.Channel(e.Data.Channel);
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

						FireNotificationAdded(new Notification(notificationType, channel));
						AddChannelToQueue(channel, tWaitTime);
					}

					channel.Commit();
				}
			};

			_irc.OnQueryMessage += (sender, e) => Message.Parse(Server, e);

			_irc.OnQueryAction += (sender, e) =>
			{
				int a = 0;
			};

			_irc.OnChannelMessage += (sender, e) => Message.Parse(Server, e);

			_irc.OnQueryNotice += (sender, e) =>
			{
				if (e.Data.Nick != null)
				{
					if (e.Data.Nick.ToLower() == "nickserv")
					{
						Nickserv.Parse(Server, e);
					}
					else
					{
						Notice.Parse(Server, e);
					}
				}
			};

			_irc.OnCtcpReply += (sender, e) => Ctcp.Parse(Server, e);

			_irc.OnCtcpRequest += (sender, e) => Ctcp.Parse(Server, e);
		}

		void UpdateChannel(XG.Core.Channel aChannel)
		{
			var channel = _irc.GetChannel(aChannel.Name);
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
						if (_latestPacketRequests.ContainsKey(name))
						{
							double time = (_latestPacketRequests[name] - DateTime.Now).TotalSeconds;
							if (time > 0)
							{
								_log.Warn("RequestFromBot(" + aBot + ") packet name " + tPacket.Name + " is blocked for " + time + "ms");
								AddBotToQueue(aBot, (int) time + 1);
								return;
							}
						}

						if (_server.Connected)
						{
							_log.Info("RequestFromBot(" + aBot + ") requesting packet #" + tPacket.Id + " (" + tPacket.Name + ")");
							_irc.RfcPrivmsg(aBot.Name, "XDCC SEND " + tPacket.Id);

							if (_latestPacketRequests.ContainsKey(name))
							{
								_latestPacketRequests.Remove(name);
							}
							_latestPacketRequests.Add(name, DateTime.Now.AddSeconds(Settings.Instance.SamePacketRequestTime));

							FireNotificationAdded(new Notification(Notification.Types.PacketRequested, tPacket));
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
			_irc.RfcPrivmsg(aBot.Name, "XDCC REMOVE");

			AddBotToQueue(aBot, Settings.Instance.CommandWaitTime);

			FireNotificationAdded(new Notification(Notification.Types.PacketRemoved, aBot));
		}

		public void RequestXdccHelp(string aBotName)
		{
			_irc.RfcPrivmsg(aBotName, "XDCC HELP");
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_iam = Settings.Instance.IrcNick;

			_irc.AutoNickHandling = true;
			_irc.ActiveChannelSyncing = true;
			_irc.AutoReconnect = true;
			_irc.AutoRetry = true;
			_irc.AutoJoinOnInvite = true;
			_irc.AutoRejoinOnKick = true;

			RegisterIrcEvents();

			try
			{
				_irc.Connect(Server.Name, Server.Port);
			}
			catch(CouldNotConnectException ex)
			{
				_log.Fatal("StartRun() connection failed " + ex.Message);
				Server.Connected = false;
				Server.Commit();
				OnDisconnected(Server);
			}
		}

		protected override void StopRun()
		{
			try
			{
				_irc.Disconnect();
			}
			catch (Meebey.SmartIrc4net.NotConnectedException)
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
		}

		void TriggerChannelRun()
		{
			var remove = new HashSet<XG.Core.Channel>();

			foreach (var kvp in _channelQueue)
			{
				DateTime time = kvp.Value;
				if ((time - DateTime.Now).TotalSeconds < 0)
				{
					remove.Add(kvp.Key);
				}
			}

			foreach (var channel in remove)
			{
				_channelQueue.Remove(channel);

				_irc.RfcJoin(channel.Name);
			}
		}

		void TriggerBotRun()
		{
			var remove = new HashSet<Bot>();

			foreach (var kvp in _botQueue)
			{
				DateTime time = kvp.Value;
				if ((time - DateTime.Now).TotalSeconds < 0)
				{
					remove.Add(kvp.Key);
				}
			}

			foreach (Bot bot in remove)
			{
				_botQueue.Remove(bot);
				RequestFromBot(bot);
			}
		}

		public void AddBotToQueue(Bot aBot, int aInt)
		{
			if (!_botQueue.ContainsKey(aBot))
			{
				_botQueue.Add(aBot, DateTime.Now.AddSeconds(aInt));
			}
		}

		public void AddChannelToQueue(XG.Core.Channel aChannel, int aInt)
		{
			if (!_channelQueue.ContainsKey(aChannel))
			{
				_channelQueue.Add(aChannel, DateTime.Now.AddSeconds(aInt));
			}
		}

		#endregion
	}
}
