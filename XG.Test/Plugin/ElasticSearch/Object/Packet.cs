// 
//  Packet.cs
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

namespace XG.Test.Plugin.ElasticSearch.Object
{
	[TestFixture]
	public class Packet
	{
		[Test]
		public void IrcLinkTest()
		{
			var packet = new XG.Model.Domain.Packet
			{
				Id = 313,
				Name = "long.avi",
				Parent = new XG.Model.Domain.Bot
				{
					Name = "bot",
					Parent = new XG.Model.Domain.Channel
					{
						Name = "channel",
						Parent = new XG.Model.Domain.Server
						{
							Name = "server.net",
							Port = 666
						}
					}
				}
			};
			var packet2 = new XG.Plugin.ElasticSearch.Object.Packet
			{
				Object = packet
			};

			Assert.AreEqual("xdcc://server.net:666/server.net/channel/bot/#0313/long.avi/", packet2.IrcLink);

			packet.Id = 34567;
			packet.Parent.Parent.Parent.Port = 6667;
			Assert.AreEqual("xdcc://server.net/server.net/channel/bot/#34567/long.avi/", packet2.IrcLink);

			packet.Id = 3;
			Assert.AreEqual("xdcc://server.net/server.net/channel/bot/#0003/long.avi/", packet2.IrcLink);
		}
	}
}
