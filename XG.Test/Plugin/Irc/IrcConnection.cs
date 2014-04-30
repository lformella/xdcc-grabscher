// 
//  IrcConnection.cs
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

using System;
using System.Net;
using System.Reflection;
using Meebey.SmartIrc4net;
using XG.Model.Domain;
using NUnit.Framework;
using System.Collections.Generic;

namespace XG.Test.Plugin.Irc
{
	[TestFixture()]
	public class IrcConnection
	{
		[Test()]
		public void GetChannelsToJoinTest()
		{
			XG.Plugin.Irc.IrcConnection ircConnection = new XG.Plugin.Irc.IrcConnection();

			Server server = new Server();
			ircConnection.Server = server;

			Bot bot1 = new Bot();
			List<string> channelsToPart1 = new List<string>();
			channelsToPart1.Add("#test1");
			channelsToPart1.Add("#test2");
			ircConnection.TemporaryPartChannels(null, new EventArgs<Server, Bot, List<string>>(server, bot1, channelsToPart1));
			
			Bot bot2 = new Bot();
			List<string> channelsToPart2 = new List<string>();
			channelsToPart2.Add("#test1");
			channelsToPart2.Add("#test3");
			ircConnection.TemporaryPartChannels(null, new EventArgs<Server, Bot, List<string>>(server, bot2, channelsToPart2));

			bot1.State = Bot.States.Idle;
			CollectionAssert.AreEquivalent(new string[]{"#test1", "#test3"}, ircConnection.GetChannelsToJoin(bot2));

			bot1.State = Bot.States.Active;
			CollectionAssert.AreEquivalent(new string[]{"#test3"}, ircConnection.GetChannelsToJoin(bot2));

			bot1.State = Bot.States.Idle;
			CollectionAssert.AreEquivalent(new string[]{"#test1", "#test3"}, ircConnection.GetChannelsToJoin(bot2));
		}
	}
}
