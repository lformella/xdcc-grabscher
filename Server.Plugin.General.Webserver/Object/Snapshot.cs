// 
//  Snapshot.cs
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

using Newtonsoft.Json;

namespace XG.Server.Plugin.General.Webserver.Object
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Snapshot
	{
		public Int64 Timestamp { get; set; }

		public Int64 Speed { get; set; }

		public Int64 Servers { get; set; }

		public Int64 ServersEnabled { get; set; }

		public Int64 ServersDisabled { get; set; }

		public Int64 ServersConnected { get; set; }

		public Int64 ServersDisconnected { get; set; }

		public Int64 Channels { get; set; }

		public Int64 ChannelsEnabled { get; set; }

		public Int64 ChannelsDisabled { get; set; }

		public Int64 ChannelsConnected { get; set; }

		public Int64 ChannelsDisconnected { get; set; }

		public Int64 Bots { get; set; }

		public Int64 BotsConnected { get; set; }

		public Int64 BotsDisconnected { get; set; }

		public Int64 BotsFreeSlots { get; set; }

		public Int64 BotsFreeQueue { get; set; }

		public Int64 BotsAverageCurrentSpeed { get; set; }

		public Int64 BotsAverageMaxSpeed { get; set; }

		public Int64 Packets { get; set; }

		public Int64 PacketsConnected { get; set; }

		public Int64 PacketsDisconnected { get; set; }

		public Int64 PacketsSize { get; set; }

		public Int64 PacketsSizeDownloading { get; set; }

		public Int64 PacketsSizeNotDownloading { get; set; }

		public Int64 PacketsSizeConnected { get; set; }

		public Int64 PacketsSizeDisconnected { get; set; }
	}
}
