// 
//  Status.cs
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

using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types.Info
{
	public class Status : AParserWithExistingBot
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				Helper.Magicstring + " ([0-9]*) (pack(s|)|Pa(c|)ket(e|)|Fil[e]+s) " + Helper.Magicstring + "\\s*(?<slot_cur>[0-9]*) (of|von) (?<slot_total>[0-9]*) (slot(s|)|Pl(a|�|.)tz(e|)) (open|opened|free|frei|in use|offen)(, ((Queue|Warteschlange): (?<queue_cur>[0-9]*)(\\/| of )(?<queue_total>[0-9]*),|).*(Record( [a-zA-Z]+|): (?<record>[0-9.]*)(K|)B\\/s|)|)"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				int valueInt;
				if (int.TryParse(match.Groups["slot_cur"].ToString(), out valueInt))
				{
					aBot.InfoSlotCurrent = valueInt;
				}
				if (int.TryParse(match.Groups["slot_total"].ToString(), out valueInt))
				{
					aBot.InfoSlotTotal = valueInt;
				}
				if (int.TryParse(match.Groups["queue_cur"].ToString(), out valueInt))
				{
					aBot.InfoQueueCurrent = valueInt;
				}
				if (int.TryParse(match.Groups["queue_total"].ToString(), out valueInt))
				{
					aBot.InfoQueueTotal = valueInt;
				}

				if (aBot.InfoSlotCurrent > aBot.InfoSlotTotal)
				{
					aBot.InfoSlotTotal = aBot.InfoSlotCurrent;
				}
				if (aBot.InfoQueueCurrent > aBot.InfoQueueTotal)
				{
					aBot.InfoQueueTotal = aBot.InfoQueueCurrent;
				}

				// uhm, there is a free slot and we are still waiting?
				if (aBot.InfoSlotCurrent > 0 && aBot.State == Bot.States.Waiting)
				{
					aBot.State = Bot.States.Idle;
					FireQueueRequestFromBot(this, new EventArgs<Bot, int>(aBot, 0));
				}
			}
			return match.Success;
		}
	}
}
