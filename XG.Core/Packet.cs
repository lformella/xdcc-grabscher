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

using System;
using System.Runtime.Serialization;

namespace XG.Core
{
	[Serializable]
	[DataContract]
	public class Packet : AObject
	{
		#region VARIABLES

		public new Bot Parent
		{
			get { return base.Parent as Bot; }
			set { base.Parent = value; }
		}

		[NonSerialized]
		FilePart part;
		[DataMember]
		public FilePart Part
		{
			get { return part; }
			set { part = value; }
		}

		[DataMember]
		public override string Name
		{
			get { return base.Name; }
			set
			{
				// iam updated if the packet name changes
				if (base.Name != value)
				{
					lastUpdated = DateTime.Now;
				}
				base.Name = value;
			}
		}

		int id = -1;
		[DataMember]
		public int Id
		{
			get { return id; }
			set
			{
				if (id != value)
				{
					id = value;
					Modified = true;
				}
			}
		}

		Int64 size = 0;
		[DataMember]
		public Int64 Size
		{
			get { return size; }
			set
			{
				if (size != value)
				{
					size = value;
					Modified = true;
				}
			}
		}

		Int64 realSize = 0;
		[DataMember]
		public Int64 RealSize
		{
			get { return realSize; }
			set
			{
				if (realSize != value)
				{
					realSize = value;
					Modified = true;
				}
			}
		}

		string realName = "";
		[DataMember]
		public string RealName
		{
			get { return realName; }
			set
			{
				if (realName != value)
				{
					realName = value;
					Modified = true;
				}
			}
		}

		DateTime lastUpdated = new DateTime(1, 1, 1);
		[DataMember]
		public DateTime LastUpdated
		{
			get { return lastUpdated; }
			set
			{
				if (lastUpdated != value)
				{
					lastUpdated = value;
					Modified = true;
				}
			}
		}

		DateTime lastMentioned = new DateTime(1, 1, 1, 0, 0, 0, 0);
		[DataMember]
		public DateTime LastMentioned
		{
			get { return lastMentioned; }
			set
			{
				if (lastMentioned != value)
				{
					lastMentioned = value;
					Modified = true;
				}
			}
		}

		#endregion
	}
}
