//
//  ASaveBotMessageParser.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
//
//  Copyright (c) 2013 Lars Formella
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

namespace XG.Plugin.Irc.Parser.Types
{
	public abstract class ASaveBotMessageParser : AParser
	{
		public override bool Parse(Message aMessage)
		{
			bool result = false;
			Bot tBot = aMessage.Channel.Bot(aMessage.Nick);
			if (tBot != null)
			{
				result = ParseInternal(tBot, aMessage.Text);

				if (result)
				{
					Log.Info("Parse() message from " + tBot + ": " + aMessage.Text);
					tBot.LastMessage = aMessage.Text;
				}

				// set em to connected if it isnt already
				tBot.Connected = true;
				tBot.Commit();
			}
			return result;
		}

		protected abstract bool ParseInternal(Bot aBot, string aMessage);
	}
}
