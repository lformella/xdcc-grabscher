// 
//  Packet.cs
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

namespace XG.Plugin.ElasticSearch.Object
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Packet : AObject
	{
		[JsonIgnore]
		public new Model.Domain.Packet Object
		{
			get
			{
				return (Model.Domain.Packet)base.Object;
			}
			set
			{
				base.Object = value;
			}
		}

		#region VARIABLES

		public int Id
		{
			get { return Object.Id; }
		}
		
		public Int64 Size
		{
			get { return Object.RealSize > 0 ? Object.RealSize : Object.Size; }
		}

		public new string Name
		{
			get { return (!String.IsNullOrWhiteSpace(Object.RealName) ? Object.RealName : Object.Name).Trim(); }
		}

		public DateTime LastUpdated
		{
			get { return Object.LastUpdated; }
		}

		public DateTime LastMentioned
		{
			get { return Object.LastMentioned; }
		}

		public string BotName
		{
			get { return Object.Parent != null ? Object.Parent.Name : ""; }
		}

		public Int64 BotSpeed
		{
			get { return Object.Parent != null ? Object.Parent.InfoSpeedCurrent : 0; }
		}

		public bool BotConnected
		{
			get { return Object.Parent != null && Object.Parent.Connected; }
		}

		public bool BotHasFreeSlots
		{
			get { return Object.Parent != null && Object.Parent.InfoSlotCurrent > 0; }
		}

		public bool BotHasFreeQueue
		{
			get { return Object.Parent != null && Object.Parent.InfoSlotCurrent > 0 || Object.Parent.InfoQueueCurrent > 0; }
		}

		public string IrcLink
		{
			get
			{
				try
				{
					return "xdcc://" + Object.Parent.Parent.Parent.Name + 
						(Object.Parent.Parent.Parent.Port == 6667 ? "" : ":" + Object.Parent.Parent.Parent.Port) + 
						"/" + Object.Parent.Parent.Parent.Name + 
						"/" + Object.Parent.Parent.Name + 
						"/" + Object.Parent.Name + 
						"/#" + String.Format("{0:0000}", Object.Id) + 
						"/" + Object.Name + "/";
				}
				catch (NullReferenceException)
				{
					return "";
				}
			}
		}

		#endregion
	}
}
