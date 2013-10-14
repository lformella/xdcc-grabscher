// 
//  Nickserv.cs
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

using System.Collections.Generic;
using System.Text.RegularExpressions;

using XG.Core;

using Meebey.SmartIrc4net;

namespace XG.Server.Plugin.Core.Irc.Parser.Types
{
	public class Nickserv : AParser
	{
		readonly HashSet<XG.Core.Server> _authenticatedServer = new HashSet<XG.Core.Server>();

		protected override bool ParseInternal(IrcConnection aConnection, string aMessage, IrcEventArgs aEvent)
		{
			if (aEvent.Data.Nick != null && aEvent.Data.Nick.ToLower() == "nickserv")
			{
				if (Helper.Match(aMessage, ".*Password incorrect.*").Success)
				{
					Log.Error("password wrong");
				}

				else if (Helper.Match(aMessage, ".*(The given email address has reached it's usage limit of 1 user|This nick is being held for a registered user).*").Success)
				{
					Log.Error("nick or email already used");
				}

				if (Helper.Match(aMessage, ".*Your nick isn't registered.*").Success)
				{
					Log.Info("registering nick");
					if (Settings.Instance.AutoRegisterNickserv && Settings.Instance.IrcPasswort != "" && Settings.Instance.IrcRegisterEmail != "")
					{
						FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, aEvent.Data.Nick + " register " + Settings.Instance.IrcPasswort + " " + Settings.Instance.IrcRegisterEmail));
					}
				}

				else if (Helper.Match(aMessage, ".*Nickname is .*in use.*").Success)
				{
					FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, aEvent.Data.Nick + " ghost " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort));
					FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, aEvent.Data.Nick + " recover " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort));
					FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, "nick " + Settings.Instance.IrcNick));
				}

				else if (Helper.Match(aMessage, ".*Services Enforcer.*").Success)
				{
					FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, aEvent.Data.Nick + " recover " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort));
					FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, aEvent.Data.Nick + " release " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort));
					FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, "nick " + Settings.Instance.IrcNick));
				}

				else if (Helper.Match(aMessage, ".*(This nickname is registered and protected|This nick is being held for a registered user|msg NickServ IDENTIFY).*").Success)
				{
					if (Settings.Instance.IrcPasswort != "" && !_authenticatedServer.Contains(aConnection.Server))
					{
						_authenticatedServer.Add(aConnection.Server);
						//TODO check if we are really registered
						FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, aEvent.Data.Nick + " identify " + Settings.Instance.IrcPasswort));
					}
					else
					{
						Log.Error("nick is already registered and i got no password");
					}
				}

				else if (Helper.Match(aMessage, ".*You must have been using this nick for at least 30 seconds to register.*").Success)
				{
					//TODO sleep the given time and reregister
					FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, aEvent.Data.Nick + " register " + Settings.Instance.IrcPasswort + " " + Settings.Instance.IrcRegisterEmail));
				}

				else if (Helper.Match(aMessage, ".*Please try again with a more obscure password.*").Success)
				{
					Log.Error("password is unsecure");
				}

				else if (Helper.Match(aMessage, ".*(A passcode has been sent to|This nick is awaiting an e-mail verification code).*").Success)
				{
					Log.Error("confirm email");
				}

				else if (Helper.Match(aMessage, ".*Nickname .*registered under your account.*").Success)
				{
					Log.Info("nick registered succesfully");
				}

				else if (Helper.Match(aMessage, ".*Password accepted.*").Success)
				{
					Log.Info("password accepted");
				}

				else if (Helper.Match(aMessage, ".*Please type .*to complete registration.*").Success)
				{
					Match tMatch = Regex.Match(aMessage, ".* NickServ confirm (?<code>[^\\s]+) .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						FireSendData(this, new EventArgs<XG.Core.Server, string>(aConnection.Server, "/msg NickServ confirm " + tMatch.Groups["code"]));
						Log.Info("Parse(" + aEvent.Data.RawMessage + ") - confirming nickserv");
					}
					else
					{
						Log.Error("Parse(" + aEvent.Data.RawMessage + ") - cant find nickserv code");
					}
				}

				else if (Helper.Match(aMessage, ".*Your password is.*").Success)
				{
					Log.Info("password accepted");
				}

				Log.Error("unknow command: " + aEvent.Data.RawMessage);
				return true;
			}
			return false;
		}
	}
}
