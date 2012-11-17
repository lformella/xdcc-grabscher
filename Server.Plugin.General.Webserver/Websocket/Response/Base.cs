//
//  Base.cs
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

using Newtonsoft.Json;

namespace XG.Server.Plugin.General.Webserver.Websocket.Response
{
	[JsonObject(MemberSerialization.OptIn)]
	public class Base
	{
		#region ENUMS

		public enum Types
		{
			None = 0,

			#region SINGLE

			ServerAdded = 1,
			ServerRemoved = 2,
			ServerChanged = 3,

			ChannelAdded = 4,
			ChannelRemoved = 5,
			ChannelChanged = 6,

			BotAdded = 7,
			BotRemoved = 8,
			BotChanged = 9,

			PacketAdded = 10,
			PacketRemoved = 11,
			PacketChanged = 12,

			FileAdded = 13,
			FileRemoved = 14,
			FileChanged = 15,

			SearchAdded = 16,
			SearchRemoved = 17,
			SearchChanged = 18,

			SnapshotAdded = 19,

			#endregion

			#region MULTI

			SearchPacket = 101,
			SearchBot = 102,

			Servers = 103,
			ChannelsFromServer = 104,
			PacketsFromBot = 105,

			Files = 106,
			Object = 107,
			Searches = 108,

			Snapshots = 109,

			Statistics = 110

			#endregion
		}

		#endregion
		
		#region VARIABLES
		
		[JsonProperty]
		public Types Type { get; set; }

		#endregion
	}
}

