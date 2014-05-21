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
using Db4objects.Db4o;

namespace XG.Model.Domain
{
	public class Packet : AObject
	{
		#region VARIABLES

		public new Bot Parent
		{
			get { return base.Parent as Bot; }
			set { base.Parent = value; }
		}

		[Transient]
		File _file;

		public File File
		{
			get { return _file; }
			set { _file = value; }
		}

		public override string Name
		{
			get { return base.Name; }
			set
			{
				// iam updated if the packet name changes
				if (base.Name != value)
				{
					LastUpdated = DateTime.Now;
				}
				base.Name = value;
			}
		}

		int _id = -1;

		public int Id
		{
			get { return GetProperty(ref _id); }
			set { SetProperty(ref _id, value, "Id"); }
		}

		Int64 _size;

		public Int64 Size
		{
			get { return GetProperty(ref _size); }
			set { SetProperty(ref _size, value, "Size"); }
		}

		Int64 _realSize;

		public Int64 RealSize
		{
			get { return GetProperty(ref _realSize); }
			set { SetProperty(ref _realSize, value, "RealSize"); }
		}

		string _realName = "";

		public string RealName
		{
			get { return GetProperty(ref _realName); }
			set { SetProperty(ref _realName, value, "RealName"); }
		}

		DateTime _lastUpdated = DateTime.MinValue.ToUniversalTime();

		public DateTime LastUpdated
		{
			get { return GetProperty(ref _lastUpdated); }
			set { SetProperty(ref _lastUpdated, value, "LastUpdated"); }
		}

		DateTime _lastMentioned = DateTime.MinValue.ToUniversalTime();

		public DateTime LastMentioned
		{
			get { return GetProperty(ref _lastMentioned); }
			set { SetProperty(ref _lastMentioned, value, "LastMentioned"); }
		}

		public bool Next
		{
			get
			{
				Packet oldestPacket = Parent != null ? Parent.OldestActivePacket() : null;
				return oldestPacket != null && oldestPacket == this;
			}
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return base.ToString() + "|#" + Id + "|" + (RealSize > 0 ? RealSize : Size);
		}

		#endregion
	}
}
