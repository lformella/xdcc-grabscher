// 
//  ServerPluginGeneralWebserver.cs
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

using NUnit.Framework;

using XG.Core;
using XG.Server.Plugin.General.Webserver;

namespace Test
{
	[TestFixture()]
	public class ServerPluginGeneralWebserver
	{
		public ServerPluginGeneralWebserver ()
		{
		}

		[Test()]
		public void JsonTest()
		{
			var part = new FilePart
			{
				Connected = true,
				Enabled = true,
				Guid = Guid.Empty
			};

			Assert.AreEqual("{\"Checked\":false,\"Connected\":true,\"CurrentSize\":0,\"Enabled\":true,\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"MissingSize\":0,\"Name\":\"\",\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Speed\":0,\"StartSize\":0,\"State\":0,\"StopSize\":0,\"TimeMissing\":9223372036854775807}", Json.Serialize<FilePart>(part));

			var packet = new Packet
			{
				Name = "Test Packet",
				Connected = true,
				Enabled = true,
				Guid = Guid.Empty,
				LastUpdated = new DateTime(2012, 08, 05, 03, 45, 44),
				Part = part
			};

			Assert.AreEqual("{\"Connected\":true,\"Enabled\":true,\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Id\":-1,\"LastMentioned\":\"\\/Date(-62135596800000)\\/\",\"LastUpdated\":\"\\/Date(1344138344000)\\/\",\"Name\":\"Test Packet\",\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Part\":{\"Checked\":false,\"Connected\":true,\"CurrentSize\":0,\"Enabled\":true,\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"MissingSize\":0,\"Name\":\"\",\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Speed\":0,\"StartSize\":0,\"State\":0,\"StopSize\":0,\"TimeMissing\":9223372036854775807},\"RealName\":\"\",\"RealSize\":0,\"Size\":0}", Json.Serialize<Packet>(packet));

			var bot = new Bot
			{
				Name = "Test Bot",
				Connected = true,
				Enabled = true,
				Guid = Guid.Empty,
				InfoQueueCurrent = 16,
				InfoQueueTotal = 16,
				InfoSlotCurrent = 16,
				InfoSlotTotal = 16,
				InfoSpeedCurrent = 16,
				InfoSpeedMax = 16,
				LastMessage = "Test Message",
				QueuePosition = 16,
				QueueTime = 16,
				State = BotState.Idle,
				LastContact = new DateTime(2012, 08, 05, 03, 45, 44)
			};
			bot.AddPacket(packet);

			Assert.AreEqual("{\"Connected\":true,\"Enabled\":true,\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"InfoQueueCurrent\":16,\"InfoQueueTotal\":16,\"InfoSlotCurrent\":16,\"InfoSlotTotal\":16,\"InfoSpeedCurrent\":16,\"InfoSpeedMax\":16,\"LastContact\":\"\\/Date(1344138344000)\\/\",\"LastMessage\":\"Test Message\",\"Name\":\"Test Bot\",\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"QueuePosition\":16,\"QueueTime\":16,\"Speed\":0,\"State\":0}", Json.Serialize<Bot>(bot));
		}
	}
}

