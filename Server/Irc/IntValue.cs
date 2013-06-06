// 
//  IntValue.cs
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
using System.Text.RegularExpressions;

using XG.Core;

using log4net;

namespace XG.Server.Irc
{
	public class IntValue : AParser
	{
		#region PARSING

		protected override void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");

			Bot tBot = aServer.Bot(aCommands[0].Split('!')[0]);
			Channel tChan = aServer.Channel(aCommands[2]);

			string tComCodeStr = aCommands[1];
			int tComCode;
			if (int.TryParse(tComCodeStr, out tComCode))
			{
				switch (tComCode)
				{
						#region 4

					case 4: // 
						aServer.Connected = true;
						aServer.Commit();
						break;

						#endregion

						#region RPL_WHOISCHANNELS

					case 319: // RPL_WHOISCHANNELS
						tBot = aServer.Bot(aCommands[3]);
						if (tBot != null)
						{
							string chanName = "";
							bool addChan = true;
							string[] tChannelList = aRawData.Split(':')[2].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
							foreach (string chan in tChannelList)
							{
								chanName = "#" + chan.Split('#')[1];
								if (aServer.Channel(chanName) != null)
								{
									addChan = false;
									FireRequestFromBot(aServer, tBot);
									break;
								}
							}
							if (addChan)
							{
								log.Info("Parse() auto adding channel " + chanName);
								aServer.AddChannel(chanName);
							}
						}
						break;

						#endregion

						#region RPL_WHOISCHANNELS

					case 331: // RPL_NOTOPIC
						tChan = aServer.Channel(aCommands[3]);
						if (tChan != null)
						{
							tChan.Topic = "";
						}
						break;

						#endregion

						#region RPL_WHOISCHANNELS

					case 332: // RPL_TOPIC
						tChan = aServer.Channel(aCommands[3]);
						if (tChan != null)
						{
							tChan.Topic = RemoveSpecialIrcChars(aMessage);
						}
						break;

						#endregion

						#region RPL_NAMREPLY

					case 353: // RPL_NAMREPLY
						tChan = aServer.Channel(aCommands[4]);
						if (tChan != null)
						{
							string[] tUsers = aMessage.Split(' ');
							foreach (string user in tUsers)
							{
								tChan.UserCount++;
								string tUser = Regex.Replace(user, "^(@|!|%|\\+){1}", "");
								tBot = tChan.Bot(tUser);
								if (tBot != null)
								{
									tBot.Connected = true;
									tBot.LastMessage = "joined channel " + tChan.Name;
									if (tBot.State != Bot.States.Active)
									{
										tBot.State = Bot.States.Idle;
									}
									log.Info("Parse() " + tBot + " is online");
									tBot.Commit();
									FireRequestFromBot(aServer, tBot);
								}
							}
						}
						break;

						#endregion

						#region RPL_ENDOFNAMES

					case 366: // RPL_ENDOFNAMES
						tChan = aServer.Channel(aCommands[3]);
						if (tChan != null)
						{
							tChan.ErrorCode = 0;
							tChan.Connected = true;
							log.Info("Parse() joined " + tChan);

							FireNotificationAdded(new Notification(Notification.Types.ChannelJoined, tChan));
						}
						break;

						#endregion

						#region RPL_ENDOFMOTD | ERR_NOMOTD

					case 376: // RPL_ENDOFMOTD
					case 422: // ERR_NOMOTD
						log.Info("Parse() really connected");
						aServer.Connected = true;
						aServer.Commit();
						foreach (Channel chan in aServer.Channels)
						{
							if (chan.Enabled)
							{
								FireJoinChannel(aServer, chan);
							}
						}

						FireNotificationAdded(new Notification(Notification.Types.ServerConnected, aServer));
						break;

						#endregion

						#region ERR_NOCHANMODES

					case 477: // ERR_NOCHANMODES
						tChan = aServer.Channel(aCommands[3]);
						// TODO should we nickserv register here?
						/*if(Settings.Instance.AutoRegisterNickserv && Settings.Instance.IrcPasswort != "" && Settings.Instance.IrcRegisterEmail != "")
						{
							FireSendDataEvent(aServer, "nickserv register " + Settings.Instance.IrcPasswort + " " + Settings.Instance.IrcRegisterEmail);
						}*/

						if (tChan != null)
						{
							tChan.ErrorCode = tComCode;
							//CreateTimerEvent(aServer, tChan, Settings.Instance.ChannelWaitTime);
						}
						else
						{
							//tBot = aServer.getBot(tCommandList[3]);
							//if(tBot != null) { CreateTimerEvent(aServer, tBot, Settings.Instance.BotWaitTime); }
						}
						break;

						#endregion

						#region ERR_TOOMANYCHANNELS

					case 405:
						tChan = aServer.Channel(aCommands[3]);
						if (tChan != null)
						{
							tChan.ErrorCode = tComCode;
							tChan.Connected = false;
							log.Warn("Parse() could not join " + tChan + ": " + tComCode);

							FireNotificationAdded(new Notification(Notification.Types.ChannelJoinFailed, tChan));
						}
						break;

						#endregion

						#region ERR_CHANNELISFULL | ERR_INVITEONLYCHAN | ERR_BANNEDFROMCHAN | ERR_BADCHANNELKEY | ERR_UNIQOPPRIVSNEEDED

					case 471: // ERR_CHANNELISFULL
					case 473: // ERR_INVITEONLYCHAN
					case 474: // ERR_BANNEDFROMCHAN
					case 475: // ERR_BADCHANNELKEY
					case 485: // ERR_UNIQOPPRIVSNEEDED
						tChan = aServer.Channel(aCommands[3]);
						if (tChan != null)
						{
							tChan.ErrorCode = tComCode;
							tChan.Connected = false;
							log.Warn("Parse() could not join " + tChan + ": " + tComCode);

							int tWaitTime = 0;
							switch (tComCode)
							{
								case 471:
									tWaitTime = Settings.Instance.ChannelWaitTimeShort;
									break;

								case 473:
								case 485:
									tWaitTime = Settings.Instance.ChannelWaitTimeMedium;
									break;

								case 474:
									tWaitTime = Settings.Instance.ChannelWaitTimeLong;
									break;
							}
							if (tWaitTime > 0)
							{
								FireCreateTimer(aServer, tChan, tWaitTime, false);
							}

							FireNotificationAdded(new Notification(tComCode == 471 ? Notification.Types.ChannelBanned : Notification.Types.ChannelJoinFailed, tChan));
						}
						break;

						#endregion
				}

				if (tBot != null)
				{
					tBot.Commit();
				}
				if (tChan != null)
				{
					tChan.Commit();
				}
			}
			else
			{
				log.Error("Parse() Irc code " + tComCodeStr + " could not be parsed. (" + aRawData + ")");
			}
		}

		#endregion
	}
}
