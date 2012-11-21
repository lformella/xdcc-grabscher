// 
//  Json.cs
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

using Newtonsoft.Json;

using NUnit.Framework;

using XG.Core;

namespace XG.Server.Plugin.General.Webserver.Test
{
	[TestFixture]
	public class Json
	{
		[Test]
		public void Serialize()
		{
			var jsonSerializerSettings = new JsonSerializerSettings
			{
				DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
				DateParseHandling = DateParseHandling.DateTime,
				DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
			};

			var part = new FilePart {Connected = true, Enabled = true, Guid = Guid.Empty};

			Assert.AreEqual(
				"{\"StartSize\":0,\"StopSize\":0,\"CurrentSize\":0,\"MissingSize\":0,\"TimeMissing\":9223372036854775807,\"Speed\":0,\"State\":0,\"Checked\":false,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"\",\"Connected\":true,\"Enabled\":true}",
				JsonConvert.SerializeObject(part, jsonSerializerSettings)
			);

			var packet = new Packet
			{
				Name = "Test Packet",
				Connected = true,
				Enabled = true,
				Guid = Guid.Empty,
				LastMentioned = new DateTime(2012, 08, 04, 03, 45, 44),
				LastUpdated = new DateTime(2012, 08, 05, 03, 45, 44),
				Part = part
			};

			Assert.AreEqual(
				"{\"Part\":{\"StartSize\":0,\"StopSize\":0,\"CurrentSize\":0,\"MissingSize\":0,\"TimeMissing\":9223372036854775807,\"Speed\":0,\"State\":0,\"Checked\":false,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"\",\"Connected\":true,\"Enabled\":true},\"Name\":\"Test Packet\",\"Id\":-1,\"Size\":0,\"RealSize\":0,\"RealName\":\"\",\"LastUpdated\":\"" + packet.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"LastMentioned\":\"" + packet.LastMentioned.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"Next\":false,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Connected\":true,\"Enabled\":true}",
				JsonConvert.SerializeObject(packet, jsonSerializerSettings)
			);

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
				State = Bot.States.Idle,
				LastContact = new DateTime(2012, 08, 05, 03, 45, 44)
			};
			bot.AddPacket(packet);

			Assert.AreEqual(
				"{\"State\":0,\"LastMessage\":\"Test Message\",\"LastMessageTime\":\"" + bot.LastMessageTime.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"LastContact\":\"" + bot.LastContact.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"QueuePosition\":16,\"QueueTime\":16,\"InfoSpeedMax\":16,\"InfoSpeedCurrent\":16,\"InfoSlotTotal\":16,\"InfoSlotCurrent\":16,\"InfoQueueTotal\":16,\"InfoQueueCurrent\":16,\"Speed\":0.0,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"Test Bot\",\"Connected\":true,\"Enabled\":true}",
				JsonConvert.SerializeObject(bot, jsonSerializerSettings)
			);
		}
	}
}
