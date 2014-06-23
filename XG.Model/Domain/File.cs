// 
//  File.cs
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
	public class File : AObject
	{
		#region VARIABLES

		string _tmpName;

		public string TmpName
		{
			get { return GetProperty(ref _tmpName); }
			protected set { SetProperty(ref _tmpName, value, "TmpName"); }
		}

		Int64 _size;

		public Int64 Size
		{
			get { return GetProperty(ref _size); }
			protected set { SetProperty(ref _size, value, "Size"); }
		}

		Int64 _currentSize;

		public Int64 CurrentSize
		{
			get { return GetProperty(ref _currentSize); }
			set { SetProperty(ref _currentSize, value, "CurrentSize"); }
		}

		public Int64 MissingSize
		{
			get { return Size - CurrentSize; }
		}

		public Int64 TimeMissing
		{
			get { return (Speed > 0 ? (MissingSize / Speed) : 0); }
		}

		[Transient]
		Int64 _speed;

		public Int64 Speed
		{
			get { return _speed; }
			set { SetProperty(ref _speed, value, "Speed"); }
		}

		[Transient]
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

		[Transient]
		Guid _packetGuid;

		public Guid PacketGuid
		{
			get { return GetProperty(ref _packetGuid); }
		}

		[Transient]
		Guid _packetGuidOld;

		public Guid PacketGuidOld
		{
			get { return GetProperty(ref _packetGuidOld); }
		}

		#endregion
		
		#region CONSTRUCTOR

		public File(string aName, Int64 aSize)
		{
			base.Name = Helper.RemoveBadCharsFromFileName(aName);
			_size = aSize;
			_tmpName = Helper.ShrinkFileName(aName, aSize);
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return base.ToString() + "|" + Size + "|" + TmpName;
		}

		#endregion
	}
}
