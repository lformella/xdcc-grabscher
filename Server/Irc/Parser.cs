// 
//  Parser.cs
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

using log4net;

using XG.Core;
using XG.Server.Helper;

namespace XG.Server.Irc
{
	/// <summary>
	/// this class parses all messages comming from the server, channel and bot
	/// </summary>
	public class Parser : AParser
	{
		#region VARIABLES

		public Servers Parent { get; set; }

		IntValue _intValue;
		PrivateMessage _privateMessage;
		Notice _notice;

		public FileActions FileActions
		{
			set
			{
				_privateMessage.FileActions = value;
			}
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
		}

		void RegisterParser(AParser aParser)
		{
			aParser.AddDownload += new DownloadDelegate(FireAddDownload);
			aParser.RemoveDownload += new BotDelegate(FireRemoveDownload);
			aParser.ParsingError += new DataTextDelegate(FireParsingError);
			aParser.SendData += new ServerDataTextDelegate(FireSendData);
			aParser.JoinChannel += new ServerChannelDelegate(FireJoinChannel);
			aParser.CreateTimer += new ServerObjectIntBoolDelegate(FireCreateTimer);
			aParser.RequestFromBot += new ServerBotDelegate(FireRequestFromBot);
			aParser.UnRequestFromBot += new ServerBotDelegate(FireUnRequestFromBot);
		}

		#region PARSING

		protected override void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands)
		{
			ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");

			string tUserName = aCommands[0].Split('!')[0];
			string tComCodeStr = aCommands[1];
			string tChannelName = aCommands[2];

			Channel tChan = aServer.Channel(tChannelName);
			Bot tBot = aServer.Bot(tUserName);

			if(tBot != null)
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

			else if (tComCodeStr == "NOTICE")
			{
				_notice.ParseData(aServer, aRawData);
				return;
			}

			#endregion

			#region NICK

			else if (tComCodeStr == "NICK")
			{
				if (tBot != null)
				{
					tBot.Name = aMessage;
					_log.Info("con_DataReceived() bot " + tUserName + " renamed to " + tBot.Name);
				}
				else if(tUserName == Settings.Instance.IRCName)
				{
					// what should i do now?!
					_log.Error("con_DataReceived() wtf? i was renamed to " + aMessage);
				}
			}

			#endregion

			#region KICK

			else if (tComCodeStr == "KICK")
			{
				if (tChan != null)
				{
					tUserName = aCommands[3];
					if (tUserName == Settings.Instance.IRCName)
					{
						tChan.Connected = false;
						_log.Warn("con_DataReceived() kicked from " + tChan.Name + (aCommands.Length >= 5 ? " (" + aCommands[4] + ")" : "") + " - rejoining");
						_log.Warn("con_DataReceived() " + aRawData);
						FireJoinChannel(aServer, tChan);

						// statistics
						Statistic.Instance.Increase(StatisticType.ChannelsKicked);
					}
					else
					{
						tBot = aServer.Bot(tUserName);
						if (tBot != null)
						{
							tBot.Connected = false;
							tBot.LastMessage = "kicked from channel " + tChan.Name;
							_log.Info("con_DataReceived() bot " + tBot.Name + " is offline");
						}
					}
				}
			}

			#endregion

			#region KILL

			else if (tComCodeStr == "KILL")
			{
				tUserName = aCommands[2];
				if (tUserName == Settings.Instance.IRCName)
				{
					_log.Warn("con_DataReceived() i was killed from server because of " + aMessage);
				}
				else
				{
					tBot = aServer.Bot(tUserName);
					if (tBot != null)
					{
						_log.Warn("con_DataReceived() bot " + tBot.Name + " was killed from server?");
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
						_log.Info("con_DataReceived() bot " + tUserName + " is online");
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
						_log.Info("con_DataReceived() bot " + tBot.Name + " parted from " + tChan.Name);
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
					_log.Info("con_DataReceived() bot " + tBot.Name + " quited");
				}
			}

			#endregion

			#region	MODE / TOPIC / WALLOP

			else if (tComCodeStr == "MODE" ||
					 tComCodeStr == "TOPIC" ||
					 tComCodeStr == "WALLOP")
			{
				// uhm, what to do now?!
			}

			#endregion

			#region	INVITE

			else if (tComCodeStr == "INVITE")
			{
				_log.Info("con_DataReceived() received an invite for channel " + aMessage);

				// ok, lets do a silent auto join
				if (Settings.Instance.AutoJoinOnInvite)
				{
					_log.Info("con_DataReceived() auto joining " + aMessage);
					FireSendData(aServer, "JOIN " + aMessage);
				}
			}

			#endregion

			#region PONG

			else if (tComCodeStr == "PONG")
			{
				_log.Info("con_DataReceived() PONG");
			}

			#endregion

			#region	INT VALUES

			else
			{
				_intValue.ParseData(aServer, aRawData);
			}

			#endregion

			if(tBot != null)
			{
				tBot.Commit();
			}
			if(tChan != null)
			{
				tChan.Commit();
			}
		}

		#endregion
	}
}
