// 
//  ExistingBot.cs
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
using System.Text.RegularExpressions;

using XG.Core;
using Meebey.SmartIrc4net;

namespace XG.Server.Plugin.Core.Irc.Parser.Types.Dcc
{
	public class DownloadFromBot : ADccParser
	{
		protected override bool ParseInternal(IrcConnection aConnection, string aUser, string aMessage)
		{
			Bot tBot = aConnection.Server.Bot(aUser);
			if (tBot == null)
			{
				return false;
			}

			Packet tPacket = tBot.OldestActivePacket();
			if (tPacket == null)
			{
				Log.Error("Parse() DCC not activated from " + tBot);
				return false;
			}

			if (tPacket.Connected)
			{
				Log.Error("Parse() ignoring dcc from " + tBot + " because " + tPacket + " is already connected");
			}
			else
			{
				bool isOk = false;

				int tPort = 0;
				Int64 tChunk = 0;
				
				string[] tDataList = aMessage.Split(' ');
				if (tDataList[0] == "SEND")
				{
					Log.Info("Parse() DCC from " + tBot);

					// if the name of the file contains spaces, we have to replace em
					if (aMessage.StartsWith("SEND \""))
					{
						Match tMatch = Regex.Match(aMessage, "SEND \"(?<packet_name>.+)\"(?<bot_data>[^\"]+)$");
						if (tMatch.Success)
						{
							tDataList = ("SEND " + tMatch.Groups["packet_name"].ToString().Replace(" ", "_").Replace("'", "") + tMatch.Groups["bot_data"]).Split(' ');
						}
					}

					try
					{
						tBot.IP = TryCalculateIp(tDataList[2]);
						tBot.Commit();
					}
					catch (Exception ex)
					{
						Log.Fatal("Parse() " + tBot + " - can not parse bot ip from string: " + aMessage, ex);
						return false;
					}

					try
					{
						tPort = int.Parse(tDataList[3]);
					}
					catch (Exception ex)
					{
						Log.Fatal("Parse() " + tBot + " - can not parse bot port from string: " + aMessage, ex);
						return false;
					}

					// we cant connect to port <= 0
					if (tPort <= 0)
					{
						Log.Error("Parse() " + tBot + " submitted wrong port: " + tPort + ", disabling packet");
						tPacket.Enabled = false;

						FireNotificationAdded(Notification.Types.BotSubmittedWrongPort, tBot);
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
							Log.Fatal("Parse() " + tBot + " - can not parse packet size from string: " + aMessage, ex);
							return false;
						}

						tChunk = FileActions.NextAvailablePartSize(tPacket.RealName, tPacket.RealSize);
						if (tChunk < 0)
						{
							Log.Error("Parse() file for " + tPacket + " from " + tBot + " already in use, disabling packet");
							tPacket.Enabled = false;
							FireUnRequestFromBot(this, new EventArgs<XG.Core.Server, Bot>(aConnection.Server, tBot));
						}
						else if (tChunk > 0)
						{
							Log.Info("Parse() try resume from " + tBot + " for " + tPacket + " @ " + tChunk);
							FireSendMessage(this, new EventArgs<XG.Core.Server, SendType, string, string>(aConnection.Server, SendType.CtcpRequest, tBot.Name, "DCC RESUME " + tPacket.RealName + " " + tPort + " " + tChunk));
						}
						else
						{
							isOk = true;
						}
					}
				}
				else if (tDataList[0] == "ACCEPT")
				{
					Log.Info("Parse() DCC resume accepted from " + tBot);

					try
					{
						tPort = int.Parse(tDataList[2]);
					}
					catch (Exception ex)
					{
						Log.Fatal("Parse() " + tBot + " - can not parse bot port from string: " + aMessage, ex);
						return false;
					}

					try
					{
						tChunk = Int64.Parse(tDataList[3]);
					}
					catch (Exception ex)
					{
						Log.Fatal("Parse() " + tBot + " - can not parse packet chunk from string: " + aMessage, ex);
						return false;
					}

					isOk = true;
				}

				tPacket.Commit();
				if (isOk)
				{
					Log.Info("Parse() downloading from " + tBot + " - Starting: " + tChunk + " - Size: " + tPacket.RealSize);
					FireAddDownload(this, new EventArgs<Packet, long, System.Net.IPAddress, int>(tPacket, tChunk, tBot.IP, tPort));
					return true;
				}
			}
			return false;
		}
	}
}
