// 
//  Packet.cs
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
using Newtonsoft.Json.Converters;

using System;

namespace XG.Core
{
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	public class Packet : AObject
	{
		#region VARIABLES

		public new Bot Parent
		{
			get { return base.Parent as Bot; }
			set { base.Parent = value; }
		}

		[NonSerialized]
		FilePart _part;

		[JsonProperty]
		public FilePart Part
		{
			get { return _part; }
			set { _part = value; }
		}

		public override string Name
		{
			get { return base.Name; }
			set
			{
				// iam updated if the packet name changes
				if (base.Name != value)
				{
					_lastUpdated = DateTime.Now;
				}
				base.Name = value;
			}
		}

		int _id = -1;

		[JsonProperty]
		[MySql]
		public int Id
		{
			get { return _id; }
			set { SetProperty(ref _id, value); }
		}

		Int64 _size;

		[JsonProperty]
		[MySql]
		public Int64 Size
		{
			get { return _size; }
			set { SetProperty(ref _size, value); }
		}

		Int64 _realSize;

		[JsonProperty]
		public Int64 RealSize
		{
			get { return _realSize; }
			set { SetProperty(ref _realSize, value); }
		}

		string _realName = "";
		
		[JsonProperty]
		public string RealName
		{
			get { return _realName; }
			set { SetProperty(ref _realName, value); }
		}
/*
		[JsonProperty(PropertyName = "RealName")]
		public string RealNameAscii
		{
			get { return RealName.ToAscii(); }
			set { RealName = value; }
		}
*/
		DateTime _lastUpdated = DateTime.MinValue.ToUniversalTime();

		[JsonProperty]
		[MySql]
		public DateTime LastUpdated
		{
			get { return _lastUpdated; }
			set { SetProperty(ref _lastUpdated, value); }
		}

		DateTime _lastMentioned = DateTime.MinValue.ToUniversalTime();

		[JsonProperty]
		[MySql]
		public DateTime LastMentioned
		{
			get { return _lastMentioned; }
			set { SetProperty(ref _lastMentioned, value); }
		}

		[JsonProperty]
		public bool Next
		{
			get
			{
				Packet oldestPacket = Parent != null ? Parent.OldestActivePacket() : null;
				return oldestPacket != null && oldestPacket == this;
			}
			private set { throw new NotSupportedException("You can not set this Property."); }
		}

		#endregion
	}
}
