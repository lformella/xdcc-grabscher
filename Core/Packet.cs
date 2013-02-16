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

namespace XG.Core
{
	[Serializable]
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

		public int Id
		{
			get { return _id; }
			set { SetProperty(ref _id, value); }
		}

		Int64 _size;

		public Int64 Size
		{
			get { return _size; }
			set { SetProperty(ref _size, value); }
		}

		Int64 _realSize;

		public Int64 RealSize
		{
			get { return _realSize; }
			set { SetProperty(ref _realSize, value); }
		}

		string _realName = "";
		
		public string RealName
		{
			get { return _realName; }
			set { SetProperty(ref _realName, value); }
		}

		DateTime _lastUpdated = DateTime.MinValue.ToUniversalTime();

		public DateTime LastUpdated
		{
			get { return _lastUpdated; }
			set { SetProperty(ref _lastUpdated, value); }
		}

		DateTime _lastMentioned = DateTime.MinValue.ToUniversalTime();

		public DateTime LastMentioned
		{
			get { return _lastMentioned; }
			set { SetProperty(ref _lastMentioned, value); }
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
