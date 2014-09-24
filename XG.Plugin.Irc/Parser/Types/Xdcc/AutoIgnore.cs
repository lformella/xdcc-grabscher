// 
//  AutoIgnore.cs
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

namespace XG.Plugin.Irc.Parser.Types.Xdcc
{
	public class AutoIgnore : ASaveBotMessageParser
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				"Punish-ignore activated for .* \\(.*\\) (?<time_m>[0-9]*) minutes",
				"Auto-ignore activated for .* lasting (?<time_m>[0-9]*)m(?<time_s>[0-9]*)s\\. Further messages will increase duration\\.",
				"Zur Strafe wirst du .* \\(.*\\) f.r (?<time_m>[0-9]*) Minuten ignoriert(.|)",
				"Auto-ignore activated for .* \\(.*\\)"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				if (aBot.State == Bot.States.Waiting)
				{
					aBot.State = Bot.States.Idle;
				}

				int valueInt;
				if (int.TryParse(match.Groups["time_m"].ToString(), out valueInt))
				{
					int time = valueInt * 60 + 1;
					if (int.TryParse(match.Groups["time_s"].ToString(), out valueInt))
					{
						time += valueInt;
					}
					FireQueueRequestFromBot(this, new EventArgs<Bot, int>(aBot, time * 1000));
				}
			}
			return match.Success;
		}
	}
}
