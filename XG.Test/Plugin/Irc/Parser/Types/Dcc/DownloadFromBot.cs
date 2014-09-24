//
//  DownloadFromBot.cs
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
using System.Net;
using NUnit.Framework;
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Test.Plugin.Irc.Parser.Types.Dcc
{
	[TestFixture]
	public class DownloadFromBot : AParser
	{
		[Test]
		public void DccDownloadTest()
		{
			var parser = new XG.Plugin.Irc.Parser.Types.Dcc.DownloadFromBot();
			EventArgs<Packet, Int64, IPAddress, int> raisedEvent = null;
			parser.OnAddDownload += (sender, e) => raisedEvent = e;

			raisedEvent = null;
			Parse(parser, "\u0001DCC SEND Testfile.with.a.long.name.mkv 1203194610 45000 975304559\u0001");

			Assert.AreEqual(0, raisedEvent.Value2);
			Assert.AreEqual("71.183.74.242", raisedEvent.Value3.ToString());
			Assert.AreEqual(45000, raisedEvent.Value4);
		}
	}
}
