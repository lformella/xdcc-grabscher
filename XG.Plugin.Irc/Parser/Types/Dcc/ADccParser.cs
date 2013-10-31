//
//  ADccParser.cs
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
using System.Net;
using Meebey.SmartIrc4net;

namespace XG.Plugin.Irc.Parser.Types.Dcc
{
	public abstract class ADccParser : AParser
	{
		#region PARSING
		
		protected override bool ParseInternal(IrcConnection aConnection, string aMessage, IrcEventArgs aEvent)
		{
			var args = aEvent as CtcpEventArgs;
			if (args != null)
			{
				if (args.CtcpCommand == "DCC")
				{
					return ParseInternal(aConnection, args.Data.Nick, args.CtcpParameter);
				}
			}
			return false;
		}

		protected abstract bool ParseInternal(IrcConnection aConnection, string aUser, string aMessage);

		protected static IPAddress TryCalculateIp(string aIp)
		{
			try
			{
				// this works not in mono?!
				return IPAddress.Parse(aIp);
			}
			catch (FormatException)
			{
				#region WTF - FLIP THE IP BECAUSE ITS REVERSED?!

				string ip = new IPAddress(long.Parse(aIp)).ToString();

				string realIp = "";
				int pos = ip.LastIndexOf('.');

				realIp += ip.Substring(pos + 1) + ".";
				ip = ip.Substring(0, pos);
				pos = ip.LastIndexOf('.');
				realIp += ip.Substring(pos + 1) + ".";
				ip = ip.Substring(0, pos);
				pos = ip.LastIndexOf('.');
				realIp += ip.Substring(pos + 1) + ".";
				ip = ip.Substring(0, pos);
				pos = ip.LastIndexOf('.');
				realIp += ip.Substring(pos + 1);

				return IPAddress.Parse(realIp);

				#endregion
			}
		}

		#endregion
	}
}

