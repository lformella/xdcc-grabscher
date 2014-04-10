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
using Meebey.SmartIrc4net;
using NUnit.Framework;
using XG.Model.Domain;

namespace XG.Test.Plugin.Irc.Parser.Types.Dcc
{
	[TestFixture()]
	public class Version : AParser
	{
		[Test()]
		public void BandwitdhParseTest()
		{
			var parser = new XG.Plugin.Irc.Parser.Types.Dcc.Version();
			EventArgs<XG.Model.Domain.Server, string, string> raisedEvent = null;
			parser.OnXdccList += (sender, e) => raisedEvent = e;
			string aExpectedCommand = "XDCC HELP";

			raisedEvent = null;
			Parse(parser, Connection, CreateIrcEventArgs(Channel.Name, Bot.Name, "\u0001VERSION *** Iroffer v2.0 Creato Da ArSeNiO ***", ReceiveType.QueryNotice));

			Assert.AreEqual(Server, raisedEvent.Value1);
			Assert.AreEqual(Bot.Name, raisedEvent.Value2);
			Assert.AreEqual(aExpectedCommand, raisedEvent.Value3);

//			raisedEvent = null;
//			Parse(parser, Connection, CreateCtcpEventArgs(Channel.Name, Bot.Name, "*** iroffer v2.0 Creato Da ArSeNiO ***", ReceiveType.CtcpReply, Rfc2812.Version()));
//			parser.OnXdccList += (sender, e) => raisedEvent = e;
//
//			Assert.AreEqual(Server, raisedEvent.Value1);
//			Assert.AreEqual(Bot.Name, raisedEvent.Value2);
//			Assert.AreEqual(aExpectedCommand, raisedEvent.Value3);
		}
	}
}

