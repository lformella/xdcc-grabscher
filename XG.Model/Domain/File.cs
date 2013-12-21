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
using System.Collections.Generic;
using System.Linq;

namespace XG.Model.Domain
{
	public class File : AObject
	{
		#region VARIABLES

		public override string Name
		{
			get { return base.Name; }
			//protected set { base.Name = value; }
		}

		string _tmpName;

		public virtual string TmpName
		{
			get { return _tmpName; }
			protected set { _tmpName = value; }
		}

		Int64 _size;

		public virtual Int64 Size
		{
			get { return _size; }
			protected set { _size = value; }
		}

		Int64 _currentSize;

		public virtual Int64 CurrentSize
		{
			get { return _currentSize; }
			set { SetProperty(ref _currentSize, value, "CurrentSize"); }
		}

		public virtual Int64 MissingSize
		{
			get { return _size - _currentSize; }
		}

		public virtual Int64 TimeMissing
		{
			get
			{
				Int64 time = (_speed > 0 ? (MissingSize / Speed) : 0);
				return time;
			}
		}

		Int64 _speed;

		public virtual Int64 Speed
		{
			get { return _speed; }
			set { SetProperty(ref _speed, value, "Speed"); }
		}

		Packet _packet;

		public virtual Packet Packet
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

		public virtual Guid PacketGuid
		{
			get { return _packetGuid; }
		}

		Guid _packetGuidOld;

		public virtual Guid PacketGuidOld
		{
			get { return _packetGuidOld; }
		}

		#endregion
		
		#region CONSTRUCTOR

		protected  File ()
		{}

		public File(string aName, Int64 aSize)
		{
			base.Name = aName;
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
