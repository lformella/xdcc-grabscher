// 
//  AlreadyReceiving.cs
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
	public class AlreadyReceiving : ASaveBotMessageParser
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				"Denied, You already have ([0-9]+) item(s|) queued, Try Again Later",
				Helper.Magicstring + " All Slots Full, Denied, You already have that item queued\\.",
				"You are already receiving or are queued for the maximum number of packs .*",
				"Du hast max\\. ([0-9]+) transfer auf einmal, Du bist jetzt in der Warteschlange f.r Datei .*",
				"Es laufen bereits genug .bertragungen, abgewiesen, Du hast diese Datei bereits in der Warteschlange\\."
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				if (aBot.State == Bot.States.Idle)
				{
					aBot.State = Bot.States.Waiting;
				}
				else if (aBot.State == Bot.States.Waiting)
				{
					// if there is no active packets lets remove us from the queue
					if (aBot.OldestActivePacket() == null)
					{
						FireUnRequestFromBot(this, new EventArgs<Bot>(aBot));
					}
				}
			}
			return match.Success;
		}
	}
}
