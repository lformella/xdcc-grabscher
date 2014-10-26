// 
//  DccPending.cs
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
	public class DccPending : ASaveBotMessageParser
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				Helper.Magicstring + " You have a DCC pending, Set your client to receive the transfer\\. ((Type .*|Send XDCC CANCEL) to abort the transfer\\. |)\\((?<time>[0-9]+) seconds remaining until timeout\\)",
				Helper.Magicstring + " Du hast eine .bertragung schwebend, Du mu.t den Download jetzt annehmen\\. ((Schreibe .*|Sende XDCC CANCEL) an den Bot um die .bertragung abzubrechen\\. |)\\((?<time>[0-9]+) Sekunden bis zum Abbruch\\)"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				int valueInt;
				if (int.TryParse(match.Groups["time"].ToString(), out valueInt))
				{
					if (valueInt == 30 && aBot.State != Bot.States.Active)
					{
						aBot.State = Bot.States.Idle;
					}
					FireQueueRequestFromBot(this, new EventArgs<Bot, int>(aBot, (valueInt + 2) * 1000));
				}
			}
			return match.Success;
		}
	}
}
