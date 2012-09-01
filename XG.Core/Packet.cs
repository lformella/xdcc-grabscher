//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;

namespace XG.Core
{
	[Serializable()]
	public class Packet : AObject
	{
		#region VARIABLES

		public new Bot Parent
		{
			get { return base.Parent as Bot; }
			set { base.Parent = value; }
		}

		[field: NonSerialized()]
		FilePart part;
		public FilePart Part
		{
			get { return part; }
			set { part = value; }
		}

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
