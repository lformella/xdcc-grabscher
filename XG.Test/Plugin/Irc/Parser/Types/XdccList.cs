//
//  XdccList.cs
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

namespace XG.Test.Plugin.Irc.Parser.Types
{
	[TestFixture()]
	public class XdccList : AParser
	{
		[Test()]
		public void XdccListParseTest()
		{
			var parser = new XG.Plugin.Irc.Parser.Types.XdccList();

			EventArgs<XG.Model.Domain.Server, string, string> raisedEvent1;
			parser.OnXdccList += (sender, e) => raisedEvent1 = e;

			raisedEvent1 = null;
			Parse(parser, Connection, CreateIrcEventArgs(Channel.Name, Bot.Name, "** Download Liste der Pakete: \"/MSG [XG]TestBot XDCC LIST\" **", ReceiveType.QueryNotice));
			Assert.AreEqual(Server, raisedEvent1.Value1);
			Assert.AreEqual(Bot.Name, raisedEvent1.Value2);
			Assert.AreEqual("XDCC LIST", raisedEvent1.Value3);

			raisedEvent1 = null;
			Parse(parser, Connection, CreateIrcEventArgs(Channel.Name, Bot.Name, "** Download Liste der Pakete: \"/MSG [XG]TestBot XDCC LIST ALL\" **", ReceiveType.QueryNotice));
			Assert.AreEqual(Server, raisedEvent1.Value1);
			Assert.AreEqual(Bot.Name, raisedEvent1.Value2);
			Assert.AreEqual("XDCC LIST ALL", raisedEvent1.Value3);

			raisedEvent1 = null;
			Parse(parser, Connection, CreateIrcEventArgs(Channel.Name, Bot.Name, "group: TOKINO - Toki no Tabibito: Time Stranger", ReceiveType.QueryNotice));
			Assert.AreEqual(Server, raisedEvent1.Value1);
			Assert.AreEqual(Bot.Name, raisedEvent1.Value2);
			Assert.AreEqual("XDCC LIST TOKINO", raisedEvent1.Value3);

			raisedEvent1 = null;
			Parse(parser, Connection, CreateIrcEventArgs(Channel.Name, Bot.Name, "group: maji[720p] - Maji de Watashi ni Koi Shinasai[720p]", ReceiveType.QueryNotice));
			Assert.AreEqual(Server, raisedEvent1.Value1);
			Assert.AreEqual(Bot.Name, raisedEvent1.Value2);
			Assert.AreEqual("XDCC LIST maji[720p]", raisedEvent1.Value3);

			raisedEvent1 = null;
			Parse(parser, Connection, CreateIrcEventArgs(Channel.Name, Bot.Name, "** Download Liste der Pakete: \"/MSG [XG]TestBot XDCC SEND LIST\" **", ReceiveType.QueryNotice));
			Assert.AreEqual(Server, raisedEvent1.Value1);
			Assert.AreEqual(Bot.Name, raisedEvent1.Value2);
			Assert.AreEqual("XDCC SEND LIST", raisedEvent1.Value3);
		}
	}
}

