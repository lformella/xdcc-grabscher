// 
//  Objects.cs
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
using NUnit.Framework;
using XG.Model.Domain;

namespace XG.Test.Business.Helper
{
	[TestFixture]
	public class Objects
	{
		const int count = 10;
		const int fakeId = 5;

		[Test]
		public void CheckAndRemoveDuplicatesTest()
		{
			var servers = new Servers();
			for (int a = 1; a <= count; a++)
			{
				servers.Add(createServer("server " + a));
			}
			servers.Named("server " + count).Name = "server " + fakeId;

			Assert.AreEqual(count, servers.All.Count());
			Assert.AreEqual(count * count, (from server in servers.All from channel in server.Channels select channel).Count());
			Assert.AreEqual(count * count * count, (from server in servers.All from channel in server.Channels from bot in channel.Bots select bot).Count());
			Assert.AreEqual(count * count * count * count, (from server in servers.All from channel in server.Channels from bot in channel.Bots from packet in bot.Packets select packet).Count());

			XG.Business.Helper.Objects.CheckAndRemoveDuplicates(servers);

			int newCount = count - 1;
			Assert.AreEqual(newCount, servers.All.Count());
			Assert.AreEqual(newCount * newCount, (from server in servers.All from channel in server.Channels select channel).Count());
			Assert.AreEqual(newCount * newCount * newCount, (from server in servers.All from channel in server.Channels from bot in channel.Bots select bot).Count());
			Assert.AreEqual(newCount * newCount * newCount * newCount, (from server in servers.All from channel in server.Channels from bot in channel.Bots from packet in bot.Packets select packet).Count());
		}

		Server createServer(String aName)
		{
			var server = new Server { Name = aName };
			for (int a = 1; a <= count; a++)
			{
				server.AddChannel(createChannel("channel " + a));
			}
			server.Named("channel " + count).Name = "channel " + fakeId;
			return server;
		}

		Channel createChannel(String aName)
		{
			var channel = new Channel { Name = aName };
			for (int a = 1; a <= count; a++)
			{
				channel.AddBot(createBot("bot " + a));
			}
			channel.Named("bot " + count).Name = "bot " + fakeId;
			return channel;
		}

		Bot createBot(String aName)
		{
			var bot = new Bot { Name = aName };
			for (int a = 1; a <= count; a++)
			{
				bot.AddPacket(createPacket(a));
			}
			bot.Packet(count).Id = fakeId;
			return bot;
		}

		Packet createPacket(int aId)
		{
			var packet = new Packet { Id = aId };
			return packet;
		}
	}
}
