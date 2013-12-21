// 
//  Notification.cs
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
	public class Notification : AObject
	{
		[JsonIgnore]
		public new XG.Model.Domain.Notification Object
		{
			get
			{
				return (XG.Model.Domain.Notification)base.Object;
			}
			set
			{
				base.Object = value;
			}
		}

		#region VARIABLES

		public new XG.Model.Domain.Notification.Types Type
		{
			get { return Object.Type; }
		}

		public string ObjectName1
		{
			get { return Object.Object1 != null ? Object.Object1.Name : ""; }
		}

		public string ParentName1
		{
			get { return Object.Object1 != null && Object.Object1.Parent != null ? Object.Object1.Parent.Name : ""; }
		}

		public string ObjectName2
		{
			get { return Object.Object2 != null ? Object.Object2.Name : ""; }
		}

		public string ParentName2
		{
			get { return Object.Object2 != null && Object.Object2.Parent != null ? Object.Object2.Parent.Name : ""; }
		}

		public DateTime Time
		{
			get { return Object.Time; }
		}

		#endregion
	}
}
