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

using NUnit.Framework;
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Test.Plugin.Irc.Parser.Types
{
	[TestFixture]
	public class XdccList : AParser
	{
		[Test]
		public void XdccListParseTest()
		{
			TestParse("** Download Liste der Pakete: \"/MSG [XG]TestBot XDCC LIST\" **", "XDCC LIST");
			TestParse("** Download Liste der Pakete: \"/MSG [XG]TestBot XDCC LIST ALL\" **", "XDCC LIST ALL");
			TestParse("group: TOKINO - Toki no Tabibito: Time Stranger", "XDCC LIST TOKINO");
			TestParse("group: maji[720p] - Maji de Watashi ni Koi Shinasai[720p]", "XDCC LIST maji[720p]");
			TestParse("** Download Liste der Pakete: \"/MSG [XG]TestBot XDCC SEND LIST\" **", "XDCC SEND LIST");
			TestParse("** Per richiedere la lista: \"/MSG XXBOTNAME XDCC LIST\" **", "XDCC LIST");
		}

		void TestParse(string aMessage, string aExpectedCommand)
		{
			var parser = new XG.Plugin.Irc.Parser.Types.XdccList();
			EventArgs<Channel, string, string> raisedEvent = null;
			parser.OnXdccList += (sender, e) => raisedEvent = e;

			Parse(parser, aMessage);
			Assert.AreEqual(Channel, raisedEvent.Value1);
			Assert.AreEqual(Bot.Name, raisedEvent.Value2);
			Assert.AreEqual(aExpectedCommand, raisedEvent.Value3);
		}
	}
}
