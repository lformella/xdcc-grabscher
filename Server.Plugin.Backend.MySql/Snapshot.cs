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
		[MySqlAttribute]
		Int64 Timestamp { get; set; }
		[MySqlAttribute]
		Int64 Speed { get; set; }
		[MySqlAttribute]
		Int64 Servers { get; set; }
		[MySqlAttribute]
		Int64 ServersEnabled { get; set; }
		[MySqlAttribute]
		Int64 ServersDisabled { get; set; }
		[MySqlAttribute]
		Int64 ServersConnected { get; set; }
		[MySqlAttribute]
		Int64 ServersDisconnected { get; set; }
		[MySqlAttribute]
		Int64 Channels { get; set; }
		[MySqlAttribute]
		Int64 ChannelsEnabled { get; set; }
		[MySqlAttribute]
		Int64 ChannelsDisabled { get; set; }
		[MySqlAttribute]
		Int64 ChannelsConnected { get; set; }
		[MySqlAttribute]
		Int64 ChannelsDisconnected { get; set; }
		[MySqlAttribute]
		Int64 Bots { get; set; }
		[MySqlAttribute]
		Int64 BotsConnected { get; set; }
		[MySqlAttribute]
		Int64 BotsDisconnected { get; set; }
		[MySqlAttribute]
		Int64 BotsFreeSlots { get; set; }
		[MySqlAttribute]
		Int64 BotsFreeQueue { get; set; }
		[MySqlAttribute]
		Int64 BotsAverageCurrentSpeed { get; set; }
		[MySqlAttribute]
		Int64 BotsAverageMaxSpeed { get; set; }
		[MySqlAttribute]
		Int64 Packets { get; set; }
		[MySqlAttribute]
		Int64 PacketsConnected { get; set; }
		[MySqlAttribute]
		Int64 PacketsDisconnected { get; set; }
		[MySqlAttribute]
		Int64 PacketsSize { get; set; }
		[MySqlAttribute]
		Int64 PacketsSizeConnected { get; set; }
		[MySqlAttribute]
		Int64 PacketsSizeDisconnected { get; set; }
	}
}

