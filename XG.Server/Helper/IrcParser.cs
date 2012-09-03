// 
//  IrcParser.cs
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
using System.Text.RegularExpressions;

using log4net;

using XG.Core;
using System.Net;
using System.Threading;

namespace XG.Server.Helper
{
	public delegate void BotDelegate (Bot aBot);

	/// <summary>
	/// this class parses all messages comming from the server, channel and bot
	/// </summary>
	public class IrcParser
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(IrcParser));

		public Servers Parent { get; set; }
		public FileActions FileActions { get; set; }

		const string MAGICSTRING = "((\\*|:){2,3}|->|<-|)";

		#endregion

		#region EVENTS
		
		public event DownloadDelegate AddDownload;
		public event BotDelegate RemoveDownload;
		public event DataTextDelegate ParsingError;

		public event ServerDataTextDelegate SendData;
		public event ServerChannelDelegate JoinChannel;
		public event ServerObjectIntBoolDelegate CreateTimer;

		public event ServerBotDelegate RequestFromBot;
		public event ServerBotDelegate UnRequestFromBot;

		#endregion

		#region PARSING

		public void ParseData(XG.Core.Server aServer, string aData)
		{
			_log.Debug("ParseData(" + aData + ")");

			if (aData.StartsWith(":"))
			{
				int tSplit = aData.IndexOf(':', 1);
				if (tSplit != -1)
				{
					string[] tCommandList = aData.Split(':')[1].Split(' ');
					// there is an evil : in the hostname - dont know if this matches the rfc2812
					if(tCommandList.Length < 3)
					{
						tSplit = aData.IndexOf(':', tSplit + 1);
						tCommandList = aData.Substring(1).Split(' ');
					}

					string tData = Regex.Replace(aData.Substring(tSplit + 1), "(\u0001|\u0002)", "");

					string tUserName = tCommandList[0].Split('!')[0];
					string tComCodeStr = tCommandList[1];
					string tChannelName = tCommandList[2];

					Channel tChan = aServer[tChannelName];
					Bot tBot = aServer.BotByName(tUserName);

					if(tBot != null)
					{
						tBot.LastContact = DateTime.Now;
					}

					#region PRIVMSG

					if (tComCodeStr == "PRIVMSG")
					{
						HandelDataPrivateMessage(aServer, tData, tCommandList);
						return;
					}

					#endregion

					#region NOTICE

					else if (tComCodeStr == "NOTICE")
					{
						HandleDataNotice(aServer, aData, tData, tCommandList);
						return;
					}

					#endregion

					#region NICK

					else if (tComCodeStr == "NICK")
					{
						if (tBot != null)
						{
							tBot.Name = tData;
							_log.Info("con_DataReceived() bot " + tUserName + " renamed to " + tBot.Name);
						}
						else if(tUserName == Settings.Instance.IRCName)
						{
							// what should i do now?!
							_log.Error("con_DataReceived() wtf? i was renamed to " + tData);
						}
					}

					#endregion

					#region KICK

					else if (tComCodeStr == "KICK")
					{
						if (tChan != null)
						{
							tUserName = tCommandList[3];
							if (tUserName == Settings.Instance.IRCName)
							{
								tChan.Connected = false;
								_log.Warn("con_DataReceived() kicked from " + tChan.Name + (tCommandList.Length >= 5 ? " (" + tCommandList[4] + ")" : "") + " - rejoining");
								_log.Warn("con_DataReceived() " + aData);
								JoinChannel(aServer, tChan);

								// statistics
								Statistic.Instance.Increase(StatisticType.ChannelsKicked);
							}
							else
							{
								tBot = aServer.BotByName(tUserName);
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
						tUserName = tCommandList[2];
						if (tUserName == Settings.Instance.IRCName)
						{
							_log.Warn("con_DataReceived() i was killed from server because of " + tData);
						}
						else
						{
							tBot = aServer.BotByName(tUserName);
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
						tChannelName = tData;
						tChan = aServer[tChannelName];
						if (tChan != null)
						{
							if (tBot != null)
							{
								tBot.Connected = true;
								tBot.LastMessage = "joined channel " + tChan.Name;
								if (tBot.BotState != BotState.Active)
								{
									tBot.BotState = BotState.Idle;
								}
								_log.Info("con_DataReceived() bot " + tUserName + " is online");
								RequestFromBot(aServer, tBot);
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
						_log.Info("con_DataReceived() received an invite for channel " + tData);

						// ok, lets do a silent auto join
						if (Settings.Instance.AutoJoinOnInvite)
						{
							_log.Info("con_DataReceived() auto joining " + tData);
							SendData(aServer, "JOIN " + tData);
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
						int t_ComCode = 0;
						if (int.TryParse(tComCodeStr, out t_ComCode))
						{
							HandleDataIntValues(aServer, aData, tData, t_ComCode, tCommandList);
							return;
						}
						else
						{
							_log.Error("con_DataReceived() Irc code " + tComCodeStr + " could not be parsed. (" + aData + ")");
						}
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
			}

			#region PING

			else if (aData.StartsWith("PING"))
			{
				_log.Info("con_DataReceived() PING");
				SendData(aServer, "PONG " + aData.Split(':')[1]);
			}

			#endregion

			#region ERROR

			else if (aData.StartsWith("ERROR"))
			{
				_log.Error("con_DataReceived() ERROR: " + aData);
			}

			#endregion
		}

		void HandleDataIntValues(XG.Core.Server aServer, string aData, string tData, int t_ComCode, string[] tCommandList)
		{
			Channel tChan = aServer[tCommandList[2]];
			Bot tBot = aServer.BotByName(tCommandList[0].Split('!')[0]);

			switch (t_ComCode)
			{
				#region 4

				case 4: // 
					aServer.Connected = true;
					aServer.Commit();
					break;

				#endregion

				#region RPL_WHOISCHANNELS

				case 319: // RPL_WHOISCHANNELS
					tBot = aServer.BotByName(tCommandList[3]);
					if (tBot != null)
					{
						string chanName = "";
						bool addChan = true;
						string[] tChannelList = aData.Split(':')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string chan in tChannelList)
						{
							chanName = "#" + chan.Split('#')[1];
							if (aServer[chanName] != null)
							{
								addChan = false;
								RequestFromBot(aServer, tBot);
								break;
							}
						}
						if (addChan)
						{
							_log.Info("con_DataReceived() auto adding channel " + chanName);
							aServer.AddChannel(chanName);
						}
					}
					break;

				#endregion

				#region RPL_NAMREPLY

				case 353: // RPL_NAMREPLY
					tChan = aServer[tCommandList[4]];
					if (tChan != null)
					{
						string[] tUsers = tData.Split(' ');
						foreach (string user in tUsers)
						{
							string tUser = Regex.Replace(user, "^(@|!|%|\\+){1}", "");
							tBot = tChan[tUser];
							if (tBot != null)
							{
								tBot.Connected = true;
								tBot.LastMessage = "joined channel " + tChan.Name;
								if (tBot.BotState != BotState.Active)
								{
									tBot.BotState = BotState.Idle;
								}
								_log.Info("con_DataReceived() bot " + tBot.Name + " is online");
								tBot.Commit();
								RequestFromBot(aServer, tBot);
							}
						}
					}
					break;

				#endregion

				#region RPL_ENDOFNAMES

				case 366: // RPL_ENDOFNAMES
					tChan = aServer[tCommandList[3]];
					if (tChan != null)
					{
						tChan.ErrorCode = 0;
						tChan.Connected = true;
						_log.Info("con_DataReceived() joined channel " + tChan.Name);
					}

					// statistics
					Statistic.Instance.Increase(StatisticType.ChannelConnectsOk);
					break;

				#endregion

				#region RPL_ENDOFMOTD | ERR_NOMOTD

				case 376: // RPL_ENDOFMOTD
				case 422: // ERR_NOMOTD
					_log.Info("con_DataReceived() really connected");
					aServer.Connected = true;
					aServer.Commit();
					foreach (Channel chan in aServer.Channels)
					{
						if (chan.Enabled) { JoinChannel(aServer, chan); }
					}
					if(Settings.Instance.IrcRegisterPasswort != "")
					{
						SendData(aServer, "nickserv identify " + Settings.Instance.IrcRegisterPasswort);
					}

					// statistics
					Statistic.Instance.Increase(StatisticType.ServerConnectsOk);
					break;

				#endregion

				#region ERR_NOCHANMODES

				case 477: // ERR_NOCHANMODES
					tChan = aServer[tCommandList[3]];
					// TODO should we nickserv register here?
					/*if(Settings.Instance.AutoRegisterNickserv && Settings.Instance.IrcRegisterPasswort != "" && Settings.Instance.IrcRegisterEmail != "")
					{
						SendDataEvent(aServer, "nickserv register " + Settings.Instance.IrcRegisterPasswort + " " + Settings.Instance.IrcRegisterEmail);
					}*/

					if(tChan != null)
					{
						tChan.ErrorCode = t_ComCode;
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
					tChan = aServer[tCommandList[3]];
					if (tChan != null)
					{
						tChan.ErrorCode = t_ComCode;
						tChan.Connected = false;
						_log.Warn("con_DataReceived() could not join channel " + tChan.Name + ": " + t_ComCode);
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
					tChan = aServer[tCommandList[3]];
					if (tChan != null)
					{
						tChan.ErrorCode = t_ComCode;
						tChan.Connected = false;
						_log.Warn("con_DataReceived() could not join channel " + tChan.Name + ": " + t_ComCode);
						CreateTimer(aServer, tChan, t_ComCode == 471 || t_ComCode == 485 ? Settings.Instance.ChannelWaitTime : Settings.Instance.ChannelWaitTimeLong, false);
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

		void HandelDataPrivateMessage(XG.Core.Server aServer, string tData, string[] tCommandList)
		{
			string tUserName = tCommandList[0].Split('!')[0];
			Channel tChan = aServer[tCommandList[2]];
			Bot tBot = aServer.BotByName(tUserName);

			#region VERSION

			if (tData == "VERSION")
			{
				_log.Info("con_DataReceived() VERSION: " + Settings.Instance.IrcVersion);
				SendData(aServer, "NOTICE " + tUserName + " :\u0001VERSION " + Settings.Instance.IrcVersion + "\u0001");
				return;
			}

			#endregion

			#region XGVERSION

			else if (tData == "XGVERSION")
			{
				_log.Info("con_DataReceived() XGVERSION: " + Settings.Instance.XgVersion);
				SendData(aServer, "NOTICE " + tUserName + " :\u0001XGVERSION " + Settings.Instance.XgVersion + "\u0001");
				return;
			}

			#endregion

			#region DCC DOWNLOAD MESSAGE

			else if (tData.StartsWith("DCC") && tBot != null)
			{
				Packet tPacket = tBot.OldestActivePacket();
				if (tPacket != null)
				{
					bool isOk = false;

					int tPort = 0;
					Int64 tChunk = 0;

					string[] tDataList = tData.Split(' ');
					if (tDataList[1] == "SEND")
					{
						_log.Info("con_DataReceived() DCC from " + tBot.Name);

						// if the name of the file contains spaces, we have to replace em
						if(tData.StartsWith("DCC SEND \""))
						{
							Match tMatch = Regex.Match(tData, "DCC SEND \"(?<packet_name>.+)\"(?<bot_data>[^\"]+)$");
							if (tMatch.Success)
							{
								tData = "DCC SEND " + tMatch.Groups["packet_name"].ToString().Replace(" ", "_").Replace("'", "") + tMatch.Groups["bot_data"];
								tDataList = tData.Split(' ');
							}
						}

						#region IP CALCULATING
						try
						{
							// this works not in mono?!
							tBot.IP = IPAddress.Parse(tDataList[3]);
						}
						catch (FormatException)
						{
							#region WTF - FLIP THE IP BECAUSE ITS REVERSED?!
							string ip = "";
							try { ip = new IPAddress(long.Parse(tDataList[3])).ToString(); }
							catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse bot ip from string: " + tData, ex); return; }
							int pos = 0;
							string realIp = "";
							pos = ip.LastIndexOf('.');
							try
							{
								realIp += ip.Substring(pos + 1) + ".";
								ip = ip.Substring(0, pos);
								pos = ip.LastIndexOf('.');
								realIp += ip.Substring(pos + 1) + ".";
								ip = ip.Substring(0, pos);
								pos = ip.LastIndexOf('.');
								realIp += ip.Substring(pos + 1) + ".";
								ip = ip.Substring(0, pos);
								pos = ip.LastIndexOf('.');
								realIp += ip.Substring(pos + 1);
							}
							catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse bot ip '" + ip + "' from string: " + tData, ex); return; }

							_log.Info("con_DataReceived() IP parsing failed, using this: " + realIp);
							try { tBot.IP = IPAddress.Parse(realIp); }
							catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse bot ip from string: " + tData, ex); return; }
							#endregion
						}
						#endregion
						try { tPort = int.Parse(tDataList[4]); }
						catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse bot port from string: " + tData, ex); return; }
						// we cant connect to port <= 0
						if(tPort <= 0)
						{
							_log.Error("con_DataReceived() " + tBot.Name + " submitted wrong port: " + tPort + ", disabling packet");
							tPacket.Enabled = false;
							tPacket.Commit();

							// statistics
							Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
						}
						else
						{
							tPacket.RealName = tDataList[2];
							try { tPacket.RealSize = Int64.Parse(tDataList[5]); }
							catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse packet size from string: " + tData, ex); return; }

							tChunk = FileActions.NextAvailablePartSize(tPacket.RealName, tPacket.RealSize);
							if (tChunk < 0)
							{
								_log.Error("con_DataReceived() file from " + tBot.Name + " already in use");
								tPacket.Enabled = false;
								tPacket.Commit();
								UnRequestFromBot(aServer, tBot);
							}
							else if (tChunk > 0)
							{
								_log.Info("con_DataReceived() try resume from " + tBot.Name + " for " + tPacket.RealName + " @ " + tChunk);
								SendData(aServer, "PRIVMSG " + tBot.Name + " :\u0001DCC RESUME " + tPacket.RealName + " " + tPort + " " + tChunk + "\u0001");
							}
							else { isOk = true; }
						}
					}
					else if (tDataList[1] == "ACCEPT")
					{
						_log.Info("con_DataReceived() DCC resume accepted from " + tBot.Name);
						try { tPort = int.Parse(tDataList[3]); 	}
						catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse bot port from string: " + tData, ex); return; }
						try { tChunk = Int64.Parse(tDataList[4]); }
						catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse packet chunk from string: " + tData, ex); return; }
						isOk = true;
					}

					if (isOk)
					{
						_log.Info("con_DataReceived() downloading from " + tBot.Name + " - Starting: " + tChunk + " - Size: " + tPacket.RealSize);
						AddDownload(tPacket, tChunk, tBot.IP, tPort);
					}
				}
				else { _log.Error("con_DataReceived() DCC not activated from " + tBot.Name); }
			}

			#endregion

			#region DCC INFO MESSAGE

			else if (tChan != null)
			{
				bool insertBot = false;
				if (tBot == null)
				{
					insertBot = true;
					tBot = new Bot();
					tBot.Name = tUserName;
					tBot.Connected = true;
					tBot.LastMessage = "initial creation";
					tBot.LastContact = DateTime.Now;
				}

				bool isParsed = false;
				Match tMatch = null;
				int valueInt = 0;
				double valueDouble = 0;

				#region PACKET /SLOT / QUEUE INFO

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, MAGICSTRING + " ([0-9]*) (pack(s|)|Pa(c|)ket(e|)|Fil[e]+s) " + MAGICSTRING + "\\s*(?<slot_cur>[0-9]*) (of|von) (?<slot_total>[0-9]*) (slot(s|)|Pl(a|�|.)tz(e|)) (open|opened|free|frei|in use|offen)(, ((Queue|Warteschlange): (?<queue_cur>[0-9]*)(\\/| of )(?<queue_total>[0-9]*),|).*(Record( [a-zA-Z]+|): (?<record>[0-9.]*)(K|)B\\/s|)|)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;

						if (int.TryParse(tMatch.Groups["slot_cur"].ToString(), out valueInt)) { tBot.InfoSlotCurrent = valueInt; }
						if (int.TryParse(tMatch.Groups["slot_total"].ToString(), out valueInt)) { tBot.InfoSlotTotal = valueInt; }
						if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt)) { tBot.InfoQueueCurrent = valueInt; }
						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }
						// this is not the all over record speed!
						//if (double.TryParse(tMatch.Groups["record"].ToString(), out valueDouble)) { tBot.InfoSpeedMax = valueDouble; }

						if(tBot.InfoSlotCurrent > tBot.InfoSlotTotal)
						{
							tBot.InfoSlotTotal = tBot.InfoSlotCurrent;
						}
						if(tBot.InfoQueueCurrent > tBot.InfoQueueTotal)
						{
							tBot.InfoQueueTotal = tBot.InfoQueueCurrent;
						}

						// uhm, there is a free slot and we are still waiting?
						if (tBot.InfoSlotCurrent > 0 && tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
							CreateTimer(aServer, tBot, 0, false);
						}
					}
				}

				#endregion

				#region BANDWIDTH

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, MAGICSTRING + " ((Bandwidth Usage|Bandbreite) " + MAGICSTRING + "|)\\s*(Current|Derzeit): (?<speed_cur>[0-9.]*)(?<speed_cur_end>(K|)(i|)B)(\\/s|s)(,|)(.*Record: (?<speed_max>[0-9.]*)(?<speed_max_end>(K|)(i|))B(\\/s|s)|)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;

						string speed_cur_end = tMatch.Groups["speed_cur_end"].ToString().ToLower();
						string speed_max_end = tMatch.Groups["speed_max_end"].ToString().ToLower();
						string speed_cur = tMatch.Groups["speed_cur"].ToString();
						string speed_max = tMatch.Groups["speed_max"].ToString();
						if(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
						{
							speed_cur = speed_cur.Replace('.', ',');
							speed_max = speed_max.Replace('.', ',');
						}
						if (double.TryParse(speed_cur, out valueDouble)) { tBot.InfoSpeedCurrent = speed_cur_end.StartsWith("k") ? valueDouble * 1024 : valueDouble; }
						if (double.TryParse(speed_max, out valueDouble)) { tBot.InfoSpeedMax = speed_max_end.StartsWith("k") ? valueDouble * 1024 : valueDouble; }

//						if(tBot.InfoSpeedCurrent > tBot.InfoSpeedMax)
//						{
//							tBot.InfoSpeedMax = tBot.InfoSpeedCurrent;
//						}
					}
				}

				#endregion

				#region PACKET INFO

				Packet newPacket = null;
				if (!isParsed)
				{ // what is this damn char \240 and how to rip it off ???
					tMatch = Regex.Match(tData, "#(?<pack_id>\\d+)(\u0240|�|)\\s+(\\d*)x\\s+\\[\\s*(�|)\\s*(?<pack_size>[\\<\\>\\d.]+)(?<pack_add>[BbGgiKMs]+)\\]\\s+(?<pack_name>.*)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;

						try
						{
							int tPacketId = -1;
							try { tPacketId = int.Parse(tMatch.Groups["pack_id"].ToString()); }
							catch (Exception ex) { _log.Fatal("con_DataReceived() " + tBot.Name + " - can not parse packet id from string: " + tData, ex); return; }

							Packet tPack = tBot[tPacketId];
							if (tPack == null)
							{
								tPack = new Packet();
								newPacket = tPack;
								tPack.Id = tPacketId;
								tBot.AddPacket(tPack);
							}
							tPack.LastMentioned = DateTime.Now;

							string name = ClearPacketName(tMatch.Groups["pack_name"].ToString());
							if (tPack.Name != name && tPack.Name != "")
							{
								//myLog.Warn(this, "The Packet " + tPack.Id + "(" + tPacketId + ") name changed from '" + tPack.Name + "' to '" + name + "' maybee they changed the content");
								tPack.Enabled = false;
								if (!tPack.Connected)
								{
									tPack.RealName = "";
									tPack.RealSize = 0;
								}
							}
							tPack.Name = name;

							double tPacketSizeFormated = 0;
							string stringSize = tMatch.Groups["pack_size"].ToString().Replace("<", "").Replace(">", "");
							if(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
							{
								stringSize = stringSize.Replace('.', ',');
							}
							double.TryParse(stringSize, out tPacketSizeFormated);

							string tPacketAdd = tMatch.Groups["pack_add"].ToString().ToLower();

							if (tPacketAdd == "k" || tPacketAdd == "kb") { tPack.Size = (Int64)(tPacketSizeFormated * 1024); }
							else if (tPacketAdd == "m" || tPacketAdd == "mb") { tPack.Size = (Int64)(tPacketSizeFormated * 1024 * 1024); }
							else if (tPacketAdd == "g" || tPacketAdd == "gb") { tPack.Size = (Int64)(tPacketSizeFormated * 1024 * 1024 * 1024); }

							tPack.Commit();
							_log.Info("con_DataReceived() updated packet #" + tPack.Id + " from " + tBot.Name);
						}
						catch (FormatException) { }
					}
				}

				#endregion

				// insert bot if ok
				if (insertBot)
				{
					if (isParsed)
					{
						tChan.AddBot(tBot);
						_log.Info("con_DataReceived() inserted bot " + tBot.Name);
					}
				}
				// and insert packet _AFTER_ this
				if (newPacket != null)
				{
					tBot.AddPacket(newPacket);
					_log.Info("con_DataReceived() inserted packet #" + newPacket.Id + " into " + tBot.Name);
				}

#if DEBUG
				#region NOT NEEDED INFOS

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, MAGICSTRING + " To request .* type .*", RegexOptions.IgnoreCase);
					if (tMatch.Success) { return; }
					tMatch = Regex.Match(tData, ".*\\/(msg|ctcp) .* xdcc (info|send) .*", RegexOptions.IgnoreCase);
					if (tMatch.Success) { return; }
					tMatch = Regex.Match(tData, MAGICSTRING + " To list a group, type .*", RegexOptions.IgnoreCase);
					if (tMatch.Success) { return; }
					tMatch = Regex.Match(tData, "Total offered(\\!|): (\\[|)[0-9.]*\\s*[BeGgiKMsTty]+(\\]|)\\s*Total transfer(r|)ed: (\\[|)[0-9.]*\\s*[BeGgiKMsTty]+(\\]|)", RegexOptions.IgnoreCase);
					if (tMatch.Success) { return; }
					tMatch = Regex.Match(tData, ".* (brought to you|powered|sp(o|0)ns(o|0)red) by .*", RegexOptions.IgnoreCase);
					if (tMatch.Success) { return; }
					tMatch = Regex.Match(tData, MAGICSTRING + " .*" + tChan.Name + " " + MAGICSTRING , RegexOptions.IgnoreCase);
					if (tMatch.Success) { return; }
				}

				#endregion

				#region COULD NOT PARSE

				if (!isParsed)// && tBot.Packets.Count() > 0)
				{
					ParsingError("[DCC Info] " + tBot.Name + " : " + ClearString(tData));
				}

				#endregion
#endif
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

		void HandleDataNotice(XG.Core.Server aServer, string aData, string tData, string[] tCommandList)
		{
			string tUserName = tCommandList[0].Split('!')[0];
			Bot tBot = aServer.BotByName(tUserName);

			#region BOT MESSAGES

			if (tBot != null)
			{
				bool isParsed = false;
				Match tMatch = null;
				Match tMatch1 = null;
				Match tMatch2 = null;
				Match tMatch3 = null;
				Match tMatch4 = null;
				Match tMatch5 = null;

				int valueInt = 0;
				tData = ClearString(tData);
				//double valueDouble = 0;

				#region ALL SLOTS FULL / ADDING TO QUEUE

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, "(" + MAGICSTRING + " All Slots Full, |)Added you to the main queue (for pack ([0-9]+) \\(\".*\"\\) |).*in positi(o|0)n (?<queue_cur>[0-9]+)\\. To Remove you(r|)self at a later time .*", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, "Queueing you for pack [0-9]+ \\(.*\\) in slot (?<queue_cur>[0-9]+)/(?<queue_total>[0-9]+)\\. To remove you(r|)self from the queue, type: .*\\. To check your position in the queue, type: .*\\. Estimated time remaining in queue: (?<queue_d>[0-9]+) days, (?<queue_h>[0-9]+) hours, (?<queue_m>[0-9]+) minutes", RegexOptions.IgnoreCase);
					tMatch3 = Regex.Match(tData, "(" + MAGICSTRING + " |)Es laufen bereits genug .bertragungen, Du bist jetzt in der Warteschlange f.r Datei [0-9]+ \\(.*\\) in Position (?<queue_cur>[0-9]+)\\. Wenn Du sp.ter Abbrechen willst schreibe .*", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success || tMatch3.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						tMatch = tMatch.Success ? tMatch : tMatch3;
						isParsed = true;
						if (tBot.BotState == BotState.Idle)
						{
							tBot.BotState = BotState.Waiting;
						}

						tBot.InfoSlotCurrent = 0;
						if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt))
						{
							tBot.QueuePosition = valueInt;
							tBot.InfoQueueCurrent = tBot.QueuePosition;
						}

						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }
						else if(tBot.InfoQueueTotal < tBot.InfoQueueCurrent) { tBot.InfoQueueTotal = tBot.InfoQueueCurrent; }

						int time = 0;
						if (int.TryParse(tMatch.Groups["queue_m"].ToString(), out valueInt)) { time += valueInt * 60; }
						if (int.TryParse(tMatch.Groups["queue_h"].ToString(), out valueInt)) { time += valueInt * 60 * 60; }
						if (int.TryParse(tMatch.Groups["queue_d"].ToString(), out valueInt)) { time += valueInt * 60 * 60 * 24; }
						tBot.QueueTime = time;
					}
				}

				#endregion

				#region REMOVE FROM QUEUE

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, MAGICSTRING + " Removed From Queue: .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
						}
						CreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);
					}
				}

				#endregion

				#region INVALID PACKET NUMBER

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, MAGICSTRING + " Die Nummer der Datei ist ung.ltig", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, MAGICSTRING + " Invalid Pack Number, Try Again", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						isParsed = true;
						Packet tPack = tBot.OldestActivePacket();
						if (tPack != null)
						{
							tPack.Enabled = false;
							tBot.RemovePacket(tPack);
						}
						_log.Error("con_DataReceived() invalid packetnumber from " + tBot.Name);
					}
				}

				#endregion

				#region PACK ALREADY REQUESTED

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, MAGICSTRING + " You already requested that pack(.*|)", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, MAGICSTRING + " Du hast diese Datei bereits angefordert(.*|)", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Idle)
						{
							tBot.BotState = BotState.Waiting;
						}
					}
				}

				#endregion

				#region ALREADY QUEUED / RECEIVING

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, "Denied, You already have ([0-9]+) item(s|) queued, Try Again Later", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, MAGICSTRING + " All Slots Full, Denied, You already have that item queued\\.", RegexOptions.IgnoreCase);
					tMatch3 = Regex.Match(tData, "You are already receiving or are queued for the maximum number of packs .*", RegexOptions.IgnoreCase);
					tMatch4 = Regex.Match(tData, "Du hast max\\. ([0-9]+) transfer auf einmal, Du bist jetzt in der Warteschlange f.r Datei .*", RegexOptions.IgnoreCase);
					tMatch5 = Regex.Match(tData, "Es laufen bereits genug .bertragungen, abgewiesen, Du hast diese Datei bereits in der Warteschlange\\.", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success || tMatch3.Success || tMatch4.Success || tMatch5.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Idle)
						{
							tBot.BotState = BotState.Waiting;
						}
						else if(tBot.BotState == BotState.Waiting)
						{
							// if there is no active packets lets remove us from the queue
							if(tBot.OldestActivePacket() == null) { UnRequestFromBot(aServer, tBot); }
						}
					}
				}

				#endregion

				#region DCC PENDING

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, MAGICSTRING + " You have a DCC pending, Set your client to receive the transfer\\. ((Type .*|Send XDCC CANCEL) to abort the transfer\\. |)\\((?<time>[0-9]+) seconds remaining until timeout\\)", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, MAGICSTRING + " Du hast eine .bertragung schwebend, Du mu.t den Download jetzt annehmen\\. ((Schreibe .*|Sende XDCC CANCEL)            an den Bot um die .bertragung abzubrechen\\. |)\\((?<time>[0-9]+) Sekunden bis zum Abbruch\\)", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						isParsed = true;
						if (int.TryParse(tMatch.Groups["time"].ToString(), out valueInt))
						{
							if (valueInt == 30 && tBot.BotState != BotState.Active)
							{
								tBot.BotState = BotState.Idle;
							}
							CreateTimer(aServer, tBot, (valueInt + 2) * 1000, false);
						}
					}
				}

				#endregion

				#region ALL SLOTS AND QUEUE FULL

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, MAGICSTRING + " All Slots Full, Main queue of size (?<queue_total>[0-9]+) is Full, Try Again Later", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, MAGICSTRING + " Es laufen bereits genug .bertragungen, abgewiesen, die Warteschlange ist voll, max\\. (?<queue_total>[0-9]+) Dateien, Versuche es sp.ter nochmal", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						isParsed = true;
						if (tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
						}
						tBot.InfoSlotCurrent = 0;
						tBot.InfoQueueCurrent = 0;
						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }

						CreateTimer(aServer, tBot, Settings.Instance.BotWaitTime, false);
					}
				}

				#endregion

				#region TRANSFER LIMIT

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, MAGICSTRING + " You can only have ([0-9]+) transfer(s|) at a time,.*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Idle)
						{
							tBot.BotState = BotState.Waiting;
						}
					}
				}

				#endregion

				#region OWNER REQUEST

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, MAGICSTRING + " The Owner Has Requested That No New Connections Are Made In The Next (?<time>[0-9]+) Minute(s|)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
						}

						if (int.TryParse(tMatch.Groups["time"].ToString(), out valueInt))
						{
							CreateTimer(aServer, tBot, (valueInt * 60 + 1) * 1000, false);
						}
					}
				}

				#endregion

				#region XDCC DOWN

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, "The XDCC is down, try again later.*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
						}
						CreateTimer(aServer, tBot, Settings.Instance.BotWaitTime, false);
					}
				}

				#endregion

				#region XDCC DENIED

				if (!isParsed)
				{
					tMatch = Regex.Match(tData,MAGICSTRING + " XDCC SEND denied, (?<info>.*)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						string info = tMatch.Groups["info"].ToString().ToLower();
						if (info.StartsWith("you must be on a known channel to request a pack"))
						{
							SendData(aServer, "WHOIS " + tBot.Name);
						}
						else if (info.StartsWith("i don't send transfers to"))
						{
							foreach (Packet tPacket in tBot.Packets)
							{
								if (tPacket.Enabled)
								{
									tPacket.Enabled = false;
									tPacket.Commit();
								}
							}
						}
						else
						{
							if (tBot.BotState == BotState.Waiting)
							{
								tBot.BotState = BotState.Idle;
							}
							CreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);
							_log.Error("con_DataReceived() XDCC denied from " + tBot.Name + ": " + info);
						}
					}
				}

				#endregion

				#region XDCC SENDING

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, MAGICSTRING + " Sending You (Your Queued |)Pack .*", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, MAGICSTRING + " Sende dir jetzt die Datei .*", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
						}
					}
				}

				#endregion

				#region QUEUED

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, "Queued ([0-9]+)h([0-9]+)m for .*, in position (?<queue_cur>[0-9]+) of (?<queue_total>[0-9]+). (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m or .* remaining\\.", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, "In der Warteschlange seit  ([0-9]+)h([0-9]+)m f.r .*, in Position (?<queue_cur>[0-9]+) von (?<queue_total>[0-9]+). Ungef.hr (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m oder .*", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						isParsed = true;
						if (tBot.BotState == BotState.Idle)
						{
							tBot.BotState = BotState.Waiting;
						}
						
						tBot.InfoSlotCurrent = 0;
						if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt)) { tBot.QueuePosition = valueInt; }
						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }
						else if(tBot.InfoQueueTotal < tBot.QueuePosition) { tBot.InfoQueueTotal = tBot.QueuePosition; }

						int time = 0;
						if (int.TryParse(tMatch.Groups["queue_m"].ToString(), out valueInt)) { time += valueInt * 60; }
						if (int.TryParse(tMatch.Groups["queue_h"].ToString(), out valueInt)) { time += valueInt * 60 * 60; }
						tBot.QueueTime = time;
					}
				}

				#endregion

				#region CLOSING CONNECTION

				if (!isParsed)
				{
					// Closing Connection You Must JOIN MG-CHAT As Well To Download - Your Download Will Be Canceled Now
					tMatch1 = Regex.Match(tData, MAGICSTRING + " (Closing Connection:|Transfer Completed)(?<reason>.*)", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, MAGICSTRING + " (Schlie.e Verbindung:)(?<reason>.*)", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						isParsed = true;
						if (tBot.BotState != BotState.Active)
						{
							tBot.BotState = BotState.Idle;
						}
						else
						{
							// kill that connection if the bot sends a close message , but our real bot 
							// connection is still alive and hangs for some crapy reason - maybe because 
							// some admins do some network fu to stop my downloads (happend to me)
							RemoveDownload(tBot);
						}
						CreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);

						tMatch3 = Regex.Match(tData, ".*\\s+JOIN (?<channel>[^\\s]+)\\s+.*", RegexOptions.IgnoreCase);
						if(tMatch3.Success && Settings.Instance.AutoJoinOnInvite)
						{
							// ok, lets do a silent auto join
							_log.Info("con_DataReceived() auto joining " + tMatch3.Groups["channel"].ToString());
							SendData(aServer, "JOIN " + tMatch3.Groups["channel"].ToString());
						}
					}
				}

				#endregion

				#region YOU ARE NOT IN QUEUE

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, "(You Don't Appear To Be In A Queue|Removed you from the queue for.*)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
						}
						tBot.QueuePosition = 0;
						CreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);
					}
				}

				#endregion

				#region PUNISH / AUTO IGNORE

				if (!isParsed)
				{
					tMatch1 = Regex.Match(tData, "Punish-ignore activated for .* \\(.*\\) (?<time_m>[0-9]*) minutes", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(tData, "Auto-ignore activated for .* lasting (?<time_m>[0-9]*)m(?<time_s>[0-9]*)s\\. Further messages will increase duration\\.", RegexOptions.IgnoreCase);
					tMatch3 = Regex.Match(tData, "Zur Strafe wirst du .* \\(.*\\) f.r (?<time_m>[0-9]*) Minuten ignoriert(.|)", RegexOptions.IgnoreCase);
					tMatch4 = Regex.Match(tData, "Auto-ignore activated for .* \\(.*\\)", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success || tMatch3.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2.Success ? tMatch2 : tMatch3;
						isParsed = true;
						if (tBot.BotState == BotState.Waiting)
						{
							tBot.BotState = BotState.Idle;
						}

						if (int.TryParse(tMatch.Groups["time_m"].ToString(), out valueInt))
						{
							int time = valueInt * 60 + 1;
							if (int.TryParse(tMatch.Groups["time_s"].ToString(), out valueInt))
							{
								time += valueInt;
							}
							CreateTimer(aServer, tBot, time * 1000, true);
						}
					}
				}

				#endregion

				#region NOT NEEDED INFOS

				if (!isParsed)
				{
					tMatch = Regex.Match(tData, ".* bandwidth limit .*", RegexOptions.IgnoreCase);
					if (tMatch.Success) { isParsed = true; }
				}

				#endregion

				if (!isParsed)
				{
					ParsingError("[DCC Notice] " + tBot.Name + " : " + tData);
				}
				else
				{
					tBot.LastMessage = tData;
					_log.Info("con_DataReceived() message from " + tBot.Name + ": " + tData);
				}
			}

			#endregion

			#region NICKSERV

			else if(tUserName.ToLower() == "nickserv")
			{
				if(tData.Contains("Password incorrect"))
				{
					_log.Error("con_DataReceived(" + aData + ") - nickserv password wrong");
				}
				else if(tData.Contains("The given email address has reached it's usage limit of 1 user") ||
						tData.Contains("This nick is being held for a registered user"))
				{
					_log.Error("con_DataReceived(" + aData + ") - nickserv nick or email already used");
				}
				else if(tData.Contains("Your nick isn't registered"))
				{
					_log.Warn("con_DataReceived(" + aData + ") - nickserv registering nick");
					if(Settings.Instance.AutoRegisterNickserv && Settings.Instance.IrcRegisterPasswort != "" && Settings.Instance.IrcRegisterEmail != "")
					{
						SendData(aServer, "nickserv register " + Settings.Instance.IrcRegisterPasswort + " " + Settings.Instance.IrcRegisterEmail);
					}
				}
				else if(tData.Contains("Nickname is already in use") ||
						tData.Contains("Nickname is currently in use"))
				{
					SendData(aServer, "nickserv ghost " + Settings.Instance.IRCName + " " + Settings.Instance.IrcRegisterPasswort);
					SendData(aServer, "nickserv recover " + Settings.Instance.IRCName + " " + Settings.Instance.IrcRegisterPasswort);
					SendData(aServer, "nick " + Settings.Instance.IRCName);
				}
				else if(tData.Contains("Services Enforcer"))
				{
					SendData(aServer, "nickserv recover " + Settings.Instance.IRCName + " " + Settings.Instance.IrcRegisterPasswort);
					SendData(aServer, "nickserv release " + Settings.Instance.IRCName + " " + Settings.Instance.IrcRegisterPasswort);
					SendData(aServer, "nick " + Settings.Instance.IRCName);
				}
				else if(tData.Contains("This nickname is registered and protected") ||
						tData.Contains("This nick is being held for a registered user") ||
						tData.Contains("msg NickServ IDENTIFY"))
				{
					if(Settings.Instance.IrcRegisterPasswort != "")
					{
						SendData(aServer, "/nickserv identify " + Settings.Instance.IrcRegisterPasswort);
					}
				}
				else if(tData.Contains("You must have been using this nick for at least 30 seconds to register."))
				{
					//TODO sleep the given time and reregister
					SendData(aServer, "nickserv register " + Settings.Instance.IrcRegisterPasswort + " " + Settings.Instance.IrcRegisterEmail);
				}
				else if(tData.Contains("Please try again with a more obscure password"))
				{
					_log.Error("con_DataReceived(" + aData + ") - nickserv password is unsecure");
				}
				else if(tData.Contains("A passcode has been sent to " + Settings.Instance.IrcRegisterEmail) ||
						tData.Contains("This nick is awaiting an e-mail verification code"))
				{
					_log.Error("con_DataReceived(" + aData + ") - nickserv confirm email");
				}
				else if(tData.Contains("Nickname " + Settings.Instance.IRCName + " registered under your account"))
				{
					_log.Info("con_DataReceived(" + aData + ") - nickserv nick registered succesfully");
				}
				else if(tData.Contains("Password accepted"))
				{
					_log.Info("con_DataReceived(" + aData + ") - nickserv password accepted");
				}
				else if(tData.Contains("Please type") && tData.Contains("to complete registration."))
				{
					Match tMatch = Regex.Match(tData, ".* NickServ confirm (?<code>[^\\s]+) .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						SendData(aServer, "/msg NickServ confirm " + tMatch.Groups["code"]);
						_log.Info("con_DataReceived(" + aData + ") - confirming nickserv");
					}
					else
					{
						_log.Error("con_DataReceived(" + aData + ") - cant find nickserv code");
					}
				}
				else
				{
					_log.Error("con_DataReceived(" + aData + ") - unknown nickserv command");
				}
			}

			#endregion
		}

		#endregion

		#region HELPER

		string ClearString(string aData)
		{ // |\u0031|\u0015)
			aData = Regex.Replace(aData, "(\u0002|\u0003)(\\d+(,\\d{1,2}|)|)", "");
			aData = Regex.Replace(aData, "(\u000F)", "");
			return aData.Trim();
		}

		string ClearPacketName(string aData)
		{
			// TODO remove all chars not matching this [a-z0-9.-_()]
			string tData = ClearString(aData);
			tData = tData.Replace("Movies", string.Empty);
			tData = tData.Replace("Charts", string.Empty);
			tData = tData.Replace("[]", string.Empty);
			tData = tData.Replace("\u000F", string.Empty);
			tData = tData.Replace("\uFFFD", string.Empty);
			tData = tData.Replace("\u0016", string.Empty);
			tData = tData.Replace("  ", " ");
			return tData.Trim();
		}

		#endregion
	}
}
