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

using Newtonsoft.Json;

namespace XG.Server.Plugin.General.Webserver.Object
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Notification : AObject
	{
		[JsonIgnore]
		public new Core.Notification Object
		{
			get
			{
				return (Core.Notification) base.Object;
			}
			set
			{
				base.Object = value;
			}
		}

		#region VARIABLES

		public Core.Notification.Types Type
		{
			get { return Object.Type; }
		}

		public string ObjectName
		{
			get { return Object.Object.Name; }
		}

		public string ParentName
		{
			get { return Object.Object.Parent != null ? Object.Object.Parent.Name : ""; }
		}

		#endregion
	}
}
