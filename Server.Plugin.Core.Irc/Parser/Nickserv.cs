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
using System.Reflection;
using System.Text.RegularExpressions;

using XG.Core;

using log4net;
using Meebey.SmartIrc4net;

namespace XG.Server.Plugin.Core.Irc.Parser
{
	public class Nickserv
	{
		#region EVENTS

		public event ServerDataTextDelegate OnSendData;

		#endregion

		readonly HashSet<XG.Core.Server> _authenticatedServer = new HashSet<XG.Core.Server>();

		#region PARSING

		public void Parse(XG.Core.Server aServer, IrcEventArgs aEvent)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");
			string tMessage = Helper.RemoveSpecialIrcChars(aEvent.Data.Message);

			if (Helper.Matches(tMessage, ".*Password incorrect.*"))
			{
				log.Error("password wrong");
				return;
			}

			if (Helper.Matches(tMessage, ".*(The given email address has reached it's usage limit of 1 user|This nick is being held for a registered user).*"))
			{
				log.Error("nick or email already used");
				return;
			}

			if (Helper.Matches(tMessage, ".*Your nick isn't registered.*"))
			{
				log.Info("registering nick");
				if (Settings.Instance.AutoRegisterNickserv && Settings.Instance.IrcPasswort != "" && Settings.Instance.IrcRegisterEmail != "")
				{
					OnSendData(aServer, aEvent.Data.Nick + " register " + Settings.Instance.IrcPasswort + " " + Settings.Instance.IrcRegisterEmail);
				}
				return;
			}

			if (Helper.Matches(tMessage, ".*Nickname is .*in use.*"))
			{
				OnSendData(aServer, aEvent.Data.Nick + " ghost " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				OnSendData(aServer, aEvent.Data.Nick + " recover " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				OnSendData(aServer, "nick " + Settings.Instance.IrcNick);
				return;
			}

			if (Helper.Matches(tMessage, ".*Services Enforcer.*"))
			{
				OnSendData(aServer, aEvent.Data.Nick + " recover " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				OnSendData(aServer, aEvent.Data.Nick + " release " + Settings.Instance.IrcNick + " " + Settings.Instance.IrcPasswort);
				OnSendData(aServer, "nick " + Settings.Instance.IrcNick);
				return;
			}

			if (Helper.Matches(tMessage, ".*(This nickname is registered and protected|This nick is being held for a registered user|msg NickServ IDENTIFY).*"))
			{
				if (Settings.Instance.IrcPasswort != "" && !_authenticatedServer.Contains(aServer))
				{
					_authenticatedServer.Add(aServer);
					//TODO check if we are really registered
					OnSendData(aServer, aEvent.Data.Nick + " identify " + Settings.Instance.IrcPasswort);
				}
				else
				{
					log.Error("nick is already registered and i got no password");
				}
				return;
			}

			if (Helper.Matches(tMessage, ".*You must have been using this nick for at least 30 seconds to register.*"))
			{
				//TODO sleep the given time and reregister
				OnSendData(aServer, aEvent.Data.Nick + " register " + Settings.Instance.IrcPasswort + " " + Settings.Instance.IrcRegisterEmail);
				return;
			}

			if (Helper.Matches(tMessage, ".*Please try again with a more obscure password.*"))
			{
				log.Error("password is unsecure");
				return;
			}

			if (Helper.Matches(tMessage, ".*(A passcode has been sent to|This nick is awaiting an e-mail verification code).*"))
			{
				log.Error("confirm email");
				return;
			}

			if (Helper.Matches(tMessage, ".*Nickname .*registered under your account.*"))
			{
				log.Info("nick registered succesfully");
				return;
			}

			if (Helper.Matches(tMessage, ".*Password accepted.*"))
			{
				log.Info("password accepted");
				return;
			}

			if (Helper.Matches(tMessage, ".*Please type .*to complete registration.*"))
			{
				Match tMatch = Regex.Match(tMessage, ".* NickServ confirm (?<code>[^\\s]+) .*", RegexOptions.IgnoreCase);
				if (tMatch.Success)
				{
					OnSendData(aServer, "/msg NickServ confirm " + tMatch.Groups["code"]);
					log.Info("Parse(" + aEvent.Data.RawMessage + ") - confirming nickserv");
				}
				else
				{
					log.Error("Parse(" + aEvent.Data.RawMessage + ") - cant find nickserv code");
				}
				return;
			}

			if (Helper.Matches(tMessage, ".*Your password is.*"))
			{
				log.Info("password accepted");
				return;
			}

			log.Error("unknow command: " + aEvent.Data.RawMessage);
		}

		#endregion
	}
}
