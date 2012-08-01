//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;

using log4net;

using XG.Core;
using XG.Server.Connection;
using XG.Server.Helper;

namespace XG.Server
{
	/// <summary>
	/// This class describes the connection to a single irc server
	/// it does the following things
	/// - parsing all messages comming from the server, channel and bot
	/// - creating and removing bots on the fly
	/// - creating and removing packets on the fly (if the bot posts them into the channel)
	/// - communicate with the bot to handle downloads
	/// </summary>	
	public class ServerConnection : AIrcConnection
	{
		#region VARIABLES

		private static readonly ILog log = LogManager.GetLogger(typeof(ServerConnection));

		private XGServer server;
		public XGServer Server
		{
			set
			{
				if(this.server != null)
				{
					this.server.ChildAddedEvent -= new ObjectObjectDelegate(Server_ChildAddedEventHandler);
					this.server.ChildRemovedEvent -= new ObjectObjectDelegate(Server_ChildRemovedEventHandler);
					this.server.EnabledChangedEvent -= new ObjectDelegate(Server_EnabledChangedEventHandler);
				}
				this.server = value;
				if(this.server != null)
				{
					this.server.ChildAddedEvent += new ObjectObjectDelegate(Server_ChildAddedEventHandler);
					this.server.ChildRemovedEvent += new ObjectObjectDelegate(Server_ChildRemovedEventHandler);
					this.server.EnabledChangedEvent -= new ObjectDelegate(Server_EnabledChangedEventHandler);
				}
			}
		}

		private IrcParser ircParser;
		public IrcParser IrcParser
		{
			set
			{
				if(this.ircParser != null)
				{
					this.ircParser.SendDataEvent += new DataTextDelegate(SendData);
					this.ircParser.JoinChannelEvent += new ChannelDelegate(JoinChannel);
					this.ircParser.CreateTimerEvent += new ObjectIntBoolDelegate(CreateTimer);

					this.ircParser.RequestFromBotEvent += new BotDelegate(RequestFromBot);
					this.ircParser.UnRequestFromBotEvent += new BotDelegate(UnRequestFromBot);
				}
				this.ircParser = value;
				if(this.ircParser != null)
				{
					this.ircParser.SendDataEvent -= new DataTextDelegate(SendData);
					this.ircParser.JoinChannelEvent -= new ChannelDelegate(JoinChannel);
					this.ircParser.CreateTimerEvent -= new ObjectIntBoolDelegate(CreateTimer);

					this.ircParser.RequestFromBotEvent -= new BotDelegate(RequestFromBot);
					this.ircParser.UnRequestFromBotEvent -= new BotDelegate(UnRequestFromBot);
				}
			}
		}

		private bool isRunning = false;
		public bool IsRunning { get { return this.isRunning; } }

		private Dictionary<XGObject, DateTime> timedObjects;
		private Dictionary<string, DateTime> latestPacketRequests;

		#endregion

		#region EVENTS

		public event ServerDelegate ConnectedEvent;
		public event ServerSocketErrorDelegate DisconnectedEvent;

		#endregion

		#region CONNECTION

		protected override void Connection_ConnectedEventHandler()
		{
			this.SendData("NICK " + Settings.Instance.IRCName);
			this.SendData("USER " + Settings.Instance.IRCName + " " + Settings.Instance.IRCName + " " + this.server.Name + " :root");

			this.timedObjects = new Dictionary<XGObject, DateTime>();
			this.latestPacketRequests = new Dictionary<string, DateTime>();
			this.isRunning = true;

			this.server.ErrorCode = SocketErrorCode.None;
			this.server.Commit();

			this.ConnectedEvent(this.server);
		}

		protected override void Connection_DisconnectedEventHandler(SocketErrorCode aValue)
		{
			this.isRunning = false;

			this.server.ErrorCode = aValue;
			this.server.Connected = false;
			this.server.Commit();

			if (this.timedObjects != null) { this.timedObjects.Clear(); }
			if (this.latestPacketRequests != null) { this.latestPacketRequests.Clear(); }

			this.DisconnectedEvent(this.server, aValue);
		}

		private void SendData(string aData)
		{
			if(this.Connection != null)
			{
				this.Connection.SendData(aData);
			}
		}

		protected override void Connection_DataReceivedEventHandler(byte[] aData)
		{
		}

		protected override void Connection_DataReceivedEventHandler(string aData)
		{
			log.Debug("con_DataReceived(" + aData + ")");

			this.ircParser.ParseData(this.server, aData);
		}

		#endregion

		#region BOT

		private void RequestFromBot(object aBot)
		{
			XGBot tBot = aBot as XGBot;
			if (tBot != null)
			{
				if (tBot.BotState == BotState.Idle)
				{
					// check if the packet is already downloaded, or active - than disable it and get the next one
					XGPacket tPacket = tBot.GetOldestActivePacket();
					while (tPacket != null)
					{
						Int64 tChunk = this.Parent.GetNextAvailablePartSize(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, tPacket.RealSize != 0 ? tPacket.RealSize : tPacket.Size);
						if (tChunk == -1 || tChunk == -2)
						{
							log.Warn("RequestFromBot(" + tBot.Name + ") packet #" + tPacket.Id + " (" + tPacket.Name + ") is already in use");
							tPacket.Enabled = false;
							tPacket.Commit();
							tPacket = tBot.GetOldestActivePacket();
						}
						else
						{
							string name = XGHelper.ShrinkFileName(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, 0);
							if (this.latestPacketRequests.ContainsKey(name))
							{
								double time = (this.latestPacketRequests[name] - DateTime.Now).TotalMilliseconds;
								if (time > 0)
								{
									log.Warn("RequestFromBot(" + tBot.Name + ") packet name " + tPacket.Name + " is blocked for " + time + "ms");
									this.CreateTimer(tBot, (long)time + 1000, false);
									return;
								}
							}

							if (this.server.Connected)
							{
								log.Info("RequestFromBot(" + tBot.Name + ") requesting packet #" + tPacket.Id + " (" + tPacket.Name + ")");
								this.SendData("PRIVMSG " + tBot.Name + " :\u0001XDCC SEND " + tPacket.Id + "\u0001");

								if (this.latestPacketRequests.ContainsKey(name)) { this.latestPacketRequests.Remove(name); }
								this.latestPacketRequests.Add(name, DateTime.Now.AddMilliseconds(Settings.Instance.SamePacketRequestTime));

								// statistics
								Statistic.Instance.Increase(StatisticType.PacketsRequested);
							}

							// create a timer to re request if the bot didnt recognized the privmsg
							this.CreateTimer(tBot, Settings.Instance.BotWaitTime, false);
							break;
						}
					}
				}
			}
		}

		private void UnRequestFromBot(XGBot aBot)
		{
			if (aBot != null) // && myServer[aBot.Name] != null)
			{
				log.Info("UnregisterFromBot(" + aBot.Name + ")");
				this.SendData("PRIVMSG " + aBot.Name + " :\u0001XDCC REMOVE\u0001");
				this.CreateTimer(aBot, Settings.Instance.CommandWaitTime, false);

				// statistics
				Statistic.Instance.Increase(StatisticType.PacketsRemoved);
			}
		}

		#endregion

		#region CHANNEL

		private void JoinChannel(object aChan)
		{
			XGChannel tChan = aChan as XGChannel;
			// only join if the channel isnt connected
			if (tChan != null && server[tChan.Name] != null && !tChan.Connected)
			{
				log.Info("JoinChannel(" + tChan.Name + ")");
				this.SendData("JOIN " + tChan.Name);

				// TODO maybe set a time to resend the command if the channel is not connected
				// it happend to me, that some available channels werent joined because no confirm messaes appeared

				// statistics
				Statistic.Instance.Increase(StatisticType.ChannelsJoined);
			}
		}

		public void PartChannel(XGChannel aChan)
		{
			if (aChan != null)
			{
				log.Info("PartChannel(" + aChan.Name + ")");
				this.SendData("PART " + aChan.Name);

				// statistics
				Statistic.Instance.Increase(StatisticType.ChannelsParted);
			}
		}

		#endregion

		#region EVENTHANDLER

		private void Server_ChildAddedEventHandler(XGObject aParent, XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGChannel))
			{
				XGChannel aChan = aObj as XGChannel;

				if (aChan.Enabled)
				{
					this.JoinChannel(aChan);
				}
			}
		}

		private void Server_ChildRemovedEventHandler(XGObject aParent, XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGChannel))
			{
				XGChannel aChan = aObj as XGChannel;

				foreach (XGBot tBot in aChan.Bots)
				{
					foreach (XGPacket tPack in tBot.Packets)
					{
						tPack.Enabled = false;
						tPack.Commit();
					}
				}

				this.PartChannel(aChan);
			}
		}

		private void Server_EnabledChangedEventHandler(XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGChannel))
			{
				XGChannel tChan = aObj as XGChannel;
	
				if (tChan.Enabled)
				{
					this.JoinChannel(tChan);
				}
				else
				{
					this.PartChannel(tChan);
				}
			}

			if(aObj.GetType() == typeof(XGPacket))
			{
				XGPacket tPack = aObj as XGPacket;
				XGBot tBot = tPack.Parent;

				if (tPack.Enabled)
				{
					if (tBot.GetOldestActivePacket() == tPack) { this.RequestFromBot(tBot); }
				}
				else
				{
					if (tBot.BotState == BotState.Waiting || tBot.BotState == BotState.Active)
					{
						XGPacket tmp = tBot.GetCurrentQueuedPacket();
						if (tmp == tPack)
						{
							this.UnRequestFromBot(tBot);
						}
					}
				}
			}
		}

		#endregion

		#region TIMER

		/// <summary>
		/// Is called from the parent onject ServerHandler (to have a single loop which triggers all ServerConnects) 
		/// </summary>
		public void TriggerTimerRun()
		{
			List<XGObject> remove = new List<XGObject>();
			foreach (KeyValuePair<XGObject, DateTime> kvp in this.timedObjects)
			{
				DateTime time = kvp.Value;
				if ((time - DateTime.Now).TotalMilliseconds < 0) { remove.Add(kvp.Key); }
			}
			foreach (XGObject obj in remove)
			{
				this.timedObjects.Remove(obj);

				if (obj.GetType() == typeof(XGChannel)) { this.JoinChannel(obj as XGChannel); }
				else if (obj.GetType() == typeof(XGBot)) { this.RequestFromBot(obj as XGBot); }
			}

			//this.SendData("PING " + this.myServer.Name);
		}

		public void CreateTimer(XGObject aObject, Int64 aTime, bool aOverride)
		{
			if(aObject == null)
			{
				log.Fatal("CreateTimer(null, " + aTime + ", " + aOverride + ") object is null!");
				return;
			}
			if (aOverride && this.timedObjects.ContainsKey(aObject))
			{
				this.timedObjects.Remove(aObject);
			}

			if (!this.timedObjects.ContainsKey(aObject))
			{
				this.timedObjects.Add(aObject, DateTime.Now.AddMilliseconds(aTime));
			}
		}

		#endregion
	}
}
