// 
//  Packet.cs
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
using System.Threading;
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types.Info
{
	public class Packet : AParser
	{
		public override bool Parse(Message aMessage)
		{
			string[] regexes =
			{
				"#(?<pack_id>\\d+)(\u0240|)\\s+(\\d*)x\\s+\\[\\s*(?<pack_size>[\\<\\>\\d.]+)(?<pack_add>[BbGgiKMs]+)\\]\\s+(?<pack_name>.*)"
			};
			var match = Helper.Match(aMessage.Text, regexes);
			if (match.Success)
			{
				string tUserName = aMessage.Nick;
				Bot tBot = aMessage.Channel.Bot(tUserName);
				Model.Domain.Packet newPacket = null;

				bool insertBot = false;
				if (tBot == null)
				{
					insertBot = true;
					tBot = new Bot {Name = tUserName, Connected = true, LastMessage = "initial creation", LastContact = DateTime.Now};
				}

				try
				{
					int tPacketId;
					try
					{
						tPacketId = int.Parse(match.Groups["pack_id"].ToString());
					}
					catch (Exception ex)
					{
						Log.Fatal("Parse() " + tBot + " - can not parse packet id from string: " + aMessage, ex);
						return false;
					}

					Model.Domain.Packet tPack = tBot.Packet(tPacketId);
					if (tPack == null)
					{
						tPack = new Model.Domain.Packet();
						newPacket = tPack;
						tPack.Id = tPacketId;
						tBot.AddPacket(tPack);
					}
					tPack.LastMentioned = DateTime.Now;

					string name = RemoveSpecialIrcCharsFromPacketName(match.Groups["pack_name"].ToString());
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
					string stringSize = match.Groups["pack_size"].ToString().Replace("<", "").Replace(">", "");
					if (Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
					{
						stringSize = stringSize.Replace('.', ',');
					}
					double.TryParse(stringSize, out tPacketSizeFormated);

					string tPacketAdd = match.Groups["pack_add"].ToString().ToLower();

					switch (tPacketAdd)
					{
						case "k":
						case "kb":
							tPack.Size = (Int64) (tPacketSizeFormated * 1024);
							break;

						case "m":
						case "mb":
							tPack.Size = (Int64) (tPacketSizeFormated * 1024 * 1024);
							break;

						case "g":
						case "gb":
							tPack.Size = (Int64) (tPacketSizeFormated * 1024 * 1024 * 1024);
							break;
					}

					if (tPack.Commit() && newPacket == null)
					{
						Log.Info("Parse() updated " + tPack + " from " + tBot);
					}
				}
				catch (FormatException) {}

				// insert bot if ok
				if (insertBot)
				{
					if (aMessage.Channel.AddBot(tBot))
					{
						Log.Info("Parse() inserted " + tBot);
					}
					else
					{
						var duplicateBot = aMessage.Channel.Bot(tBot.Name);
						if (duplicateBot != null)
						{
							tBot = duplicateBot;
						}
						else
						{
							Log.Error("Parse() cant insert " + tBot + " into " + aMessage.Channel);
						}
					}
				}
				// and insert packet _AFTER_ this
				if (newPacket != null)
				{
					tBot.AddPacket(newPacket);
					Log.Info("Parse() inserted " + newPacket + " into " + tBot);
				}

				tBot.Commit();
				aMessage.Channel.Commit();
			}
			return match.Success;
		}

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
