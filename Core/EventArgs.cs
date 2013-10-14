// 
//  EventArgs.cs
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
	public class EventArgs<T> : EventArgs
	{
		public EventArgs(T aValue1)
		{
			Value1 = aValue1;
		}

		public T Value1 { get; private set; }
	}

	public class EventArgs<T, U> : EventArgs<T>
	{
		public EventArgs(T aValue, U aValue2) : base(aValue)
		{
			Value2 = aValue2;
		}

		public U Value2 { get; private set; }
	}

	public class EventArgs<T, U, V> : EventArgs<T, U>
	{
		public EventArgs(T aValue, U aValue2, V aValue3)
			: base(aValue, aValue2)
		{
			Value3 = aValue3;
		}

		public V Value3 { get; private set; }
	}

	public class EventArgs<T, U, V, W> : EventArgs<T, U, V>
	{
		public EventArgs(T aValue, U aValue2, V aValue3, W aValue4)
			: base(aValue, aValue2, aValue3)
		{
			Value4 = aValue4;
		}

		public W Value4 { get; private set; }
	}

	public class EventArgs<T, U, V, W, X> : EventArgs<T, U, V, W>
	{
		public EventArgs(T aValue, U aValue2, V aValue3, W aValue4, X aValue5)
			: base(aValue, aValue2, aValue3, aValue4)
		{
			Value5 = aValue5;
		}

		public X Value5 { get; private set; }
	}
}
