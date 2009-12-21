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
	public class XGFilePart : XGObject
	{
		public new XGFile Parent
		{
			get { return base.Parent as XGFile; }
			set { base.Parent = value; }
		}

		[field: NonSerialized()]
		private XGPacket packet;
		public XGPacket Packet
		{
			get { return this.packet; }
			set
			{
				if (this.packet != value)
				{
					this.packet = value;
					if (this.packet != null) { this.packetGuid = this.packet.Guid; }
					else { this.packetGuid = Guid.Empty; }
				}
			}
		}

		private Guid packetGuid;
		public Guid PacketGuid
		{
			get { return this.packetGuid; }
		}

		private Int64 startSize = 0;
		public Int64 StartSize
		{
			get { return this.startSize; }
			set
			{
				if (this.startSize != value)
				{
					this.startSize = value;
					this.Modified = true;
				}
			}
		}

		private Int64 stopSize = 0;
		public Int64 StopSize
		{
			get { return this.stopSize; }
			set
			{
				if (this.stopSize != value)
				{
					this.stopSize = value;
					this.Modified = true;
				}
			}
		}

		private Int64 currentSize = 0;
		public Int64 CurrentSize
		{
			get { return this.currentSize; }
			set
			{
				if (this.currentSize != value)
				{
					this.currentSize = value;
					this.Modified = true;
				}
			}
		}

		public Int64 MissingSize
		{
			get { return this.stopSize - this.currentSize; }
		}

		public Int64 TimeMissing
		{
			get { return (this.speed > 0 ? (Int64)(this.MissingSize / this.Speed) : Int64.MaxValue); }
		}

		private double speed = 0;
		public double Speed
		{
			get { return this.speed; }
			set
			{
				if (this.speed != value)
				{
					this.speed = value;
					this.Modified = true;
				}
			}
		}

		private FilePartState partState;
		public FilePartState PartState
		{
			get { return this.partState; }
			set
			{
				if (this.partState != value)
				{
					this.partState = value;
					if (this.partState != FilePartState.Open) { this.speed = 0; }
					if (this.partState == FilePartState.Ready) { this.currentSize = this.stopSize; }
					this.Modified = true;
				}
			}
		}

		private bool isChecked;
		public bool IsChecked
		{
			get { return this.isChecked; }
			set
			{
				if (this.isChecked != value)
				{
					this.isChecked = value;
					this.Modified = true;
				}
			}
		}

		public XGFilePart()
			: base()
		{
		}
		public XGFilePart(XGFile aParent)
			: this()
		{
			this.Parent = aParent;
			this.Parent.addPart(this);
			this.isChecked = false;
			this.partState = FilePartState.Closed;
		}

		public void Clone(XGFilePart aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
			this.packetGuid = aCopy.packetGuid;
			this.isChecked = aCopy.isChecked;
			this.partState = aCopy.partState;
			this.speed = aCopy.speed;
			this.startSize = aCopy.startSize;
			this.currentSize = aCopy.currentSize;
			this.stopSize = aCopy.stopSize;
			if (aFull) { this.Parent = aCopy.Parent; }
		}
	}
}
