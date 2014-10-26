// 
//  Snapshots.cs
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
using XG.Extensions;
using XG.Model.Domain;
using XG.Business.Model;

namespace XG.Business.Helper
{
	public static class Snapshots
	{
		public static Servers Servers { get; set; }
		public static Files Files { get; set; }

		public static Snapshot GenerateSnapshot()
		{
			Server[] servers = (from server in Servers.All select server).ToArray();
			Channel[] channels = (from server in servers from channel in server.Channels select channel).ToArray();
			Bot[] bots = (from channel in channels from bot in channel.Bots select bot).ToArray();
			Packet[] packets = (from bot in bots from packet in bot.Packets select packet).ToArray();
			File[] files = (from file in Files.All select file).ToArray();

			var snapshot = new Snapshot();

			snapshot.Set(SnapshotValue.Timestamp, DateTime.Now.ToTimestamp());

			snapshot.Set(SnapshotValue.Speed, (from file in files select file.Speed).Sum());

			snapshot.Set(SnapshotValue.Servers, (from server in servers select server).Count());
			snapshot.Set(SnapshotValue.ServersEnabled, (from server in servers where server.Enabled select server).Count());
			snapshot.Set(SnapshotValue.ServersDisabled, (from server in servers where !server.Enabled select server).Count());
			snapshot.Set(SnapshotValue.ServersConnected, (from server in servers where server.Connected select server).Count());
			snapshot.Set(SnapshotValue.ServersDisconnected, (from server in servers where !server.Connected select server).Count());

			snapshot.Set(SnapshotValue.Channels, (from channel in channels select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsEnabled, (from channel in channels where channel.Parent.Enabled && channel.Enabled select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsDisabled, (from channel in channels where !channel.Parent.Enabled || !channel.Enabled select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsConnected, (from channel in channels where channel.Connected select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsDisconnected, (from channel in channels where !channel.Connected select channel).Count());

			snapshot.Set(SnapshotValue.Bots, (from bot in bots select bot).Count());
			snapshot.Set(SnapshotValue.BotsConnected, (from bot in bots where bot.Connected select bot).Count());
			snapshot.Set(SnapshotValue.BotsDisconnected, (from bot in bots where !bot.Connected select bot).Count());
			snapshot.Set(SnapshotValue.BotsFreeSlots, (from bot in bots where bot.InfoSlotCurrent > 0 select bot).Count());
			snapshot.Set(SnapshotValue.BotsFreeQueue, (from bot in bots where bot.InfoQueueCurrent > 0 select bot).Count());
			try
			{
				snapshot.Set(SnapshotValue.BotsAverageCurrentSpeed, ((from bot in bots select bot.InfoSpeedCurrent).Sum() / (from bot in bots where bot.InfoSpeedCurrent > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				snapshot.Set(SnapshotValue.BotsAverageCurrentSpeed, 0);
			}
			try
			{
				snapshot.Set(SnapshotValue.BotsAverageMaxSpeed, ((from bot in bots select bot.InfoSpeedMax).Sum() / (from bot in bots where bot.InfoSpeedMax > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				snapshot.Set(SnapshotValue.BotsAverageMaxSpeed, 0);
			}

			snapshot.Set(SnapshotValue.Packets, (from packet in packets select packet).Count());
			snapshot.Set(SnapshotValue.PacketsConnected, (from packet in packets where packet.Connected select packet).Count());
			snapshot.Set(SnapshotValue.PacketsDisconnected, (from packet in packets where !packet.Connected select packet).Count());
			snapshot.Set(SnapshotValue.PacketsSize, (from packet in packets select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeDownloading, (from packet in packets where packet.Connected select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeNotDownloading, (from packet in packets where !packet.Connected select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeConnected, (from packet in packets where packet.Parent.Connected select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeDisconnected, (from packet in packets where !packet.Parent.Connected select packet.Size).Sum());

			snapshot.Set(SnapshotValue.FileSizeDownloaded, (from file in files select file.CurrentSize).Sum());
			snapshot.Set(SnapshotValue.FileSizeMissing, (from file in files select file.MissingSize).Sum());
			try
			{
				snapshot.Set(SnapshotValue.FileTimeMissing, (from file in files select file.TimeMissing).Max());
			}
			catch (Exception)
			{
				snapshot.Set(SnapshotValue.FileTimeMissing, 0);
			}

			return snapshot;
		}
	}
}
