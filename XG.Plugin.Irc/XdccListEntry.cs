//
//  XdccListEntry.cs
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

using System;
using System.Collections.Generic;
using XG.Config.Properties;

namespace XG.Plugin.Irc
{
	public class XdccListEntry
	{
		public string User { get; private set; }
		public Queue<string> Commands { get; private set; }
		public DateTime WaitUntil { get; set; }

		public XdccListEntry (string aUser, string aCommand)
		{
			User = aUser;
			Commands = new Queue<string>();
			Commands.Enqueue(aCommand);
			IncreaseTime();
		}

		public void IncreaseTime()
		{
			WaitUntil = DateTime.Now.AddSeconds(Settings.Default.CommandWaitTime);
		}
	}
}
