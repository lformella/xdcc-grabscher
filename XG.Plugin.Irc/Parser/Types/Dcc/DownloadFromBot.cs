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
using Meebey.SmartIrc4net;
using XG.Model;
using XG.Model.Domain;
using XG.Business.Helper;
using XG.Config.Properties;
using System.Net;

namespace XG.Plugin.Irc.Parser.Types.Dcc
{
	public class DownloadFromBot : AParser
	{
		public override bool Parse(Model.Domain.Channel aChannel, string aNick, string aMessage)
		{
			if (!aMessage.StartsWith("\u0001DCC ", StringComparison.Ordinal))
			{
				return false;
			}
			aMessage = aMessage.Substring(5, aMessage.Length - 6);

			Bot tBot = aChannel.Bot(aNick);
			if (tBot == null)
			{
				return false;
			}

			Packet tPacket = tBot.OldestActivePacket();
			if (tPacket == null)
			{
				Log.Error("Parse() DCC not activated from " + tBot);
				return true;
			}

			if (tPacket.Connected)
			{
				Log.Error("Parse() ignoring dcc from " + tBot + " because " + tPacket + " is already connected");
				return true;
			}

			bool isOk = false;

			int tPort = 0;
			File tFile = FileActions.TryGetFile(tPacket.RealName, tPacket.RealSize);
			Int64 startSize = 0;

			if (tFile != null)
			{
				if (tFile.Connected)
				{
					return true;
				}
				if (tFile.CurrentSize > Settings.Default.FileRollbackBytes)
				{
					startSize = tFile.CurrentSize - Settings.Default.FileRollbackBytes;
				}
			}

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
					tBot.IP = IPAddress.Parse(tDataList[2]);
				}
				catch (Exception ex)
				{
					Log.Fatal("Parse() " + tBot + " - can not parse bot ip from string: " + aMessage, ex);
					return true;
				}

				try
				{
					tPort = int.Parse(tDataList[3]);
				}
				catch (Exception ex)
				{
					Log.Fatal("Parse() " + tBot + " - can not parse bot port from string: " + aMessage, ex);
					return true;
				}

				// we cant connect to port <= 0
				if (tPort <= 0)
				{
					Log.Error("Parse() " + tBot + " submitted wrong port: " + tPort + ", disabling packet");
					tPacket.Enabled = false;
					tPacket.Commit();

					FireNotificationAdded(Notification.Types.BotSubmittedWrongData, tBot);
					return true;
				}

				tPacket.RealName = tDataList[1];

				if (tPacket.Name.Difference(tPacket.RealName) > 0.7)
				{
					FireNotificationAdded(Notification.Types.PacketNameDifferent, tPacket);
				}

				try
				{
					tPacket.RealSize = Int64.Parse(tDataList[4]);
				}
				catch (Exception ex)
				{
					Log.Fatal("Parse() " + tBot + " - can not parse packet size from string: " + aMessage, ex);
					return true;
				}

				if (tPacket.RealSize <= 0)
				{
					Log.Error("Parse() " + tBot + " submitted wrong file size: " + tPacket.RealSize + ", disabling packet");
					tPacket.Enabled = false;
					tPacket.Commit();

					FireNotificationAdded(Notification.Types.BotSubmittedWrongData, tBot);
					return true;
				}

				if (tFile != null)
				{
					if (tFile.Connected)
					{
						Log.Error("Parse() file for " + tPacket + " from " + tBot + " already in use or not found, disabling packet");
						tPacket.Enabled = false;
						FireUnRequestFromBot(this, new EventArgs<Bot>(tBot));
					}
					else if (tFile.CurrentSize > 0)
					{
						Log.Info("Parse() try resume from " + tBot + " for " + tPacket + " @ " + startSize);
						FireSendMessage(this, new EventArgs<Server, SendType, string, string>(aChannel.Parent, SendType.CtcpRequest, tBot.Name, "DCC RESUME " + tPacket.RealName + " " + tPort + " " + startSize));
					}
					else
					{
						isOk = true;
					}
				}
				else
				{
					isOk = true;
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
					return true;
				}

				try
				{
					startSize = Int64.Parse(tDataList[3]);
				}
				catch (Exception ex)
				{
					Log.Fatal("Parse() " + tBot + " - can not parse packet startSize from string: " + aMessage, ex);
					return true;
				}

				isOk = true;
			}

			tPacket.Commit();
			if (isOk)
			{
				Log.Info("Parse() downloading from " + tBot + " - Starting: " + startSize + " - Size: " + tPacket.RealSize);
				FireAddDownload(this, new EventArgs<Packet, long, System.Net.IPAddress, int>(tPacket, startSize, tBot.IP, tPort));
			}

			return true;
		}
	}
}
