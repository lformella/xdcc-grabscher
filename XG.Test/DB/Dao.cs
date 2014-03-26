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
		const int Count = 6;
		Random random = new Random();

		[Test]
		[Ignore]
		public void DaoLoadObjectsTest()
		{
			using(var dao = new XG.DB.Dao())
			{
				var servers = dao.Servers;
			}
		}
			
		[Test]
		[Ignore]
		public void DaoWriteObjectsTest()
		{
			using (var dao = new XG.DB.Dao())
			{
				var files = dao.Files;
				for (int a = 0; a < Count; a++)
				{
					var file = new File("test" + a, 1000000 * (a + 1));
					file.CurrentSize = 700000 * (a + 1);
					files.Add(file);
				}

				var servers = dao.Servers;
				for (int a = 1; a < Count; a++)
				{
					var server = new Server
					{
						Connected = random.Next(1, 3) == 1,
						Name = "irc.test.com" + a,
						Port = 6666 + a
					};

					for (int b = 1; b < Count; b++)
					{
						var channel = new Channel
						{
							Connected = random.Next(1, 3) == 1,
							Name = "#test" + a + "-" + b
						};

						for (int c = 1; c < Count; c++)
						{
							var bot = new Bot
							{
								Name = "Bot " + a + "-" + b + "-" + c,
								InfoSpeedCurrent = random.Next(100000, 1000000),
								InfoSpeedMax = random.Next(1000000, 10000000),
								InfoSlotCurrent = random.Next(1, 10),
								InfoSlotTotal = random.Next(10, 100),
								InfoQueueCurrent = random.Next(1, 10),
								InfoQueueTotal = random.Next(10, 1000),
								HasNetworkProblems = random.Next(1, 10) > 7,
								LastMessage = "This is a test message that should be long enough for the most of the table and cell width test cases which are there for testing purposes.",
								LastMessageTime = DateTime.Now.AddMinutes(random.Next(10000, 100000))
							};

							int rand = random.Next(1, 4);
							if (rand == 1)
							{
								bot.Connected = true;
								bot.State = Bot.States.Active;
								bot.HasNetworkProblems = false;
							}
							else if (rand == 2)
							{
								bot.Connected = random.Next(1, 3) == 1;
								bot.State = Bot.States.Idle;
							}
							else if (rand == 3)
							{
								bot.Connected = true;
								bot.State = Bot.States.Waiting;
								bot.QueueTime = random.Next(10, 100);
								bot.QueuePosition = random.Next(1, 10);
							}

							for (int d = 1; d < Count; d++)
							{
								var ending = new string[]{"rar", "mkv", "mp3", "tar", "avi", "wav", "jpg", "bmp"};

								var packet = new Packet
								{
									Name = "Pack " + a + "-" + b + "-" + c + "-" + d + "." + ending[random.Next(0, ending.Length)],
									Id = a + b + c + d,
									Size = random.Next(1000000, 10000000),
									LastUpdated = DateTime.Now.AddDays(random.Next(1, 10) * -1),
									LastMentioned = DateTime.Now.AddDays(random.Next(10, 100) * -1)
								};

								if (d == 1)
								{
									if (bot.State == Bot.States.Active)
									{
										packet.Enabled = true;
										packet.Connected = true;
									}
									else if (bot.State == Bot.States.Waiting)
									{
										packet.Enabled = true;
									}
								}
								else if (d == 2)
								{
									if (bot.State == Bot.States.Waiting)
									{
										packet.Enabled = true;
									}
								}

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

				var searches = dao.Searches;
				for (int a = 0; a < Count; a++)
				{
					var search = new Search
					{
						Name = "test" + random.Next(1000, 10000)
					};

					searches.Add(search);

					search.Name = "Pack " + a;
				}
			}
		}
	}
}
