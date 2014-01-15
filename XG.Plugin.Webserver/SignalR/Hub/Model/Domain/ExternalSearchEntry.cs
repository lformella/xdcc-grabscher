// 
//  ExternalSearchEntry.cs
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

namespace XG.Plugin.Webserver.SignalR.Hub.Model.Domain
{
	[JsonObject(MemberSerialization.OptOut)]
	public class ExternalSearchEntry
	{
		#region PROPERTIES

		public Guid ParentGuid { get; set; }

		public Guid Guid { get; set; }
		
		public virtual string Name { get; set; }
		
		public bool Connected { get; set; }
		
		public bool Enabled { get; set; }

		public int Id { get; set; }

		public Int64 Size { get; set; }
		
		public DateTime LastUpdated { get; set; }
		
		public DateTime LastMentioned { get; set; }
		
		public string BotName { get; set; }
		
		public Int64 BotSpeed { get; set; }
		
		public Int64 BotConnected { get; set; }
		
		public Int64 BotHasFreeSlots { get; set; }
		
		public Int64 BotHasFreeQueue { get; set; }
		
		public string IrcLink { get; set; }

		#endregion
	}
}
