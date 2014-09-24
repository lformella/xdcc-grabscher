// 
//  Queued.cs
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

using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types.Xdcc
{
	public class Queued : ASaveBotMessageParser
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				"Queued ([0-9]+)h([0-9]+)m for .*, in position (?<queue_cur>[0-9]+) of (?<queue_total>[0-9]+). (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m or .* remaining\\.",
				"In der Warteschlange seit  ([0-9]+)h([0-9]+)m f.r .*, in Position (?<queue_cur>[0-9]+) von (?<queue_total>[0-9]+). Ungef.hr (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m oder .*"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				if (aBot.State == Bot.States.Idle)
				{
					aBot.State = Bot.States.Waiting;
				}

				int valueInt;
				aBot.InfoSlotCurrent = 0;
				if (int.TryParse(match.Groups["queue_cur"].ToString(), out valueInt))
				{
					aBot.QueuePosition = valueInt;
				}
				if (int.TryParse(match.Groups["queue_total"].ToString(), out valueInt))
				{
					aBot.InfoQueueTotal = valueInt;
				}
				else if (aBot.InfoQueueTotal < aBot.QueuePosition)
				{
					aBot.InfoQueueTotal = aBot.QueuePosition;
				}

				int time = 0;
				if (int.TryParse(match.Groups["queue_m"].ToString(), out valueInt))
				{
					time += valueInt * 60;
				}
				if (int.TryParse(match.Groups["queue_h"].ToString(), out valueInt))
				{
					time += valueInt * 60 * 60;
				}
				aBot.QueueTime = time;
			}
			return match.Success;
		}
	}
}
