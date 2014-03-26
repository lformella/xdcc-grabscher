// 
//  Bot.cs
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
using System.Linq;
using Newtonsoft.Json;

namespace XG.Plugin.ElasticSearch.Object
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Bot : AObject
	{
		[JsonIgnore]
		public new Model.Domain.Bot Object
		{
			get
			{
				return (Model.Domain.Bot)base.Object;
			}
			set
			{
				base.Object = value;
			}
		}

		#region VARIABLES

		public Model.Domain.Bot.States State
		{
			get { return Object.State; }
		}

		public DateTime LastContact
		{
			get { return Object.LastContact; }
		}

		public string LastMessage
		{
			get { return Object.LastMessage; }
		}

		public Int64 InfoSpeedMax
		{
			get { return Object.InfoSpeedMax; }
		}

		public Int64 InfoSpeedCurrent
		{
			get { return Object.InfoSpeedCurrent; }
		}

		public int InfoSlotTotal
		{
			get { return Object.InfoSlotTotal; }
		}

		public int InfoSlotCurrent
		{
			get { return Object.InfoSlotCurrent; }
		}

		public int InfoQueueTotal
		{
			get { return Object.InfoQueueTotal; }
		}

		public int InfoQueueCurrent
		{
			get { return Object.InfoQueueCurrent; }
		}

		public bool HasNetworkProblems
		{
			get { return Object.HasNetworkProblems; }
		}

		public int PacketCount
		{
			get { return Object.Packets.Count(); }
		}

		public string IrcLink
		{
			get { return Object.Parent != null ?  "irc://" + Object.Parent.Parent.Name + ":" + Object.Parent.Parent.Port + "/" + Object.Parent.Name : ""; }
		}

		#endregion
	}
}
