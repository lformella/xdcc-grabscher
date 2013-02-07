// 
//  SnapshotWorker.cs
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

using XG.Core;

namespace XG.Server.Worker
{
	public class SnapshotWorker : ALoopWorker
	{
		#region AWorker

		protected override void LoopRun()
		{
			Core.Server[] servers = (from server in Servers.All select server).ToArray();
			Channel[] channels = (from server in servers from channel in server.Channels select channel).ToArray();
			Bot[] bots = (from channel in channels from bot in channel.Bots select bot).ToArray();
			Packet[] packets = (from bot in bots from packet in bot.Packets select packet).ToArray();

			var snap = new Snapshot();
			snap.Set(SnapshotValue.Timestamp, DateTime.Now.ToTimestamp());

			snap.Set(SnapshotValue.Speed, (from file in Files.All from part in file.Parts select part.Speed).Sum());

			snap.Set(SnapshotValue.Servers, (from server in servers select server).Count());
			snap.Set(SnapshotValue.ServersEnabled, (from server in servers where server.Enabled select server).Count());
			snap.Set(SnapshotValue.ServersDisabled, (from server in servers where !server.Enabled select server).Count());
			snap.Set(SnapshotValue.ServersConnected, (from server in servers where server.Connected select server).Count());
			snap.Set(SnapshotValue.ServersDisconnected, (from server in servers where !server.Connected select server).Count());

			snap.Set(SnapshotValue.Channels, (from channel in channels select channel).Count());
			snap.Set(SnapshotValue.ChannelsEnabled, (from channel in channels where channel.Enabled select channel).Count());
			snap.Set(SnapshotValue.ChannelsDisabled, (from channel in channels where !channel.Enabled select channel).Count());
			snap.Set(SnapshotValue.ChannelsConnected, (from channel in channels where channel.Connected select channel).Count());
			snap.Set(SnapshotValue.ChannelsDisconnected, (from channel in channels where !channel.Connected select channel).Count());

			snap.Set(SnapshotValue.Bots, (from bot in bots select bot).Count());
			snap.Set(SnapshotValue.BotsConnected, (from bot in bots where bot.Connected select bot).Count());
			snap.Set(SnapshotValue.BotsDisconnected, (from bot in bots where !bot.Connected select bot).Count());
			snap.Set(SnapshotValue.BotsFreeSlots, (from bot in bots where bot.InfoSlotCurrent > 0 select bot).Count());
			snap.Set(SnapshotValue.BotsFreeQueue, (from bot in bots where bot.InfoQueueCurrent > 0 select bot).Count());
			try
			{
				snap.Set(SnapshotValue.BotsAverageCurrentSpeed,
				         ((from bot in bots select bot.InfoSpeedCurrent).Sum() / (from bot in bots where bot.InfoSpeedCurrent > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				snap.Set(SnapshotValue.BotsAverageCurrentSpeed, 0);
			}
			try
			{
				snap.Set(SnapshotValue.BotsAverageMaxSpeed,
				         ((from bot in bots select bot.InfoSpeedMax).Sum() / (from bot in bots where bot.InfoSpeedMax > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				snap.Set(SnapshotValue.BotsAverageMaxSpeed, 0);
			}

			snap.Set(SnapshotValue.Packets, (from packet in packets select packet).Count());
			snap.Set(SnapshotValue.PacketsConnected, (from packet in packets where packet.Connected select packet).Count());
			snap.Set(SnapshotValue.PacketsDisconnected, (from packet in packets where !packet.Connected select packet).Count());
			snap.Set(SnapshotValue.PacketsSize, (from packet in packets select packet.Size).Sum());
			snap.Set(SnapshotValue.PacketsSizeDownloading, (from packet in packets where packet.Connected select packet.Size).Sum());
			snap.Set(SnapshotValue.PacketsSizeNotDownloading, (from packet in packets where !packet.Connected select packet.Size).Sum());
			snap.Set(SnapshotValue.PacketsSizeConnected, (from packet in packets where packet.Parent.Connected select packet.Size).Sum());
			snap.Set(SnapshotValue.PacketsSizeDisconnected, (from packet in packets where !packet.Parent.Connected select packet.Size).Sum());

			Snapshots.Add(snap);
		}

		#endregion
	}
}
