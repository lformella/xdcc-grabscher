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
using Meebey.SmartIrc4net;
using XG.Config.Properties;
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types
{
	public class Nickserv : AParser
	{
		readonly HashSet<Server> _authenticatedServer = new HashSet<Server>();

		public override bool Parse(Message aMessage)
		{
			if (aMessage.Nick != null && aMessage.Nick.ToLower() == "nickserv")
			{
				if (Helper.Match(aMessage.Text, ".*Password incorrect.*").Success)
				{
					Log.Error("password wrong");
				}

				else if (Helper.Match(aMessage.Text, ".*(The given email address has reached it's usage limit of 1 user|This nick is being held for a registered user).*").Success)
				{
					Log.Error("nick or email already used");
				}

				if (Helper.Match(aMessage.Text, ".*Your nick isn't registered.*").Success)
				{
					Log.Info("registering nick");
					if (Settings.Default.AutoRegisterNickserv && Settings.Default.IrcPasswort != "" && Settings.Default.IrcRegisterEmail != "")
					{
						FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " register " + Settings.Default.IrcPasswort + " " + Settings.Default.IrcRegisterEmail));
					}
				}

				else if (Helper.Match(aMessage.Text, ".*Nickname is .*in use.*").Success)
				{
					FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " ghost " + Settings.Default.IrcNick + " " + Settings.Default.IrcPasswort));
					FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " recover " + Settings.Default.IrcNick + " " + Settings.Default.IrcPasswort));
					FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, Rfc2812.Nick(Settings.Default.IrcNick)));
				}

				else if (Helper.Match(aMessage.Text, ".*Services Enforcer.*").Success)
				{
					FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " recover " + Settings.Default.IrcNick + " " + Settings.Default.IrcPasswort));
					FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " release " + Settings.Default.IrcNick + " " + Settings.Default.IrcPasswort));
					FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, Rfc2812.Nick(Settings.Default.IrcNick)));
				}

				else if (Helper.Match(aMessage.Text, ".*(This nickname is registered and protected|This nick is being held for a registered user|msg NickServ IDENTIFY).*").Success)
				{
					if (Settings.Default.IrcPasswort != "" && !_authenticatedServer.Contains(aMessage.Channel.Parent))
					{
						_authenticatedServer.Add(aMessage.Channel.Parent);
						//TODO check if we are really registered
						FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " identify " + Settings.Default.IrcPasswort));
					}
					else
					{
						Log.Error("nick is already registered and i got no password");
					}
				}

				else if (Helper.Match(aMessage.Text, ".*You must have been using this nick for at least 30 seconds to register.*").Success)
				{
					//TODO sleep the given time and reregister
					FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " register " + Settings.Default.IrcPasswort + " " + Settings.Default.IrcRegisterEmail));
				}

				else if (Helper.Match(aMessage.Text, ".*Please try again with a more obscure password.*").Success)
				{
					Log.Error("password is unsecure");
				}

				else if (Helper.Match(aMessage.Text, ".*(A passcode has been sent to|This nick is awaiting an e-mail verification code).*").Success)
				{
					Log.Error("confirm email");
				}

				else if (Helper.Match(aMessage.Text, ".*Nickname .*registered under your account.*").Success)
				{
					Log.Info("nick registered succesfully");
				}

				else if (Helper.Match(aMessage.Text, ".*Password accepted.*").Success)
				{
					Log.Info("password accepted");
				}

				else if (Helper.Match(aMessage.Text, ".*Please type .*to complete registration.*").Success)
				{
					Match tMatch = Regex.Match(aMessage.Text, ".* NickServ confirm (?<code>[^\\s]+) .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						FireWriteLine(this, new EventArgs<Server, string>(aMessage.Channel.Parent, aMessage.Nick + " confirm " + tMatch.Groups["code"]));
						Log.Info("Parse(" + aMessage + ") - confirming nickserv");
					}
					else
					{
						Log.Error("Parse(" + aMessage + ") - cant find nickserv code");
					}
				}

				else if (Helper.Match(aMessage.Text, ".*Your password is.*").Success)
				{
					Log.Info("password accepted");
				}

				Log.Error("unknow command: " + aMessage.Text);
				return true;
			}
			return false;
		}
	}
}
