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
	[Flags]
	public enum FilePartState : byte
	{
		Open,
		Closed,
		Ready,
		Broken
	}

	[Serializable()]
	public class FilePart : AObject
	{
		#region VARIABLES

		public new File Parent
		{
			get { return base.Parent as File; }
			set { base.Parent = value; }
		}

		[field: NonSerialized()]
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
		public Int64 StartSize
		{
			get { return _startSize; }
			set
			{
				if (_startSize != value)
				{
					_startSize = value;
					Modified = true;
				}
			}
		}

		Int64 _stopSize = 0;
		public Int64 StopSize
		{
			get { return _stopSize; }
			set
			{
				if (_stopSize != value)
				{
					_stopSize = value;
					Modified = true;
				}
			}
		}

		Int64 _currentSize = 0;
		public Int64 CurrentSize
		{
			get { return _currentSize; }
			set
			{
				if (_currentSize != value)
				{
					_currentSize = value;
					Modified = true;
				}
			}
		}

		public Int64 MissingSize
		{
			get { return _stopSize - _currentSize; }
		}

		public Int64 TimeMissing
		{
			get { return (_speed > 0 ? (Int64)(MissingSize / Speed) : Int64.MaxValue); }
		}

		double _speed = 0;
		public double Speed
		{
			get { return _speed; }
			set
			{
				if (_speed != value)
				{
					_speed = value;
					Modified = true;
				}
			}
		}

		FilePartState _partState;
		public FilePartState PartState
		{
			get { return _partState; }
			set
			{
				if (_partState != value)
				{
					_partState = value;
					if (_partState != FilePartState.Open) { _speed = 0; }
					if (_partState == FilePartState.Ready) { _currentSize = _stopSize; }
					Modified = true;
				}
			}
		}

		bool _isChecked;
		public bool IsChecked
		{
			get { return _isChecked; }
			set
			{
				if (_isChecked != value)
				{
					_isChecked = value;
					Modified = true;
				}
			}
		}

		#endregion
	}
}
