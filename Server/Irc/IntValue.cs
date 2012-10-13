// 
//  IntValue.cs
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

using log4net;

using XG.Core;

namespace XG.Server.Irc
{
	public class IntValue : AParser
	{
		#region PARSING

		protected override void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands)
		{
			ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");

			Bot tBot = aServer.Bot(aCommands[0].Split('!')[0]);
			Channel tChan = aServer.Channel(aCommands[2]);
			
			string tComCodeStr = aCommands[1];
			int tComCode = 0;
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
							string[] tChannelList = aRawData.Split(':')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
								_log.Info("Parse() auto adding channel " + chanName);
								aServer.AddChannel(chanName);
							}
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
									_log.Info("Parse() bot " + tBot.Name + " is online");
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
							_log.Info("Parse() joined channel " + tChan.Name);
						}
	
						// statistics
						Statistic.Instance.Increase(StatisticType.ChannelConnectsOk);
						break;
	
					#endregion
	
					#region RPL_ENDOFMOTD | ERR_NOMOTD
	
					case 376: // RPL_ENDOFMOTD
					case 422: // ERR_NOMOTD
						_log.Info("Parse() really connected");
						aServer.Connected = true;
						aServer.Commit();
						foreach (Channel chan in aServer.Channels)
						{
							if (chan.Enabled) { FireJoinChannel(aServer, chan); }
						}
	
						// statistics
						Statistic.Instance.Increase(StatisticType.ServerConnectsOk);
						break;
	
					#endregion
	
					#region ERR_NOCHANMODES
	
					case 477: // ERR_NOCHANMODES
						tChan = aServer.Channel(aCommands[3]);
						// TODO should we nickserv register here?
						/*if(Settings.Instance.AutoRegisterNickserv && Settings.Instance.IrcRegisterPasswort != "" && Settings.Instance.IrcRegisterEmail != "")
						{
							FireSendDataEvent(aServer, "nickserv register " + Settings.Instance.IrcRegisterPasswort + " " + Settings.Instance.IrcRegisterEmail);
						}*/
	
						if(tChan != null)
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
							_log.Warn("Parse() could not join channel " + tChan.Name + ": " + tComCode);
						}
	
						// statistics
						Statistic.Instance.Increase(StatisticType.ChannelConnectsFailed);
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
							_log.Warn("Parse() could not join channel " + tChan.Name + ": " + tComCode);
							FireCreateTimer(aServer, tChan, tComCode == 471 || tComCode == 485 ? Settings.Instance.ChannelWaitTime : Settings.Instance.ChannelWaitTimeLong, false);
						}
	
						// statistics
						Statistic.Instance.Increase(StatisticType.ChannelConnectsFailed);
						break;
	
					#endregion
				}
	
				if(tBot != null)
				{
					tBot.Commit();
				}
				if(tChan != null)
				{
					tChan.Commit();
				}
			}
			else
			{
				_log.Error("Parse() Irc code " + tComCodeStr + " could not be parsed. (" + aRawData + ")");
			}
		}

		#endregion
	}
}

