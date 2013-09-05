// 
//  Message.cs
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
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using XG.Core;

using log4net;
using Meebey.SmartIrc4net;

namespace XG.Server.Plugin.Core.Irc.Parser
{
	public delegate void ServerBotIntDelegate(XG.Core.Server aServer, Bot aBot, int aInt);
	public delegate void ServerBotTextDelegate(XG.Core.Server aServer, Bot aBot, string aText);

	public class Message : ANotificationSender
	{
		#region EVENTS

		public event ServerBotIntDelegate OnQueueRequestFromBot;
		public event ServerDataTextDelegate OnJoinChannel;

		#endregion

		#region PARSING

		public void Parse(XG.Core.Server aServer, IrcEventArgs aEvent)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");
			string tMessage = Helper.RemoveSpecialIrcChars(aEvent.Data.Message);

			var tChan = aServer.Channel(aEvent.Data.Channel);
			if (tChan != null)
			{
				string tUserName = aEvent.Data.Nick;
				Bot tBot = aServer.Bot(tUserName);
				Packet newPacket = null;

				bool insertBot = false;
				if (tBot == null)
				{
					insertBot = true;
					tBot = new Bot {Name = tUserName, Connected = true, LastMessage = "initial creation", LastContact = DateTime.Now};
				}

				bool isParsed = false;
				Match tMatch;

				#region PACKET /SLOT / QUEUE INFO

				if (!isParsed)
				{
					tMatch = Regex.Match(tMessage,
					                     Helper.Magicstring + " ([0-9]*) (pack(s|)|Pa(c|)ket(e|)|Fil[e]+s) " + Helper.Magicstring +
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
							OnQueueRequestFromBot(aServer, tBot, 0);
						}
					}
				}

				#endregion

				#region BANDWIDTH

				if (!isParsed)
				{
					tMatch = Regex.Match(tMessage,
					                     Helper.Magicstring + " ((Bandwidth Usage|Bandbreite) " + Helper.Magicstring +
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

				if (!isParsed)
				{
					tMatch = Regex.Match(tMessage,
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
								log.Fatal("Parse() " + tBot + " - can not parse packet id from string: " + tMessage, ex);
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

							string name = RemoveSpecialIrcCharsFromPacketName(tMatch.Groups["pack_name"].ToString());
							if (tPack.Name != name && tPack.Name != "")
							{
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
								log.Info("Parse() updated " + tPack + " from " + tBot);
							}
						}
						catch (FormatException) {}
					}
				}

				#endregion

				#region INFO MESSAGE

				if (!isParsed)
				{
					tMatch = Regex.Match(tMessage, @".*\s+JOIN (?<channel>[^\s]+).*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						string channel = tMatch.Groups["channel"].ToString();
						if (!channel.StartsWith("#"))
						{
							channel = "#" + channel;
						}
						OnJoinChannel(aServer, channel);
					}
				}

				#endregion

				// insert bot if ok
				if (insertBot)
				{
					if (isParsed)
					{
						if (tChan.AddBot(tBot))
						{
							log.Info("Parse() inserted " + tBot);
						}
						else
						{
							var duplicateBot = tChan.Bot(tBot.Name);
							if (duplicateBot != null)
							{
								tBot = duplicateBot;
							}
							else
							{
								log.Error("Parse() cant insert " + tBot + " into " + tChan);
							}
						}
					}
				}
				// and insert packet _AFTER_ this
				if (newPacket != null)
				{
					tBot.AddPacket(newPacket);
					log.Info("Parse() inserted " + newPacket + " into " + tBot);
				}

				tBot.Commit();
				tChan.Commit();
			}
		}

		#endregion

		#region HELPER

		string RemoveSpecialIrcCharsFromPacketName(string aData)
		{
			string tData = Helper.RemoveSpecialIrcChars(aData);
			tData = tData.Replace("  ", " ");
			return tData.RemoveSpecialChars();
		}

		#endregion
	}
}
