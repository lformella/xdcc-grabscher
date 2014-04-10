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

using Meebey.SmartIrc4net;

namespace XG.Plugin.Irc.Parser.Types.Dcc
{
	public class Version : AParser
	{
		protected override bool ParseInternal(IrcConnection aConnection, string aMessage, IrcEventArgs aEvent)
		{
			var args = aEvent as CtcpEventArgs;
			if (args != null)
			{
				if (args.CtcpCommand == Rfc2812.Version())
				{
					CheckVersion(aConnection, aEvent.Data.Nick, aMessage);
				}
			}
			else if (aEvent.Data.Type == ReceiveType.QueryNotice)
			{
				if (aMessage.StartsWith("\u0001" + Rfc2812.Version() + " "))
				{
					CheckVersion(aConnection, aEvent.Data.Nick, aMessage.Substring(9));
				}
			}
			return false;
		}

		private void CheckVersion(IrcConnection aConnection, string aNick, string aVersion)
		{
			Log.Info("Parse() received version reply from " + aNick + ": " + aVersion);
			if (aVersion.ToLower().Contains("iroffer"))
			{
				FireXdccList(this, new Model.Domain.EventArgs<Model.Domain.Server, string, string>(aConnection.Server, aNick, "XDCC HELP"));
			}
		}
	}
}

