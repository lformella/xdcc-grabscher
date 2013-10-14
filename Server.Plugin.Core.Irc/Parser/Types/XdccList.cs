// 
//  XdccList.cs
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

using System.Text.RegularExpressions;

using XG.Core;

using Meebey.SmartIrc4net;

namespace XG.Server.Plugin.Core.Irc.Parser.Types
{
	public class XdccList : AParser
	{
		protected override bool ParseInternal(IrcConnection aConnection, string aMessage, IrcEventArgs aEvent)
		{
			var regexes = new string[]
			{
				".* XDCC LIST ALL(\"|'|)\\s*.*"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				FireXdccList(this, new EventArgs<XG.Core.Server, string, string>(aConnection.Server, aEvent.Data.Nick, "XDCC LIST ALL"));
				return true;
			}

			regexes = new string[]
			{
				".* XDCC LIST(\"|'|)\\s*.*"
			};
			match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				FireXdccList(this, new EventArgs<XG.Core.Server, string, string>(aConnection.Server, aEvent.Data.Nick, "XDCC LIST"));
				return true;
			}

			regexes = new string[]
			{
				".* XDCC SEND LIST(\"|'|)\\s*.*"
			};
			match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				FireXdccList(this, new EventArgs<XG.Core.Server, string, string>(aConnection.Server, aEvent.Data.Nick, "XDCC SEND LIST"));
				return true;
			}

			regexes = new string[]
			{
				"^group: (?<group>[-a-z0-9_,.{}\\[\\]\\(\\)]+) .*"
			};
			match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				FireXdccList(this, new EventArgs<XG.Core.Server, string, string>(aConnection.Server, aEvent.Data.Nick, "XDCC LIST " + match.Groups["group"].ToString()));
				return true;
			}
			return false;
		}
	}
}
