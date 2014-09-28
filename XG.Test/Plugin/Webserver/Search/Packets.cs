// 
//  AParser.cs
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
using System.Linq;
using XG.Model.Domain;
using NUnit.Framework;

namespace XG.Test.Plugin.Webserver.Search
{
	[TestFixture]
	public class Packets
	{
		Bot bot1 = new Bot
		{
			Name = "[XG]TestBot1",
			Connected = true
		};
		Bot bot2 = new Bot
		{
			Name = "[XG]TestBot2",
			Connected = true
		};

		public Packets ()
		{
			var servers = new Servers();

			var server = new Server
			{
				Name = "test.bitpir.at"
			};
			servers.Add(server);

			var channel = new Channel
			{
				Name = "#test"
			};
			server.AddChannel(channel);

			channel.AddBot(bot1);
			channel.AddBot(bot2);

			bot1.AddPacket(CreatePacket(1, "Under.the.Dome.s01e01.mkv", 101));
			bot1.AddPacket(CreatePacket(2, "Under.the.Dome.s01e02.mkv", 102));
			bot1.AddPacket(CreatePacket(3, "Under.the.Dome.s01e03.mkv", 103));
			bot1.AddPacket(CreatePacket(4, "Under.the.Dome.s01e04.mkv", 104));

			bot2.AddPacket(CreatePacket(1, "Under.the.Dome.s01e01.mkv", 201));
			bot2.AddPacket(CreatePacket(2, "Under.the.Dome.s01e02.mkv", 202));
			bot2.AddPacket(CreatePacket(3, "Under.the.Dome.s01e03.mkv", 203));
			bot2.AddPacket(CreatePacket(4, "Under.the.Dome.s01e04.mkv", 204));
			bot2.AddPacket(CreatePacket(5, "Ander.the.Dome.s01e05.mkv", 205));

			XG.Plugin.Webserver.Search.Packets.Servers = servers;
			XG.Plugin.Webserver.Search.Packets.Initialize();
		}

		Packet CreatePacket(int aId, string aName, int aSize)
		{
			return new Packet
			{
				Name = aName,
				Id = aId,
				Enabled = true,
				Size = aSize
			};
		}

		XG.Plugin.Webserver.Search.Results GetResults(string aTerm, Int64 aSize, bool includeOffline, int aStart, int aLimit)
		{
			return XG.Plugin.Webserver.Search.Packets.GetResults(new XG.Model.Domain.Search { Name = aTerm, Size = aSize }, includeOffline, aStart, aLimit, "Size", false);
		}

		[Test]
		public void SearchUpdateBotStatusTest()
		{
			var result = GetResults("under", 0, false, 0, 4);
			Assert.AreEqual(8, result.Total);

			bot2.Connected = false;
			bot2.Commit();

			result = GetResults("under", 0, false, 0, 4);
			Assert.AreEqual(4, result.Total);

			bot2.Connected = true;
			bot2.Commit();

			result = GetResults("under", 0, false, 0, 4);
			Assert.AreEqual(8, result.Total);
		}

		[Test]
		public void SearchSingleStringTest()
		{
			var result = GetResults("under", 0, true, 0, 4);
			Assert.AreEqual(8, result.Total);
			Assert.AreEqual(1, result.Packets.Count);
			Assert.AreEqual(4, result.Packets.First().Value.Count());
			Assert.AreEqual(101, result.Packets.First().Value.First().Size);
		}

		[Test]
		public void SearchMultipleStringTest()
		{
			var result = GetResults("under s01e04", 0, true, 0, 4);
			Assert.AreEqual(2, result.Total);
			Assert.AreEqual(1, result.Packets.Count);
			Assert.AreEqual(2, result.Packets.First().Value.Count());
			Assert.AreEqual(104, result.Packets.First().Value.First().Size);
		}

		[Test]
		public void SearchMultipleNotStringTest()
		{
			var result = GetResults("dome -under", 0, true, 0, 4);
			Assert.AreEqual(1, result.Total);
			Assert.AreEqual(1, result.Packets.Count);
			Assert.AreEqual(1, result.Packets.First().Value.Count());
			Assert.AreEqual(205, result.Packets.First().Value.First().Size);
		}

		[Test]
		public void SearchByGroupTest()
		{
			var result = GetResults("under s01e**", 0, true, 0, 4);
			Assert.AreEqual(8, result.Total);
			Assert.AreEqual(2, result.Packets.Count);
			Assert.AreEqual(2, result.Packets.First().Value.Count());
			Assert.AreEqual(101, result.Packets.First().Value.First().Size);

			result = GetResults("under s01e**", 0, true, 1, 4);
			Assert.AreEqual(8, result.Total);
			Assert.AreEqual(3, result.Packets.Count);
			Assert.AreEqual(1, result.Packets.First().Value.Count());
			Assert.AreEqual(201, result.Packets.First().Value.First().Size);
		}

		[Test]
		public void SearchByDoubleGroupTest()
		{
			var result = GetResults("under s**e**", 0, true, 0, 4);
			Assert.AreEqual(8, result.Total);
			Assert.AreEqual(2, result.Packets.Count);
			Assert.AreEqual(2, result.Packets.First().Value.Count());
			Assert.AreEqual(101, result.Packets.First().Value.First().Size);
			Assert.AreEqual(2, result.Packets.Last().Value.Count());
			Assert.AreEqual(202, result.Packets.Last().Value.Last().Size);

			result = GetResults("under s**e**", 0, true, 3, 1);
			Assert.AreEqual(8, result.Total);
			Assert.AreEqual(1, result.Packets.Count);
			Assert.AreEqual(1, result.Packets.First().Value.Count());
			Assert.AreEqual(202, result.Packets.First().Value.First().Size);

			result = GetResults("under s**e**", 0, true, 3, 4);
			Assert.AreEqual(8, result.Total);
			Assert.AreEqual(3, result.Packets.Count);
			Assert.AreEqual(1, result.Packets.First().Value.Count());
			Assert.AreEqual(202, result.Packets.First().Value.First().Size);
			Assert.AreEqual(1, result.Packets.Last().Value.Count());
			Assert.AreEqual(104, result.Packets.Last().Value.Last().Size);
		}

		[Test]
		public void SearchMinSizeTest()
		{
			var result = GetResults("under", 202, true, 0, 4);
			Assert.AreEqual(2, result.Total);
			Assert.AreEqual(1, result.Packets.Count);
			Assert.AreEqual(2, result.Packets.First().Value.Count());
			Assert.AreEqual(203, result.Packets.First().Value.First().Size);

			result = GetResults("under", 200, true, 0, 4);
			Assert.AreEqual(4, result.Total);
			Assert.AreEqual(1, result.Packets.Count);
			Assert.AreEqual(4, result.Packets.First().Value.Count());
			Assert.AreEqual(201, result.Packets.First().Value.First().Size);
		}
	}
}
