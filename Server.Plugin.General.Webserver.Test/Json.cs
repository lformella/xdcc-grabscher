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
		TimeZone _zone = TimeZone.CurrentTimeZone;

		string JsonDate(DateTime dt)
		{
			DateTime d1 = new DateTime(1970, 1, 1);
			DateTime d2 = dt.ToUniversalTime();
			TimeSpan ts = new TimeSpan(d2.Ticks - d1.Ticks);

			var hours = _zone.GetUtcOffset(DateTime.Now).Hours;
			var str = hours < 10 && hours > -10 ? "0" + hours : "" + hours;
			str = hours > 0 ? "+" + str + "00" : (hours < 0 ? "-" + str + "00" : "");
			return @"\/Date(" + (Int64)ts.TotalMilliseconds + str + @")\/";
		}

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
				"{\"StartSize\":0,\"StopSize\":0,\"CurrentSize\":0,\"MissingSize\":0,\"TimeMissing\":0,\"Speed\":0,\"State\":0,\"Checked\":false,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"\",\"Connected\":true,\"Enabled\":true}",
				JsonConvert.SerializeObject(part, jsonSerializerSettings)
			);

			var packet = new Packet
			{
				Name = "Test Packet",
				Connected = true,
				Enabled = true,
				Guid = Guid.Empty,
				LastMentioned = DateTime.Now,
				LastUpdated = DateTime.Now,
				Part = part
			};

			Assert.AreEqual(
				"{\"Part\":{\"StartSize\":0,\"StopSize\":0,\"CurrentSize\":0,\"MissingSize\":0,\"TimeMissing\":0,\"Speed\":0,\"State\":0,\"Checked\":false,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"\",\"Connected\":true,\"Enabled\":true},\"Name\":\"Test Packet\",\"Id\":-1,\"Size\":0,\"RealSize\":0,\"RealName\":\"\",\"LastUpdated\":\"" + JsonDate(packet.LastUpdated) + "\",\"LastMentioned\":\"" + JsonDate(packet.LastMentioned) + "\",\"Next\":false,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Connected\":true,\"Enabled\":true}",
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
				LastContact = DateTime.Now
			};
			bot.AddPacket(packet);

			Assert.AreEqual(
				"{\"State\":0,\"LastMessage\":\"Test Message\",\"LastMessageTime\":\"" + JsonDate(bot.LastMessageTime) + "\",\"LastContact\":\"" + JsonDate(bot.LastContact) + "\",\"QueuePosition\":16,\"QueueTime\":16,\"InfoSpeedMax\":16,\"InfoSpeedCurrent\":16,\"InfoSlotTotal\":16,\"InfoSlotCurrent\":16,\"InfoQueueTotal\":16,\"InfoQueueCurrent\":16,\"Speed\":0,\"ParentGuid\":\"00000000-0000-0000-0000-000000000000\",\"Guid\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"Test Bot\",\"Connected\":true,\"Enabled\":true}",
				JsonConvert.SerializeObject(bot, jsonSerializerSettings)
			);
		}
	}
}
