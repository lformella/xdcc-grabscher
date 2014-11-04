//
//  XdccClient.cs
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
using System.Threading;
using Meebey.SmartIrc4net;
using XG.Config.Properties;
using XG.Extensions;
using XG.Model.Domain;
using XG.Plugin;
using log4net;

namespace XG.Plugin.Irc
{
	public class XdccClient : ANotificationSender
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly IrcClient _client = new IrcClient
		{
			AutoNickHandling = true,
			ActiveChannelSyncing = true,
			AutoRetry = true,
			AutoJoinOnInvite = true,
			AutoRejoinOnKick = true,
			CtcpVersion = Settings.Default.IrcVersion,
			SupportNonRfc = true
		};

		string _iam = Settings.Default.IrcNick;

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
					_server.OnAdded -= ObjectAdded;
					_server.OnRemoved -= ObjectRemoved;
					_server.OnEnabledChanged -= ObjectEnabledChanged;
				}
				_server = value;
				if (_server != null)
				{
					_server.OnAdded += ObjectAdded;
					_server.OnRemoved += ObjectRemoved;
					_server.OnEnabledChanged += ObjectEnabledChanged;
				}
			}
		}

		public bool IsConnected
		{
			get { return _client.IsConnected; }
		}

		readonly Queue<IrcEvent> _events = new Queue<IrcEvent>();

		AutoResetEvent _waitHandle = new AutoResetEvent(false);

		bool _allowRunning = true;

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<Server>> OnConnected = delegate {};
		public event EventHandler<EventArgs<Server>> OnDisconnected = delegate {};
		public event EventHandler<EventArgs<Model.Domain.Channel, string, string>> OnMessage = delegate {};
		public event EventHandler<EventArgs<string>> OnReadLine = delegate {};
		public event EventHandler<EventArgs<Model.Domain.Channel>> OnChannelJoined = delegate {};
		public event EventHandler<EventArgs<Bot>> OnBotJoined = delegate {};
		public event EventHandler<EventArgs<Model.Domain.Channel, string>> OnUserJoined = delegate {};
		public event EventHandler<EventArgs<Model.Domain.Channel, int>> OnQueueChannel = delegate {};

		#endregion

		#region EVENTHANDLER SERVER

		void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			var aChan = aEventArgs.Value2 as Model.Domain.Channel;
			if (aChan != null)
			{
				if (aChan.Enabled)
				{
					Join(aChan);
				}
			}
		}

		void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			var aChan = aEventArgs.Value2 as Model.Domain.Channel;
			if (aChan != null)
			{
				var packets = (from bot in aChan.Bots from packet in bot.Packets select packet).ToArray();
				foreach (Packet tPack in packets)
				{
					tPack.Enabled = false;
				}

				Part(aChan);
			}
		}

		void ObjectEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			var aChan = aEventArgs.Value1 as Model.Domain.Channel;
			if (aChan != null)
			{
				if (aChan.Enabled)
				{
					Join(aChan);
				}
				else
				{
					Part(aChan);
				}
			}
		}

		#endregion

		#region EVENTHANDLER CLIENT

		void ClientOnBan(object sender, BanEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Ban, Event = e });
			_waitHandle.Set();
		}
			
		void ClientOnChannelMessage(object sender, IrcEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.ChannelMessage, Event = e });
			_waitHandle.Set();
		}

		void ClientOnConnected(object sender, EventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Connected, Event = e });
			_waitHandle.Set();
		}

		void ClientOnCtcpReply(object sender, CtcpEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.CtcpReply, Event = e });
			_waitHandle.Set();
		}

		void ClientOnCtcpRequest(object sender, CtcpEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.CtcpRequest, Event = e });
			_waitHandle.Set();
		}

		void ClientOnErrorMessage(object sender, IrcEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.ErrorMessage, Event = e });
			_waitHandle.Set();
		}

		void ClientOnJoin(object sender, JoinEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Join, Event = e });
			_waitHandle.Set();
		}

		void ClientOnKick(object sender, KickEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Kick, Event = e });
			_waitHandle.Set();
		}

		void ClientOnPart(object sender, PartEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Part, Event = e });
			_waitHandle.Set();
		}

		void ClientOnNames(object sender, NamesEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Names, Event = e });
			_waitHandle.Set();
		}

		void ClientOnNickChange(object sender, NickChangeEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.NickChange, Event = e });
			_waitHandle.Set();
		}

		void ClientOnQueryMessage(object sender, IrcEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.QueryMessage, Event = e });
			_waitHandle.Set();
		}

		void ClientOnQueryNotice(object sender, IrcEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.QueryNotice, Event = e });
			_waitHandle.Set();
		}

		void ClientOnQuit(object sender, QuitEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Quit, Event = e });
			_waitHandle.Set();
		}

		void ClientOnReadLine(object sender, ReadLineEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.ReadLine, Event = e });
			_waitHandle.Set();
		}

		void ClientOnTopic(object sender, TopicEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.Topic, Event = e });
			_waitHandle.Set();
		}

		void ClientOnTopicChange(object sender, TopicChangeEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.TopicChange, Event = e });
			_waitHandle.Set();
		}

		void ClientOnUnBan(object sender, UnbanEventArgs e)
		{
			_events.Enqueue(new IrcEvent { Type = IrcEvent.EventType.UnBan, Event = e });
			_waitHandle.Set();
		}

		protected void EventThread()
		{
			IrcEvent tEvent = null;
			while (true)
			{
				if (_events.Count == 0)
				{
					_waitHandle.WaitOne();
				}
				if (!_allowRunning)
				{
					break;
				}

				try
				{
					tEvent = _events.Dequeue();
				}
				catch (Exception) {}
				if (tEvent == null)
				{
					continue;
				}

				// run
				switch (tEvent.Type)
				{
					case IrcEvent.EventType.Ban:
						ClientBan((BanEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.ChannelMessage:
						ClientChannelMessage((IrcEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.Connected:
						ClientConnected(tEvent.Event);
						break;
					case IrcEvent.EventType.CtcpReply:
						MessageReceived((CtcpEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.CtcpRequest:
						MessageReceived((CtcpEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.ErrorMessage:
						ClientErrorMessage((IrcEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.Join:
						ClientJoin((JoinEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.Kick:
						ClientKick((KickEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.Part:
						ClientPart((PartEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.Names:
						ClientNames((NamesEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.NickChange:
						ClienNickChange((NickChangeEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.QueryMessage:
						MessageReceived((IrcEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.QueryNotice:
						MessageReceived((IrcEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.Quit:
						ClientQuit((QuitEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.ReadLine:
						OnReadLine(this, new EventArgs<string>(((ReadLineEventArgs) tEvent.Event).Line));
						break;
					case IrcEvent.EventType.Topic:
						ClientTopic((TopicEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.TopicChange:
						ClientTopicChange((TopicChangeEventArgs) tEvent.Event);
						break;
					case IrcEvent.EventType.UnBan:
						ClientUnBan((UnbanEventArgs) tEvent.Event);
						break;
				}
			}
		}

		void ClientBan (BanEventArgs e)
		{
			var channel = Server.Channel(e.Channel);
			if (channel != null)
			{
				if (_iam == e.Who)
				{
					channel.Connected = false;
					_log.Warn("banned from " + channel.Name);
					FireNotificationAdded(Notification.Types.ChannelBanned, channel);
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
		}

		void ClientChannelMessage(IrcEventArgs e)
		{
			Model.Domain.Channel tChan = null;
			if (e.Data.Type == ReceiveType.QueryNotice)
			{
				if (!String.IsNullOrEmpty(e.Data.Nick))
				{
					var user = _client.GetIrcUser(e.Data.Nick);
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
				}
			}
			else
			{
				tChan = Server.Channel(e.Data.Channel);
			}
			if (tChan != null)
			{
				OnMessage(this, new EventArgs<Model.Domain.Channel, string, string>(tChan, e.Data.Nick, e.Data.Message));
			}
		}

		void ClientConnected(EventArgs e)
		{
			Server.Connected = true;
			Server.Commit();
			FireNotificationAdded(Notification.Types.ServerConnected, Server);
			_log.Info("connected " + Server);
			_client.Login(Settings.Default.IrcNick, Settings.Default.IrcNick, 0, Settings.Default.IrcNick, Settings.Default.IrcPasswort);
			if (Server.Channels.Count > 0)
			{
				var channels = (from channel in Server.Channels
				where channel.Enabled
				select channel.Name).ToArray();
				if (channels.Any())
				{
					_client.RfcJoin(channels);
				}
			}
			OnConnected(this, new EventArgs<Server>(Server));
		}

		void ClientErrorMessage(IrcEventArgs e)
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
						tWaitTime = Settings.Default.ChannelWaitTimeShort;
						break;
					case ReplyCode.ErrorInviteOnlyChannel:
					case ReplyCode.ErrorUniqueOpPrivilegesNeeded:
						tWaitTime = Settings.Default.ChannelWaitTimeMedium;
						break;
					case ReplyCode.ErrorBannedFromChannel:
						tWaitTime = Settings.Default.ChannelWaitTimeLong;
						break;
				}
				if (tWaitTime > 0)
				{
					channel.ErrorCode = (int)e.Data.ReplyCode;
					channel.Connected = false;
					_log.Warn("could not join " + channel + ": " + e.Data.ReplyCode);
					FireNotificationAdded(notificationType, channel);
					OnQueueChannel(this, new EventArgs<Model.Domain.Channel, int>(channel, tWaitTime));
				}
				channel.Commit();
			}
		}

		void ClientJoin(JoinEventArgs e)
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

					OnChannelJoined(this, new EventArgs<Model.Domain.Channel>(channel));
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
						OnBotJoined(this, new EventArgs<Bot>(bot));
					}
					else
					{
						OnUserJoined(this, new EventArgs<Model.Domain.Channel, string>(channel, e.Who));
					}
				}
				UpdateChannel(channel);
			}
		}

		void ClientKick(KickEventArgs e)
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
		}

		void ClientPart(PartEventArgs e)
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
		}

		void ClientNames(NamesEventArgs e)
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
						OnBotJoined(this, new EventArgs<Bot>(bot));
					}
					else
					{
						OnUserJoined(this, new EventArgs<Model.Domain.Channel, string>(channel, user));
					}
				}
				UpdateChannel(channel);
			}
		}

		void ClienNickChange(NickChangeEventArgs e)
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
		}

		void ClientQuit(QuitEventArgs e)
		{
			var bot = Server.Bot(e.Who);
			if (bot != null)
			{
				bot.Connected = false;
				bot.LastMessage = "quited";
				UpdateBot(bot);
				UpdateChannel(bot.Parent);
			}
		}

		void ClientTopic(TopicEventArgs e)
		{
			var channel = Server.Channel(e.Channel);
			if (channel != null)
			{
				channel.Topic = Irc.Parser.Helper.RemoveSpecialIrcChars(e.Topic);
				channel.Commit();
			}
		}

		void ClientTopicChange(TopicChangeEventArgs e)
		{
			var channel = Server.Channel(e.Channel);
			if (channel != null)
			{
				channel.Topic = Irc.Parser.Helper.RemoveSpecialIrcChars(e.NewTopic);
				channel.Commit();
			}
		}

		void ClientUnBan(UnbanEventArgs e)
		{
			var channel = Server.Channel(e.Channel);
			if (channel != null)
			{
				if (_iam == e.Who)
				{
					channel.ErrorCode = 0;
					channel.Commit();
					OnQueueChannel(this, new EventArgs<Model.Domain.Channel, int>(channel, Settings.Default.CommandWaitTime));
				}
			}
		}

		void MessageReceived(IrcEventArgs e)
		{
			Model.Domain.Channel tChan = null;

			if (e.Data.Type == ReceiveType.QueryNotice || e is CtcpEventArgs)
			{
				if (!String.IsNullOrEmpty(e.Data.Nick))
				{
					var user = _client.GetIrcUser(e.Data.Nick);
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
				}
			}
			else
			{
				tChan = Server.Channel(e.Data.Channel);
			}

			if (tChan != null)
			{
				OnMessage(this, new EventArgs<Model.Domain.Channel, string, string>(tChan, e.Data.Nick,  e.Data.Message));
			}
		}

		#endregion

		#region FUNCTIONS

		public void Connect()
		{
			Server.Connected = false;
			Server.Commit();
			_allowRunning = true;

			_client.OnBan += ClientOnBan;
			_client.OnConnected += ClientOnConnected;
			_client.OnChannelMessage += ClientOnChannelMessage;
			_client.OnCtcpReply += ClientOnCtcpReply;
			_client.OnCtcpRequest += ClientOnCtcpRequest;
			_client.OnErrorMessage += ClientOnErrorMessage;
			_client.OnJoin += ClientOnJoin;
			_client.OnKick += ClientOnKick;
			_client.OnNames += ClientOnNames;
			_client.OnNickChange += ClientOnNickChange;
			_client.OnPart += ClientOnPart;
			_client.OnQueryMessage += ClientOnQueryMessage;
			_client.OnQueryNotice += ClientOnQueryNotice;
			_client.OnQuit += ClientOnQuit;
			_client.OnTopic += ClientOnTopic;
			_client.OnTopicChange += ClientOnTopicChange;
			_client.OnUnban += ClientOnUnBan;
			_client.OnReadLine += ClientOnReadLine;

			try
			{
				new Thread(new ThreadStart(EventThread)).Start();

				_log.Info("connecting " + Server);
				_client.Connect(Server.Name, Server.Port);

				// this is blocking, so we have a straight flow
				_client.Listen();
			}
			catch (CouldNotConnectException ex)
			{
				_log.Warn("Connect() connection failed " + ex.Message);

				FireNotificationAdded(Notification.Types.ServerConnectFailed, Server);
			}
			catch (Exception ex)
			{
				_log.Fatal("Connect() connection failed " + ex.Message);

				FireNotificationAdded(Notification.Types.ServerConnectFailed, Server);
			}

			_log.Info("connection stoped " + Server);

			Server.Connected = false;
			Server.Commit();

			_allowRunning = false;
			_waitHandle.Set();

			_client.OnBan -= ClientOnBan;
			_client.OnConnected -= ClientOnConnected;
			_client.OnChannelMessage -= ClientOnChannelMessage;
			_client.OnCtcpReply -= ClientOnCtcpReply;
			_client.OnCtcpRequest -= ClientOnCtcpRequest;
			_client.OnErrorMessage -= ClientOnErrorMessage;
			_client.OnJoin -= ClientOnJoin;
			_client.OnKick -= ClientOnKick;
			_client.OnNames -= ClientOnNames;
			_client.OnNickChange -= ClientOnNickChange;
			_client.OnPart -= ClientOnPart;
			_client.OnQueryMessage -= ClientOnQueryMessage;
			_client.OnQueryNotice -= ClientOnQueryNotice;
			_client.OnQuit -= ClientOnQuit;
			_client.OnTopic -= ClientOnTopic;
			_client.OnTopicChange -= ClientOnTopicChange;
			_client.OnUnban -= ClientOnUnBan;
			_client.OnReadLine -= ClientOnReadLine;

			if (OnDisconnected != null)
			{
				OnDisconnected(this, new EventArgs<Server>(Server));
			}
		}

		public void Disconnect()
		{
			try
			{
				_client.Disconnect();
			}
			catch (NotConnectedException) {}
		}

		public void Join(XG.Model.Domain.Channel aChannel)
		{
			Join(aChannel.Name);
		}

		public void Join(string aChannel)
		{
			try
			{
				_client.RfcJoin(aChannel);
			}
			catch (NotConnectedException) {}
		}

		public void TryJoinBotChannels(Bot aBot)
		{
			if (!_client.IsConnected)
			{
				return;
			}
			var user = _client.GetIrcUser(aBot.Name);
			if (user != null)
			{
				_log.Info("JoinChannelsFromBot(" + aBot + ")");
				try
				{
					_client.RfcJoin(user.JoinedChannels);
				}
				catch (NotConnectedException) {}
			}
		}

		public void Part(XG.Model.Domain.Channel aChan)
		{
			try
			{
				_client.RfcPart(aChan.Name);
			}
			catch (NotConnectedException) {}
		}

		public void SendMessage(string aUser, string aCommand)
		{
			SendMessage(SendType.Message, aUser, aCommand);
		}

		public void SendMessage(SendType aType, string aUser, string aCommand)
		{
			_client.SendMessage(aType, aUser, aCommand);
		}

		public void WriteLine(string aCommand)
		{
			try
			{
				_client.WriteLine(aCommand);
			}
			catch (NotConnectedException) {}
		}

		public void Version(string aUser)
		{
			SendMessage(SendType.CtcpRequest, aUser, Rfc2812.Version());
		}

		public void XdccSend(Packet aPacket)
		{
			try
			{
				_client.SendMessage(SendType.Message, aPacket.Parent.Name, "XDCC SEND " + aPacket.Id);
			}
			catch (NotConnectedException) {}
		}

		public void XdccRemove(Bot aBot)
		{
			try
			{
				_client.SendMessage(SendType.Message, aBot.Name, "XDCC REMOVE");
			}
			catch (NotConnectedException) {}
		}

		public bool IsUserMaybeeXdccBot(string aChannel, string aUser)
		{
			var user = (NonRfcChannelUser) _client.GetChannelUser(aChannel, aUser);
			if (user != null)
			{
				// dont version ops!
				if (user.IsIrcOp || user.IsOwner || user.IsOp || user.IsHalfop)
				{
					return false;
				}
				// just aks voiced users because they could be bots
				if (!user.IsVoice)
				{
					return false;
				}
			}
			else
			{
				// just ask users who are named like bots
				if (!Irc.Parser.Helper.Match(aUser, ".*XDCC.*").Success)
				{
					return false;
				}
			}

			return true;
		}

		public IrcUser GetIrcUser(string aUser)
		{
			return _client.GetIrcUser(aUser);
		}

		#endregion

		#region IRC

		void UpdateChannel(Model.Domain.Channel aChannel)
		{
			var channel = (NonRfcChannel) _client.GetChannel(aChannel.Name);
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

		#endregion
	}
}
