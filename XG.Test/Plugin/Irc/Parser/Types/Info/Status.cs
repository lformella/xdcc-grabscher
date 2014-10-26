//
//  Packet.cs
// This file is part of XG - XDCC Grabscher
// http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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

namespace XG.Test.Plugin.Irc.Parser.Types.Info
{
	[TestFixture]
	public class Status : AParser
	{
		[Test]
		public void StatusParseTest()
		{
			var parser = new XG.Plugin.Irc.Parser.Types.Info.Status();

			Parse(parser, "** 9 packs **  1 of 1 slot open, Min: 5.0kB/s, Record: 59.3kB/s");
			Assert.AreEqual(1, Bot.InfoSlotCurrent);
			Assert.AreEqual(1, Bot.InfoSlotTotal);

			Parse(parser, "-> 1 Pack <-  10 Of 10 Slots Open Min: 15.0KB/s Record: 691.8KB/s");
			Assert.AreEqual(10, Bot.InfoSlotCurrent);
			Assert.AreEqual(10, Bot.InfoSlotTotal);

			Parse(parser, "**[EWG]*   packs **  12 of 12 slots open, Record: 1736.8kB/s");
			Assert.AreEqual(12, Bot.InfoSlotCurrent);
			Assert.AreEqual(12, Bot.InfoSlotTotal);

			Parse(parser, "-> 18 PackS <-  13 Of 15 Slots Open Min: 15.0KB/s Record: 99902.4KB/s");
			Assert.AreEqual(13, Bot.InfoSlotCurrent);
			Assert.AreEqual(15, Bot.InfoSlotTotal);
		}
	}
}
