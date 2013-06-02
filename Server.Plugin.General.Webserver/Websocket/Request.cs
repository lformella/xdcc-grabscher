//
//  Request.cs
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

using Newtonsoft.Json;

using System;

namespace XG.Server.Plugin.General.Webserver.Websocket
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Request
	{
		#region ENUMS

		public enum Types
		{
			None = 0,

			AddServer = 1,
			RemoveServer = 2,
			AddChannel = 3,
			RemoveChannel = 4,

			ActivateObject = 5,
			DeactivateObject = 6,

			Search = 7,
			SearchExternal = 8,

			Servers = 9,
			ChannelsFromServer = 10,
			PacketsFromBot = 11,
			Files = 12,

			AddSearch = 13,
			RemoveSearch = 14,
			Searches = 15,

			Snapshots = 16,
			ParseXdccLink = 17,

			CloseServer = 18
		}

		#endregion

		#region VARIABLES

		public Types Type { get; set; }

		public string Password { get; set; }

		public Guid Guid { get; set; }

		public string Name { get; set; }

		#endregion
	}
}

