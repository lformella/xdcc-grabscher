// 
//  Nickserv.cs
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
using System.Reflection;
using System.Text.RegularExpressions;

using log4net;

namespace XG.Server.Irc
{
	public class Nickserv : AParser
	{
		readonly List<Core.Server> _authenticatedServer = new List<Core.Server>();

		#region PARSING

		protected override void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");

			string tUserName = aCommands[0].Split('!')[0];

			if (Matches(aMessage, ".*Password incorrect.*"))
			{
				log.Error("password wrong");
				return;
			}

			if (Matches(aMessage, ".*(The given email address has reached it's usage limit of 1 user|This nick is being held for a registered user).*"))
			{
				log.Error("nick or email already used");
				return;
			}

			if (Matches(aMessage, ".*Your nick isn't registered.*"))
			{
				log.Info("registering nick");
				if (Settings.Instance.AutoRegisterNickserv && Settings.Instance.IrcPasswort != "" && Settings.Instance.IrcRegisterEmail != "")
				{
					FireSendData(aServer, tUserName + " register " + Settings.Instance.IrcPasswort + " " + Settings.Instance.IrcRegisterEmail);
				}
				return;
			}

			if (Matches(aMessage, ".*Nickname is .*in use.*"))
			{
				FireSendData(aServer, tUserName + " ghost " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				FireSendData(aServer, tUserName + " recover " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				FireSendData(aServer, "nick " + Settings.Instance.IrcNick);
				return;
			}

			if (Matches(aMessage, ".*Services Enforcer.*"))
			{
				FireSendData(aServer, tUserName + " recover " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				FireSendData(aServer, tUserName + " release " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				FireSendData(aServer, "nick " + Settings.Instance.IrcNick);
				return;
			}

			if (Matches(aMessage, ".*(This nickname is registered and protected|This nick is being held for a registered user|msg NickServ IDENTIFY).*"))
			{
				if (Settings.Instance.IrcPasswort != "" && !_authenticatedServer.Contains(aServer))
				{
					_authenticatedServer.Add(aServer);
					//TODO check if we are really registered
					FireSendData(aServer, tUserName + " identify " + Settings.Instance.IrcPasswort);
				}
				else
				{
					log.Error("nick is already registered and i got no password");
				}
				return;
			}

			if (Matches(aMessage, ".*You must have been using this nick for at least 30 seconds to register.*"))
			{
				//TODO sleep the given time and reregister
				FireSendData(aServer, tUserName + " register " + Settings.Instance.IrcPasswort + " " + Settings.Instance.IrcRegisterEmail);
				return;
			}

			if (Matches(aMessage, ".*Please try again with a more obscure password.*"))
			{
				log.Error("password is unsecure");
				return;
			}

			if (Matches(aMessage, ".*(A passcode has been sent to|This nick is awaiting an e-mail verification code).*"))
			{
				log.Error("confirm email");
				return;
			}

			if (Matches(aMessage, ".*Nickname .*registered under your account.*"))
			{
				log.Info("nick registered succesfully");
				return;
			}

			if (Matches(aMessage, ".*Password accepted.*"))
			{
				log.Info("password accepted");
				return;
			}

			if (Matches(aMessage, ".*Please type .*to complete registration.*"))
			{
				Match tMatch = Regex.Match(aMessage, ".* NickServ confirm (?<code>[^\\s]+) .*", RegexOptions.IgnoreCase);
				if (tMatch.Success)
				{
					FireSendData(aServer, "/msg NickServ confirm " + tMatch.Groups["code"]);
					log.Info("Parse(" + aRawData + ") - confirming nickserv");
				}
				else
				{
					log.Error("Parse(" + aRawData + ") - cant find nickserv code");
				}
				return;
			}

			if (Matches(aMessage, ".*Your password is.*"))
			{
				log.Info("password accepted");
				return;
			}

			log.Error("unknow command: " + aRawData);
		}

		#endregion

		bool Matches(string aMessage, string aRegex)
		{
			Match tMatch = Regex.Match(aMessage, aRegex, RegexOptions.IgnoreCase);
			return tMatch.Success;
		}
	}
}
