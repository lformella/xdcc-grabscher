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
	public class XGPacket : XGObject
	{
		public new XGBot Parent
		{
			get { return base.Parent as XGBot; }
			set { base.Parent = value; }
		}

		private int id = -1;
		public int Id
		{
			get { return this.id; }
			set
			{
				if (this.id != value)
				{
					this.id = value;
					this.Modified = true;
				}
			}
		}

		private Int64 size = 0;
		public Int64 Size
		{
			get { return this.size; }
			set
			{
				if (this.size != value)
				{
					this.size = value;
					this.Modified = true;
				}
			}
		}

		private Int64 realSize = 0;
		public Int64 RealSize
		{
			get { return this.realSize; }
			set
			{
				if (this.realSize != value)
				{
					this.realSize = value;
					this.Modified = true;
				}
			}
		}

		private string realName = "";
		public string RealName
		{
			get { return this.realName; }
			set
			{
				if (this.realName != value)
				{
					this.realName = value;
					this.Modified = true;
				}
			}
		}

		private DateTime lastUpdated = new DateTime(1, 1, 1);
		public DateTime LastUpdated
		{
			get { return this.lastUpdated; }
			set
			{
				if (this.lastUpdated != value)
				{
					this.lastUpdated = value;
					this.Modified = true;
				}
			}
		}

		public XGPacket()
			: base()
		{
			this.realName = "";
			this.LastUpdated = DateTime.Now;
		}
		public XGPacket(XGBot parent)
			: this()
		{
			this.Parent = parent;
			this.Parent.addPacket(this);
		}

		public void Clone(XGPacket aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
			this.id = aCopy.id;
			this.size = aCopy.size;
			if (aFull)
			{
				this.realName = aCopy.realName;
				this.realSize = aCopy.realSize;
			}
		}
	}
}
