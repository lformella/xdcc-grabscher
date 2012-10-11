// 
//  FilePart.cs
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
	public class FilePart : AObject
	{
		#region ENUMS

		public enum States : byte
		{
			Open,
			Closed,
			Ready,
			Broken
		}

		#endregion

		#region VARIABLES

		public new File Parent
		{
			get { return base.Parent as File; }
			set { base.Parent = value; }
		}

		[NonSerialized]
		Packet _packet;
		public Packet Packet
		{
			get { return _packet; }
			set
			{
				if (_packet != value)
				{
					_packet = value;
					if (_packet != null) { _packetGuid = _packet.Guid; }
					else
					{
						_packetGuidOld = _packetGuid;
						_packetGuid = Guid.Empty;
					}
				}
			}
		}

		Guid _packetGuid;
		public Guid PacketGuid
		{
			get { return _packetGuid; }
		}

		Guid _packetGuidOld;
		public Guid PacketGuidOld
		{
			get { return _packetGuidOld; }
		}

		Int64 _startSize = 0;
		[DataMember]
		public Int64 StartSize
		{
			get { return _startSize; }
			set { SetProperty(ref _startSize, value); }
		}

		Int64 _stopSize = 0;
		[DataMember]
		public Int64 StopSize
		{
			get { return _stopSize; }
			set { SetProperty(ref _stopSize, value); }
		}

		Int64 _currentSize = 0;
		[DataMember]
		public Int64 CurrentSize
		{
			get { return _currentSize; }
			set { SetProperty(ref _currentSize, value); }
		}
		
		[DataMember]
		public Int64 MissingSize
		{
			get { return _stopSize - _currentSize; }
			private set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}
		
		[DataMember]
		public Int64 TimeMissing
		{
			get { return (_speed > 0 ? (Int64)(MissingSize / Speed) : Int64.MaxValue); }
			private set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}

		Int64 _speed = 0;
		[DataMember]
		public Int64 Speed
		{
			get { return _speed; }
			set { SetProperty(ref _speed, value); }
		}

		FilePart.States _state;
		[DataMember]
		public FilePart.States State
		{
			get { return _state; }
			set
			{
				if (_state != value)
				{
					_state = value;
					if (_state != FilePart.States.Open) { _speed = 0; }
					if (_state == FilePart.States.Ready) { _currentSize = _stopSize; }
					Modified = true;
				}
			}
		}

		bool _checked;
		[DataMember]
		public bool Checked
		{
			get { return _checked; }
			set { SetProperty(ref _checked, value); }
		}
		
		[NonSerialized]
		byte[] _startReference;
		public byte[] StartReference
		{
			get { return _startReference; }
			set { _startReference = value; }
		}

		#endregion
	}
}
