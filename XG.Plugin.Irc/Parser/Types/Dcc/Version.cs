//
//  Version.cs
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
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types.Dcc
{
	public class Version : AParser
	{
		public override bool Parse(Message aMessage)
		{
			if (aMessage.Text.StartsWith("\u0001VERSION ", StringComparison.Ordinal))
			{
				CheckVersion(aMessage.Channel, aMessage.Nick, aMessage.Text.Substring(9));
				return true;
			}
			return false;
		}

		void CheckVersion(Channel aChannel, string aNick, string aVersion)
		{
			Log.Info("Parse() received version reply from " + aNick + ": " + aVersion);
			if (aVersion.ToLower().Contains("iroffer"))
			{
				FireXdccList(this, new EventArgs<Channel, string, string>(aChannel, aNick, "XDCC HELP"));
			}
		}
	}
}
