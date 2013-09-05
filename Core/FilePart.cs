// 
//  FilePart.cs
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
					if (_packet != null)
					{
						_packetGuid = _packet.Guid;
					}
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

		Int64 _startSize;

		public Int64 StartSize
		{
			get { return _startSize; }
			set { SetProperty(ref _startSize, value, "StartSize"); }
		}

		Int64 _stopSize;

		public Int64 StopSize
		{
			get { return _stopSize; }
			set { SetProperty(ref _stopSize, value, "StopSize"); }
		}

		Int64 _currentSize;

		public Int64 CurrentSize
		{
			get { return _currentSize; }
			set { SetProperty(ref _currentSize, value, "CurrentSize"); }
		}
		
		public Int64 DownloadedSize
		{
			get { return _currentSize - _startSize; }
		}

		public Int64 MissingSize
		{
			get { return _stopSize - _currentSize; }
		}

		public Int64 TimeMissing
		{
			get
			{
				Int64 time = (_speed > 0 ? (MissingSize / Speed) : 0);
				return time;
			}
		}

		Int64 _speed;

		public Int64 Speed
		{
			get { return _speed; }
			set { SetProperty(ref _speed, value, "Speed"); }
		}

		States _state;

		public States State
		{
			get { return _state; }
			set
			{
				if (_state != value)
				{
					SetProperty(ref _state, value, "State");

					if (_state != States.Open)
					{
						_speed = 0;
					}
					if (_state == States.Ready)
					{
						_currentSize = _stopSize;
					}
				}
			}
		}

		bool _checked;

		public bool Checked
		{
			get { return _checked; }
			set { SetProperty(ref _checked, value, "Checked"); }
		}

		[NonSerialized]
		byte[] _startReference;

		public byte[] StartReference
		{
			get { return _startReference; }
			set { _startReference = value; }
		}

		#endregion

		#region CONSTRUCTOR

		public FilePart(FilePart aObject = null) : base(aObject)
		{
			if (aObject != null)
			{
				_startSize = aObject._startSize;
				_stopSize = aObject._stopSize;
				_currentSize = aObject._currentSize;
				_speed = aObject._speed;
				_state = aObject._state;
				_checked = aObject._checked;
			}
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return base.ToString() + "|" + StartSize + "|" + StopSize;
		}

		#endregion
	}
}
