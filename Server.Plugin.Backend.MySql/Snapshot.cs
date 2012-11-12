// 
//  Snapshot.cs
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

using XG.Core;

namespace XG.Server.Plugin.Backend.MySql
{
	public class Snapshot
	{
		[MySql]
		Int64 Timestamp { get; set; }

		[MySql]
		Int64 Speed { get; set; }

		[MySql]
		Int64 Servers { get; set; }

		[MySql]
		Int64 ServersEnabled { get; set; }

		[MySql]
		Int64 ServersDisabled { get; set; }

		[MySql]
		Int64 ServersConnected { get; set; }

		[MySql]
		Int64 ServersDisconnected { get; set; }

		[MySql]
		Int64 Channels { get; set; }

		[MySql]
		Int64 ChannelsEnabled { get; set; }

		[MySql]
		Int64 ChannelsDisabled { get; set; }

		[MySql]
		Int64 ChannelsConnected { get; set; }

		[MySql]
		Int64 ChannelsDisconnected { get; set; }

		[MySql]
		Int64 Bots { get; set; }

		[MySql]
		Int64 BotsConnected { get; set; }

		[MySql]
		Int64 BotsDisconnected { get; set; }

		[MySql]
		Int64 BotsFreeSlots { get; set; }

		[MySql]
		Int64 BotsFreeQueue { get; set; }

		[MySql]
		Int64 BotsAverageCurrentSpeed { get; set; }

		[MySql]
		Int64 BotsAverageMaxSpeed { get; set; }

		[MySql]
		Int64 Packets { get; set; }

		[MySql]
		Int64 PacketsConnected { get; set; }

		[MySql]
		Int64 PacketsDisconnected { get; set; }

		[MySql]
		Int64 PacketsSize { get; set; }

		[MySql]
		Int64 PacketsSizeDownloading { get; set; }

		[MySql]
		Int64 PacketsSizeNotDownloading { get; set; }

		[MySql]
		Int64 PacketsSizeConnected { get; set; }

		[MySql]
		Int64 PacketsSizeDisconnected { get; set; }
	}
}
