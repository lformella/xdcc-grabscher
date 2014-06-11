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
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace XG.Plugin.Webserver.Nancy.Api.Model.Domain
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Packet : AObject
	{
		[XmlIgnore]
		[JsonIgnore]
		public new XG.Model.Domain.Packet Object
		{
			get
			{
				return (XG.Model.Domain.Packet)base.Object;
			}
			set
			{
				base.Object = value;
//				_bot = new Bot { Object = value.Parent };
			}
		}

//		Bot _bot;

		#region VARIABLES

//		public Bot Bot
//		{
//			get { return _bot; }
//		}

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
			get { return !string.IsNullOrWhiteSpace(Object.RealName) ? Object.RealName : Object.Name; }
		}

		public DateTime LastUpdated
		{
			get { return Object.LastUpdated; }
		}

		public DateTime LastMentioned
		{
			get { return Object.LastMentioned; }
		}

		public bool Next
		{
			get { return Object.Next; }
		}
		
		public Int64 Speed
		{
			get { return Object.File != null ? Object.File.Speed : 0; }
		}
		
		public Int64 CurrentSize
		{
			get { return Object.File != null ? Object.File.CurrentSize : 0; }
		}
		
		public Int64 TimeMissing
		{
			get { return Object.File != null ? Object.File.TimeMissing : 0; }
		}

		#endregion
	}
}
