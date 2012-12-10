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

namespace XG.Server.Plugin.General.Webserver.Websocket
{
	[JsonObject(MemberSerialization.OptIn)]
	public class Response
	{
		#region ENUMS

		public enum Types
		{
			None = 0,

			#region SINGLE

			ObjectAdded = 1,
			ObjectRemoved = 2,
			ObjectChanged = 3,

			BlockStart = 4,
			BlockStop = 5,

			#endregion

			#region MULTI

			//SearchPacket = 11,
			//SearchBot = 12,
			SearchExternal = 13,

			//Servers = 14,
			//ChannelsFromServer = 15,
			//PacketsFromBot = 16,

			//Files = 17,
			Searches = 18,

			Snapshots = 19,

			Statistics = 20

			#endregion
		}

		#endregion

		#region VARIABLES

		[JsonProperty]
		public Types Type { get; set; }

		[JsonProperty]
		public object Data { get; set; }

		[JsonProperty]
		public string DataType
		{
			get
			{
				return Data.GetType().Name;
			}
		}

		#endregion
	}
}

