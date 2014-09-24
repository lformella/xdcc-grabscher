// 
//  AllSlotsFull.cs
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
	public class AllSlotsFull : ASaveBotMessageParser
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				"(" + Helper.Magicstring + " All Slots Full, |)Added you to the main queue (for pack ([0-9]+) \\(\".*\"\\) |).*in positi(o|0)n (?<queue_cur>[0-9]+)\\. To Remove you(r|)self at a later time .*",
				"Queueing you for pack [0-9]+ \\(.*\\) in slot (?<queue_cur>[0-9]+)/(?<queue_total>[0-9]+)\\. To remove you(r|)self from the queue, type: .*\\. To check your position in the queue, type: .*\\. Estimated time remaining in queue: (?<queue_d>[0-9]+) days, (?<queue_h>[0-9]+) hours, (?<queue_m>[0-9]+) minutes",
				"(" + Helper.Magicstring + " |)Es laufen bereits genug .bertragungen, Du bist jetzt in der Warteschlange f.r Datei [0-9]+ \\(.*\\) in Position (?<queue_cur>[0-9]+)\\. Wenn Du sp.ter Abbrechen willst schreibe .*"
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
					aBot.InfoQueueCurrent = aBot.QueuePosition;
				}

				if (int.TryParse(match.Groups["queue_total"].ToString(), out valueInt))
				{
					aBot.InfoQueueTotal = valueInt;
				}
				else if (aBot.InfoQueueTotal < aBot.InfoQueueCurrent)
				{
					aBot.InfoQueueTotal = aBot.InfoQueueCurrent;
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
				if (int.TryParse(match.Groups["queue_d"].ToString(), out valueInt))
				{
					time += valueInt * 60 * 60 * 24;
				}
				aBot.QueueTime = time;
			}
			return match.Success;
		}
	}
}
