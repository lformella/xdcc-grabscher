// 
//  NHibernate.cs
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

using NUnit.Framework;
using XG.Model.Domain;
using System;
using System.Linq;

namespace XG.Test.DB
{
	[TestFixture]
	class Dao
	{
		const int Count = 3;
		Random random = new Random();

		[Test]
		public void DaoTest()
		{
			using(var dao = new XG.DB.Dao())
			{
				dao.Servers();
			}
		}
			
		[Test]
		public void DaoServerTest()
		{
			using (var dao = new XG.DB.Dao())
			{
				var servers = dao.Servers();

				for (int a = 0; a < Count; a++)
				{
					var server = new XG.Model.Domain.Server
					{
						Name = "irc.test.com" + a,
						Port = 6666 + a
					};

					for (int b = 0; b < Count; b++)
					{
						var channel = new Channel
						{
							Name = "#test" + a + "-" + b
						};

						for (int c = 0; c < Count; c++)
						{
							var bot = new Bot
							{
								Name = "Bot" + a + "-" + b + "-" + c
							};

							for (int d = 0; d < Count; d++)
							{
								var packet = new Packet
								{
									Name = "Pack" + a + "-" + b + "-" + c + "-" + d,
									Id = d
								};
								bot.AddPacket(packet);
							}

							channel.AddBot(bot);
						}

						server.AddChannel(channel);
					}

					servers.Add(server);

					var channelToDrop = server.Channels.First();
					server.RemoveChannel(channelToDrop);
				}
			}
		}

		[Test]
		public void DaoFileTest()
		{
			using (var dao = new XG.DB.Dao())
			{
				var files = dao.Files();

				for (int a = 0; a < Count; a++)
				{
					var file = new File("test" + a, 1000 * (a + 1));

					for (int b = 0; b < Count; b++)
					{
						var part = new FilePart
						{
							StartSize = 100 * (b + 1),
							StopSize = 200 * (b + 1)
						};
						file.Add(part);
					}

					files.Add(file);
				}
			}
		}

		[Test]
		public void DaoSearchTest()
		{
			using (var dao = new XG.DB.Dao())
			{
				var searches = dao.Searches();

				for (int a = 0; a < Count; a++)
				{
					var search = new Search
					{
						Name = "test" + random.Next(1000, 9999)
					};

					searches.Add(search);

					search.Name = "search test " + a;
				}
			}
		}
	}
}
