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
using System.IO;
using System.Linq;

using XG.Core;

using SharpRobin.Core;

namespace XG.Server.Worker
{
	public class RrdWorker : ALoopWorker
	{
		public RrdDb RrdDb { get; set; }

		#region AWorker

		protected override void LoopRun()
		{
			Core.Server[] servers = (from server in Servers.All select server).ToArray();
			Channel[] channels = (from server in servers from channel in server.Channels select channel).ToArray();
			Bot[] bots = (from channel in channels from bot in channel.Bots select bot).ToArray();
			Packet[] packets = (from bot in bots from packet in bot.Packets select packet).ToArray();

			Sample sample = RrdDb.createSample(DateTime.Now.ToTimestamp());

			sample.setValue((int)SnapshotValue.Speed + "", (from file in Files.All from part in file.Parts select part.Speed).Sum());

			sample.setValue((int)SnapshotValue.Servers + "", (from server in servers select server).Count());
			sample.setValue((int)SnapshotValue.ServersEnabled + "", (from server in servers where server.Enabled select server).Count());
			sample.setValue((int)SnapshotValue.ServersDisabled + "", (from server in servers where !server.Enabled select server).Count());
			sample.setValue((int)SnapshotValue.ServersConnected + "", (from server in servers where server.Connected select server).Count());
			sample.setValue((int)SnapshotValue.ServersDisconnected + "", (from server in servers where !server.Connected select server).Count());

			sample.setValue((int)SnapshotValue.Channels + "", (from channel in channels select channel).Count());
			sample.setValue((int)SnapshotValue.ChannelsEnabled + "", (from channel in channels where channel.Enabled select channel).Count());
			sample.setValue((int)SnapshotValue.ChannelsDisabled + "", (from channel in channels where !channel.Enabled select channel).Count());
			sample.setValue((int)SnapshotValue.ChannelsConnected + "", (from channel in channels where channel.Connected select channel).Count());
			sample.setValue((int)SnapshotValue.ChannelsDisconnected + "", (from channel in channels where !channel.Connected select channel).Count());

			sample.setValue((int)SnapshotValue.Bots + "", (from bot in bots select bot).Count());
			sample.setValue((int)SnapshotValue.BotsConnected + "", (from bot in bots where bot.Connected select bot).Count());
			sample.setValue((int)SnapshotValue.BotsDisconnected + "", (from bot in bots where !bot.Connected select bot).Count());
			sample.setValue((int)SnapshotValue.BotsFreeSlots + "", (from bot in bots where bot.InfoSlotCurrent > 0 select bot).Count());
			sample.setValue((int)SnapshotValue.BotsFreeQueue + "", (from bot in bots where bot.InfoQueueCurrent > 0 select bot).Count());
			try
			{
				sample.setValue((int)SnapshotValue.BotsAverageCurrentSpeed + "",
				         ((from bot in bots select bot.InfoSpeedCurrent).Sum() / (from bot in bots where bot.InfoSpeedCurrent > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				sample.setValue((int)SnapshotValue.BotsAverageCurrentSpeed + "", 0);
			}
			try
			{
				sample.setValue((int)SnapshotValue.BotsAverageMaxSpeed + "",
				         ((from bot in bots select bot.InfoSpeedMax).Sum() / (from bot in bots where bot.InfoSpeedMax > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				sample.setValue((int)SnapshotValue.BotsAverageMaxSpeed + "", 0);
			}

			sample.setValue((int)SnapshotValue.Packets + "", (from packet in packets select packet).Count());
			sample.setValue((int)SnapshotValue.PacketsConnected + "", (from packet in packets where packet.Connected select packet).Count());
			sample.setValue((int)SnapshotValue.PacketsDisconnected + "", (from packet in packets where !packet.Connected select packet).Count());
			sample.setValue((int)SnapshotValue.PacketsSize + "", (from packet in packets select packet.Size).Sum());
			sample.setValue((int)SnapshotValue.PacketsSizeDownloading + "", (from packet in packets where packet.Connected select packet.Size).Sum());
			sample.setValue((int)SnapshotValue.PacketsSizeNotDownloading + "", (from packet in packets where !packet.Connected select packet.Size).Sum());
			sample.setValue((int)SnapshotValue.PacketsSizeConnected + "", (from packet in packets where packet.Parent.Connected select packet.Size).Sum());
			sample.setValue((int)SnapshotValue.PacketsSizeDisconnected + "", (from packet in packets where !packet.Parent.Connected select packet.Size).Sum());

			sample.update();
		}

		#endregion
	}
}
