// 
//  Ctcp.cs
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

using XG.Core;
using XG.Server.Helper;

using log4net;
using Meebey.SmartIrc4net;

namespace XG.Server.Plugin.Core.Irc.Parser
{
	public delegate void DownloadDelegate(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort);

	public class Ctcp : ANotificationSender
	{
		#region VARIABLES

		public FileActions FileActions { get; set; }

		#endregion

		#region EVENTS
		
		public event DownloadDelegate OnAddDownload;
		public event ServerBotDelegate OnUnRequestFromBot;
		public event ServerBotTextDelegate OnSendPrivateMessage;

		#endregion

		#region PARSING

		public void Parse(XG.Core.Server aServer, CtcpEventArgs aEvent)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");
			string tMessage = Helper.RemoveSpecialIrcChars(aEvent.CtcpParameter);
			
			string tUserName = aEvent.Data.Nick;
			Bot tBot = aServer.Bot(tUserName);

			if (aEvent.CtcpCommand == "DCC" && tBot != null)
			{
				Packet tPacket = tBot.OldestActivePacket();
				if (tPacket != null)
				{
					if (tPacket.Connected)
					{
						log.Error("Parse() ignoring dcc from " + tBot + " because " + tPacket + " is already connected");
					}
					else
					{
						bool isOk = false;

						int tPort = 0;
						Int64 tChunk = 0;

						string[] tDataList = tMessage.Split(' ');
						if (tDataList[0] == "SEND")
						{
							log.Info("Parse() DCC from " + tBot);

							// if the name of the file contains spaces, we have to replace em
							if (tMessage.StartsWith("SEND \""))
							{
								Match tMatch = Regex.Match(tMessage, "SEND \"(?<packet_name>.+)\"(?<bot_data>[^\"]+)$");
								if (tMatch.Success)
								{
									tDataList = ("SEND " + tMatch.Groups["packet_name"].ToString().Replace(" ", "_").Replace("'", "") + tMatch.Groups["bot_data"]).Split(' ');
								}
							}

							#region IP CALCULATING

							try
							{
								// this works not in mono?!
								tBot.IP = IPAddress.Parse(tDataList[2]);
								tBot.Commit();
							}
							catch (FormatException)
							{
								#region WTF - FLIP THE IP BECAUSE ITS REVERSED?!

								string ip;
								try
								{
									ip = new IPAddress(long.Parse(tDataList[2])).ToString();
								}
								catch (Exception ex)
								{
									log.Fatal("Parse() " + tBot + " - can not parse bot ip from string: " + tMessage, ex);
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
									log.Fatal("Parse() " + tBot + " - can not parse bot ip '" + ip + "' from string: " + tMessage, ex);
									return;
								}

								log.Info("Parse() IP parsing failed, using this: " + realIp);
								try
								{
									tBot.IP = IPAddress.Parse(realIp);
									tBot.Commit();
								}
								catch (Exception ex)
								{
									log.Fatal("Parse() " + tBot + " - can not parse bot ip from string: " + tMessage, ex);
									return;
								}

								#endregion
							}

							#endregion

							try
							{
								tPort = int.Parse(tDataList[3]);
							}
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot + " - can not parse bot port from string: " + tMessage, ex);
								return;
							}
							// we cant connect to port <= 0
							if (tPort <= 0)
							{
								log.Error("Parse() " + tBot + " submitted wrong port: " + tPort + ", disabling packet");
								tPacket.Enabled = false;

								FireNotificationAdded(new Notification(Notification.Types.BotSubmittedWrongPort, tBot));
							}
							else
							{
								tPacket.RealName = tDataList[1];
								try
								{
									tPacket.RealSize = Int64.Parse(tDataList[4]);
								}
								catch (Exception ex)
								{
									log.Fatal("Parse() " + tBot + " - can not parse packet size from string: " + tMessage, ex);
									return;
								}

								tChunk = FileActions.NextAvailablePartSize(tPacket.RealName, tPacket.RealSize);
								if (tChunk < 0)
								{
									log.Error("Parse() file for " + tPacket + " from " + tBot + " already in use, disabling packet");
									tPacket.Enabled = false;
									OnUnRequestFromBot(aServer, tBot);
								}
								else if (tChunk > 0)
								{
									log.Info("Parse() try resume from " + tBot + " for " + tPacket + " @ " + tChunk);
									OnSendPrivateMessage(aServer, tBot, "\u0001DCC RESUME " + tPacket.RealName + " " + tPort + " " + tChunk + "\u0001");
								}
								else
								{
									isOk = true;
								}
							}
						}
						else if (tDataList[0] == "ACCEPT")
						{
							log.Info("Parse() DCC resume accepted from " + tBot);
							try
							{
								tPort = int.Parse(tDataList[2]);
							}
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot + " - can not parse bot port from string: " + tMessage, ex);
								return;
							}
							try
							{
								tChunk = Int64.Parse(tDataList[3]);
							}
							catch (Exception ex)
							{
								log.Fatal("Parse() " + tBot + " - can not parse packet chunk from string: " + tMessage, ex);
								return;
							}
							isOk = true;
						}

						tPacket.Commit();
						if (isOk)
						{
							log.Info("Parse() downloading from " + tBot + " - Starting: " + tChunk + " - Size: " + tPacket.RealSize);
							OnAddDownload(tPacket, tChunk, tBot.IP, tPort);
						}
					}
				}
				else
				{
					log.Error("Parse() DCC not activated from " + tBot);
				}
			}
		}

		#endregion
	}
}
