// 
//  Parser.cs
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
using System.Reflection;

using XG.Core;
using XG.Server.Helper;

using log4net;

namespace XG.Server.Irc
{
	/// <summary>
	/// 	this class parses all messages comming from the server, channel and bot
	/// </summary>
	public class Parser : AParser
	{
		#region VARIABLES

		readonly IntValue _intValue;
		readonly PrivateMessage _privateMessage;
		readonly Notice _notice;
		readonly Nickserv _nickserv;

		public FileActions FileActions
		{
			set { _privateMessage.FileActions = value; }
		}

		#endregion

		public Parser()
		{
			_intValue = new IntValue();
			RegisterParser(_intValue);

			_privateMessage = new PrivateMessage();
			RegisterParser(_privateMessage);

			_notice = new Notice();
			RegisterParser(_notice);

			_nickserv = new Nickserv();
			RegisterParser(_nickserv);
		}

		void RegisterParser(AParser aParser)
		{
			aParser.AddDownload += FireAddDownload;
			aParser.RemoveDownload += FireRemoveDownload;
			aParser.ParsingError += FireParsingError;
			aParser.SendData += FireSendData;
			aParser.JoinChannel += FireJoinChannel;
			aParser.CreateTimer += FireCreateTimer;
			aParser.RequestFromBot += FireRequestFromBot;
			aParser.UnRequestFromBot += FireUnRequestFromBot;
		}

		#region PARSING

		protected override void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");

			string tUserName = aCommands[0].Split('!')[0];
			string tComCodeStr = aCommands[1];
			string tChannelName = aCommands[2];

			Channel tChan = aServer.Channel(tChannelName);
			Bot tBot = aServer.Bot(tUserName);

			// dont hammer plugins with not needed information updates - 60 seconds are enough
			if (tBot != null && (DateTime.Now - tBot.LastContact).TotalSeconds > 60)
			{
				tBot.LastContact = DateTime.Now;
			}

			#region PRIVMSG

			if (tComCodeStr == "PRIVMSG")
			{
				_privateMessage.ParseData(aServer, aRawData);
				return;
			}

				#endregion

				#region NOTICE

			if (tComCodeStr == "NOTICE")
			{
				if (tUserName.ToLower() == "nickserv")
				{
					_nickserv.ParseData(aServer, aRawData);
				}
				else
				{
					_notice.ParseData(aServer, aRawData);
				}
				return;
			}

				#endregion

				#region NICK

			if (tComCodeStr == "NICK")
			{
				if (tBot != null)
				{
					tBot.Name = aMessage;
					log.Info("con_DataReceived() bot " + tUserName + " renamed to " + tBot);
				}
				else if (tUserName == Settings.Instance.IrcNick)
				{
					// what should i do now?!
					log.Error("con_DataReceived() wtf? i was renamed to " + aMessage);
				}
			}

				#endregion

				#region KICK

			else if (tComCodeStr == "KICK")
			{
				if (tChan != null)
				{
					tUserName = aCommands[3];
					if (tUserName == Settings.Instance.IrcNick)
					{
						tChan.Connected = false;
						log.Warn("con_DataReceived() kicked from " + tChan + (aCommands.Length >= 5 ? " (" + aCommands[4] + ")" : "") + " - rejoining");
						log.Warn("con_DataReceived() " + aRawData);
						FireJoinChannel(aServer, tChan);

						FireNotificationAdded(new Notification(Notification.Types.ChannelKicked, tChan));
					}
					else
					{
						tBot = aServer.Bot(tUserName);
						if (tBot != null)
						{
							tBot.Connected = false;
							tBot.LastMessage = "kicked from " + tChan;
							log.Info("con_DataReceived() " + tBot + " is offline");
						}
					}
				}
			}

				#endregion

				#region KILL

			else if (tComCodeStr == "KILL")
			{
				tUserName = aCommands[2];
				if (tUserName == Settings.Instance.IrcNick)
				{
					log.Warn("con_DataReceived() i was killed from server because of " + aMessage);
				}
				else
				{
					tBot = aServer.Bot(tUserName);
					if (tBot != null)
					{
						log.Warn("con_DataReceived() " + tBot + " was killed from server?");
					}
				}
			}

				#endregion

				#region JOIN

			else if (tComCodeStr == "JOIN")
			{
				tChannelName = aMessage;
				tChan = aServer.Channel(tChannelName);
				if (tChan != null)
				{
					if (tBot != null)
					{
						tBot.Connected = true;
						tBot.LastMessage = "joined channel " + tChan.Name;
						if (tBot.State != Bot.States.Active)
						{
							tBot.State = Bot.States.Idle;
						}
						log.Info("con_DataReceived() " + tBot + " is online");
						FireRequestFromBot(aServer, tBot);
					}
				}
			}

				#endregion

				#region PART

			else if (tComCodeStr == "PART")
			{
				if (tChan != null)
				{
					if (tBot != null)
					{
						tBot.Connected = true;
						tBot.LastMessage = "parted channel " + tChan.Name;
						log.Info("con_DataReceived() " + tBot + " parted from " + tChan);
					}
				}
			}

				#endregion

				#region QUIT

			else if (tComCodeStr == "QUIT")
			{
				if (tBot != null)
				{
					tBot.Connected = false;
					tBot.LastMessage = "quited";
					log.Info("con_DataReceived() " + tBot + " quited");
				}
			}

				#endregion

				#region	MODE / TOPIC / WALLOP

			else if (tComCodeStr == "MODE" || tComCodeStr == "TOPIC" || tComCodeStr == "WALLOP")
			{
				// uhm, what to do now?!
			}

				#endregion

				#region	INVITE

			else if (tComCodeStr == "INVITE")
			{
				log.Info("con_DataReceived() received an invite for channel " + aMessage);

				// ok, lets do a silent auto join
				if (Settings.Instance.AutoJoinOnInvite)
				{
					log.Info("con_DataReceived() auto joining " + aMessage);
					FireSendData(aServer, "JOIN " + aMessage);
				}
			}

				#endregion

				#region PONG

			else if (tComCodeStr == "PONG")
			{
				log.Info("con_DataReceived() PONG");
			}

				#endregion

				#region	INT VALUES

			else
			{
				_intValue.ParseData(aServer, aRawData);
			}

			#endregion

			if (tBot != null)
			{
				tBot.Commit();
			}
			if (tChan != null)
			{
				tChan.Commit();
			}
		}

		#endregion
	}
}
