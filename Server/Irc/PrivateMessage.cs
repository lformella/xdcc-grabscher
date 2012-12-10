// 
//  PrivateMessage.cs
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
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using XG.Core;
using XG.Server.Helper;

using log4net;

namespace XG.Server.Irc
{
	public class PrivateMessage : AParser
	{
		#region VARIABLES

		public FileActions FileActions { get; set; }

		#endregion

		#region PARSING

		protected override void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");

			string tUserName = aCommands[0].Split('!')[0];
			Channel tChan = aServer.Channel(aCommands[2]);

			Bot tBot = aServer.Bot(tUserName);

			#region VERSION

			if (aMessage == "VERSION")
			{
				log.Info("Parse() VERSION: " + Settings.Instance.IrcVersion);
				FireSendData(aServer, "NOTICE " + tUserName + " :\u0001VERSION " + Settings.Instance.IrcVersion + "\u0001");
				return;
			}

				#endregion

				#region XGVERSION

			if (aMessage == "XGVERSION")
			{
				log.Info("Parse() XGVERSION: " + Settings.Instance.XgVersion);
				FireSendData(aServer, "NOTICE " + tUserName + " :\u0001XGVERSION " + Settings.Instance.XgVersion + "\u0001");
				return;
			}

				#endregion

				#region DCC DOWNLOAD MESSAGE

			if (aMessage.StartsWith("DCC") && tBot != null)
			{
				Packet tPacket = tBot.OldestActivePacket();
				if (tPacket != null)
				{
					bool isOk = false;

					int tPort = 0;
					Int64 tChunk = 0;

					string[] tDataList = aMessage.Split(' ');
					if (tDataList[1] == "SEND")
					{
						log.Info("Parse() DCC from " + tBot.Name);

						// if the name of the file contains spaces, we have to replace em
						if (aMessage.StartsWith("DCC SEND \""))
						{
							Match tMatch = Regex.Match(aMessage, "DCC SEND \"(?<packet_name>.+)\"(?<bot_data>[^\"]+)$");
							if (tMatch.Success)
							{
								aMessage = "DCC SEND " + tMatch.Groups["packet_name"].ToString().Replace(" ", "_").Replace("'", "") + tMatch.Groups["bot_data"];
								tDataList = aMessage.Split(' ');
							}
						}

						#region IP CALCULATING

						try
						{
							// this works not in mono?!
							tBot.Ip = IPAddress.Parse(tDataList[3]);
						}
						catch (FormatException)
						{
							#region WTF - FLIP THE IP BECAUSE ITS REVERSED?!

							string ip;
							try
							{
								ip = new IPAddress(long.Parse(tDataList[3])).ToString();
							}
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot.Name + " - can not parse bot ip from string: " + aMessage, ex);
								return;
							}
							string realIp = "";
							int pos = ip.LastIndexOf('.');
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
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot.Name + " - can not parse bot ip '" + ip + "' from string: " + aMessage, ex);
								return;
							}

							log.Info("Parse() IP parsing failed, using this: " + realIp);
							try
							{
								tBot.Ip = IPAddress.Parse(realIp);
							}
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot.Name + " - can not parse bot ip from string: " + aMessage, ex);
								return;
							}

							#endregion
						}

						#endregion

						try
						{
							tPort = int.Parse(tDataList[4]);
						}
						catch (Exception ex)
						{
							log.Fatal("Parse() " + tBot.Name + " - can not parse bot port from string: " + aMessage, ex);
							return;
						}
						// we cant connect to port <= 0
						if (tPort <= 0)
						{
							log.Error("Parse() " + tBot.Name + " submitted wrong port: " + tPort + ", disabling packet");
							tPacket.Enabled = false;
							tPacket.Commit();

							// statistics
							Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
						}
						else
						{
							tPacket.RealName = tDataList[2];
							try
							{
								tPacket.RealSize = Int64.Parse(tDataList[5]);
							}
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot.Name + " - can not parse packet size from string: " + aMessage, ex);
								return;
							}

							tChunk = FileActions.NextAvailablePartSize(tPacket.RealName, tPacket.RealSize);
							if (tChunk < 0)
							{
								log.Error("Parse() file (" + tPacket.RealName + ") from " + tBot.Name + " already in use, disable packet");
								tPacket.Enabled = false;
								tPacket.Commit();
								FireUnRequestFromBot(aServer, tBot);
							}
							else if (tChunk > 0)
							{
								log.Info("Parse() try resume from " + tBot.Name + " for " + tPacket.RealName + " @ " + tChunk);
								FireSendData(aServer, "PRIVMSG " + tBot.Name + " :\u0001DCC RESUME " + tPacket.RealName + " " + tPort + " " + tChunk + "\u0001");
							}
							else
							{
								isOk = true;
							}
						}
					}
					else if (tDataList[1] == "ACCEPT")
					{
						log.Info("Parse() DCC resume accepted from " + tBot.Name);
						try
						{
							tPort = int.Parse(tDataList[3]);
						}
						catch (Exception ex)
						{
							log.Fatal("Parse() " + tBot.Name + " - can not parse bot port from string: " + aMessage, ex);
							return;
						}
						try
						{
							tChunk = Int64.Parse(tDataList[4]);
						}
						catch (Exception ex)
						{
							log.Fatal("Parse() " + tBot.Name + " - can not parse packet chunk from string: " + aMessage, ex);
							return;
						}
						isOk = true;
					}

					if (isOk)
					{
						log.Info("Parse() downloading from " + tBot.Name + " - Starting: " + tChunk + " - Size: " + tPacket.RealSize);
						FireAddDownload(tPacket, tChunk, tBot.Ip, tPort);
					}

					tPacket.Commit();
				}
				else
				{
					log.Error("Parse() DCC not activated from " + tBot.Name);
				}
			}

				#endregion

				#region DCC INFO MESSAGE

			else if (tChan != null)
			{
				bool insertBot = false;
				if (tBot == null)
				{
					insertBot = true;
					tBot = new Bot {Name = tUserName, Connected = true, LastMessage = "initial creation", LastContact = DateTime.Now};
				}

				bool isParsed = false;
				Match tMatch;

				#region PACKET /SLOT / QUEUE INFO

				if (true)
				{
					tMatch = Regex.Match(aMessage,
					                     Magicstring + " ([0-9]*) (pack(s|)|Pa(c|)ket(e|)|Fil[e]+s) " + Magicstring +
					                     "\\s*(?<slot_cur>[0-9]*) (of|von) (?<slot_total>[0-9]*) (slot(s|)|Pl(a|�|.)tz(e|)) (open|opened|free|frei|in use|offen)(, ((Queue|Warteschlange): (?<queue_cur>[0-9]*)(\\/| of )(?<queue_total>[0-9]*),|).*(Record( [a-zA-Z]+|): (?<record>[0-9.]*)(K|)B\\/s|)|)",
					                     RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;

						int valueInt;
						if (int.TryParse(tMatch.Groups["slot_cur"].ToString(), out valueInt))
						{
							tBot.InfoSlotCurrent = valueInt;
						}
						if (int.TryParse(tMatch.Groups["slot_total"].ToString(), out valueInt))
						{
							tBot.InfoSlotTotal = valueInt;
						}
						if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt))
						{
							tBot.InfoQueueCurrent = valueInt;
						}
						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt))
						{
							tBot.InfoQueueTotal = valueInt;
						}

						if (tBot.InfoSlotCurrent > tBot.InfoSlotTotal)
						{
							tBot.InfoSlotTotal = tBot.InfoSlotCurrent;
						}
						if (tBot.InfoQueueCurrent > tBot.InfoQueueTotal)
						{
							tBot.InfoQueueTotal = tBot.InfoQueueCurrent;
						}

						// uhm, there is a free slot and we are still waiting?
						if (tBot.InfoSlotCurrent > 0 && tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
							FireCreateTimer(aServer, tBot, 0, false);
						}
					}
				}

				#endregion

				#region BANDWIDTH

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage,
					                     Magicstring + " ((Bandwidth Usage|Bandbreite) " + Magicstring +
					                     "|)\\s*(Current|Derzeit): (?<speed_cur>[0-9.]*)(?<speed_cur_end>(K|)(i|)B)(\\/s|s)(,|)(.*Record: (?<speed_max>[0-9.]*)(?<speed_max_end>(K|)(i|))B(\\/s|s)|)",
					                     RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;

						string speedCurEnd = tMatch.Groups["speed_cur_end"].ToString().ToLower();
						string speedMaxEnd = tMatch.Groups["speed_max_end"].ToString().ToLower();
						string speedCur = tMatch.Groups["speed_cur"].ToString();
						string speedMax = tMatch.Groups["speed_max"].ToString();
						if (Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
						{
							speedCur = speedCur.Replace('.', ',');
							speedMax = speedMax.Replace('.', ',');
						}
						double valueDouble;
						if (double.TryParse(speedCur, out valueDouble))
						{
							tBot.InfoSpeedCurrent = speedCurEnd.StartsWith("k") ? (Int64) (valueDouble * 1024) : (Int64) valueDouble;
						}
						if (double.TryParse(speedMax, out valueDouble))
						{
							tBot.InfoSpeedMax = speedMaxEnd.StartsWith("k") ? (Int64) (valueDouble * 1024) : (Int64) valueDouble;
						}
					}
				}

				#endregion

				#region PACKET INFO

				Packet newPacket = null;
				if (!isParsed)
				{
					// what is this damn char \240 and how to rip it off ???
					tMatch = Regex.Match(aMessage,
					                     "#(?<pack_id>\\d+)(\u0240|�|)\\s+(\\d*)x\\s+\\[\\s*(�|)\\s*(?<pack_size>[\\<\\>\\d.]+)(?<pack_add>[BbGgiKMs]+)\\]\\s+(?<pack_name>.*)",
					                     RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;

						try
						{
							int tPacketId;
							try
							{
								tPacketId = int.Parse(tMatch.Groups["pack_id"].ToString());
							}
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot.Name + " - can not parse packet id from string: " + aMessage, ex);
								return;
							}

							Packet tPack = tBot.Packet(tPacketId);
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

							double tPacketSizeFormated;
							string stringSize = tMatch.Groups["pack_size"].ToString().Replace("<", "").Replace(">", "");
							if (Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
							{
								stringSize = stringSize.Replace('.', ',');
							}
							double.TryParse(stringSize, out tPacketSizeFormated);

							string tPacketAdd = tMatch.Groups["pack_add"].ToString().ToLower();

							if (tPacketAdd == "k" || tPacketAdd == "kb")
							{
								tPack.Size = (Int64) (tPacketSizeFormated * 1024);
							}
							else if (tPacketAdd == "m" || tPacketAdd == "mb")
							{
								tPack.Size = (Int64) (tPacketSizeFormated * 1024 * 1024);
							}
							else if (tPacketAdd == "g" || tPacketAdd == "gb")
							{
								tPack.Size = (Int64) (tPacketSizeFormated * 1024 * 1024 * 1024);
							}

							if (tPack.Commit())
							{
								log.Info("Parse() updated packet #" + tPack.Id + " from " + tBot.Name);
							}
						}
						catch (FormatException) {}
					}
				}

				#endregion

				// insert bot if ok
				if (insertBot)
				{
					if (isParsed)
					{
						tChan.AddBot(tBot);
						log.Info("Parse() inserted bot " + tBot.Name);
					}
				}
				// and insert packet _AFTER_ this
				if (newPacket != null)
				{
					tBot.AddPacket(newPacket);
					log.Info("Parse() inserted packet #" + newPacket.Id + " into " + tBot.Name);
				}

#if DEBUG

				#region NOT NEEDED INFOS

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, Magicstring + " To request .* type .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						return;
					}
					tMatch = Regex.Match(aMessage, ".*\\/(msg|ctcp) .* xdcc (info|send) .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						return;
					}
					tMatch = Regex.Match(aMessage, Magicstring + " To list a group, type .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						return;
					}
					tMatch = Regex.Match(aMessage,
					                     "Total offered(\\!|): (\\[|)[0-9.]*\\s*[BeGgiKMsTty]+(\\]|)\\s*Total transfer(r|)ed: (\\[|)[0-9.]*\\s*[BeGgiKMsTty]+(\\]|)",
					                     RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						return;
					}
					tMatch = Regex.Match(aMessage, ".* (brought to you|powered|sp(o|0)ns(o|0)red) by .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						return;
					}
					tMatch = Regex.Match(aMessage, Magicstring + " .*" + tChan.Name + " " + Magicstring, RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						return;
					}
				}

				#endregion

				#region COULD NOT PARSE

				if (!isParsed) // && tBot.Packets.Count() > 0)
				{
					FireParsingError("[DCC Info] " + tBot.Name + " : " + ClearString(aMessage));
				}

				#endregion

#endif
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

		#region HELPER

		string ClearPacketName(string aData)
		{
			string tData = ClearString(aData);
			tData = tData.Replace("Movies", string.Empty);
			tData = tData.Replace("Charts", string.Empty);
			tData = tData.Replace("[]", string.Empty);
			tData = tData.Replace("\u000F", string.Empty);
			tData = tData.Replace("\uFFFD", string.Empty);
			tData = tData.Replace("\u0016", string.Empty);
			tData = tData.Replace("  ", " ");
			return tData.Clear();
		}

		#endregion
	}
}
