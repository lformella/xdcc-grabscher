//
//  Request.cs
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

namespace XG.Server.Plugin.General.Webserver.WebSocket
{
	public class Request
	{
		#region ENUMS

		public enum Types
		{
			None = 0,
			Version = 1,

			AddServer = 2,
			RemoveServer = 3,
			AddChannel = 4,
			RemoveChannel = 5,

			ActivateObject = 6,
			DeactivateObject = 7,

			SearchPacket = 8,
			SearchBot = 9,

			Servers = 10,
			ChannelsFromServer = 11,
			BotsFromChannel = 12,
			PacketsFromBot = 13,
			Files = 14,
			Object = 15,

			AddSearch = 16,
			RemoveSearch = 17,
			Searches = 18,

			Statistics = 19,
			GetSnapshots = 20,
			ParseXdccLink = 21,

			CloseServer = 22
		}

		public enum SortModes
		{
			Asc,
			Desc
		}

		#endregion

		#region VARIABLES

		public Types Type { get; set; }

		public string Password { get; set; }

		public Guid Guid { get; set; }

		public string Name { get; set; }

		public bool IgnoreOfflineBots { get; set; }

		public int Page { get; set; }

		public int Rows { get; set; }

		public string SearchBy { get; set; }

		public string Search { get; set; }

		public string SortBy { get; set; }

		public SortModes SortMode { get; set; }

		#endregion
	}
}

