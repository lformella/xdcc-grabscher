//
//  Bandwitdh.cs
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
using NUnit.Framework;

namespace XG.Test.Plugin.Irc.Parser.Types.Info
{
	[TestFixture]
	public class Bandwitdh : AParser
	{
		[Test]
		public void BandwitdhParseTest()
		{
			var parser = new XG.Plugin.Irc.Parser.Types.Info.Bandwitdh();

			Parse(parser, @"** Bandwidth Usage ** Current: 12.7kB/s, Record: 139.5kB/s");
			Assert.AreEqual((Int64) (12.7 * 1024), Bot.InfoSpeedCurrent);
			Assert.AreEqual((Int64) (139.5 * 1024), Bot.InfoSpeedMax);
			
			Parse(parser, @"** Bandwidth Usage ** Current: 0.0KB/s, Record: 231.4KB/s");
			Assert.AreEqual(0, Bot.InfoSpeedCurrent);
			Assert.AreEqual((Int64) (231.4 * 1024), Bot.InfoSpeedMax);
		}
	}
}
