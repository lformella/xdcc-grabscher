// 
//  Join.cs
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
	public class Join : AParserWithExistingBot
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				@".*\s+JOIN (?<channel>[^\s]+).*"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				string channel = match.Groups["channel"].ToString();
				if (!channel.StartsWith("#", System.StringComparison.CurrentCulture))
				{
					channel = "#" + channel;
				}
				FireJoinChannel(this, new EventArgs<Server, string>(aBot.Parent.Parent, channel));
			}
			return match.Success;
		}
	}
}
