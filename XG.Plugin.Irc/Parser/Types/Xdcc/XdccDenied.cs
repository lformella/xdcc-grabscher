// 
//  XdccDenied.cs
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

using XG.Config.Properties;
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types.Xdcc
{
	public class XdccDenied : ASaveBotMessageParser
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				Helper.Magicstring + " XDCC SEND denied, (?<info>.*)"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				string info = match.Groups["info"].ToString().ToLower();
				// ** XDCC SEND denied, you must have voice on a known channel to request a pack
				if (info.StartsWith("you must be on a known channel to request a pack", System.StringComparison.CurrentCulture))
				{
					FireJoinChannelsFromBot(this, new EventArgs<Bot>(aBot));
					FireQueueRequestFromBot(this, new EventArgs<Bot, int>(aBot, Settings.Default.CommandWaitTime));
				}
				else if (info.StartsWith("i don't send transfers to", System.StringComparison.CurrentCulture))
				{
					foreach (Packet tPacket in aBot.Packets)
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
					if (aBot.State == Bot.States.Waiting)
					{
						aBot.State = Bot.States.Idle;
					}
					FireQueueRequestFromBot(this, new EventArgs<Bot, int>(aBot, Settings.Default.CommandWaitTime));
					Log.Error("Parse() XDCC denied from " + aBot + ": " + info);
				}
			}
			return match.Success;
		}
	}
}
