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

namespace XG.Extensions
{
	public class EventArgs<T1> : EventArgs
	{
		public EventArgs(T1 aValue1)
		{
			Value1 = aValue1;
		}

		public T1 Value1 { get; private set; }
	}

	public class EventArgs<T1, T2> : EventArgs<T1>
	{
		public EventArgs(T1 aValue, T2 aValue2) : base(aValue)
		{
			Value2 = aValue2;
		}

		public T2 Value2 { get; private set; }
	}

	public class EventArgs<T1, T2, T3> : EventArgs<T1, T2>
	{
		public EventArgs(T1 aValue, T2 aValue2, T3 aValue3)
			: base(aValue, aValue2)
		{
			Value3 = aValue3;
		}

		public T3 Value3 { get; private set; }
	}

	public class EventArgs<T1, T2, T3, T4> : EventArgs<T1, T2, T3>
	{
		public EventArgs(T1 aValue, T2 aValue2, T3 aValue3, T4 aValue4)
			: base(aValue, aValue2, aValue3)
		{
			Value4 = aValue4;
		}

		public T4 Value4 { get; private set; }
	}

	public class EventArgs<T1, T2, T3, T4, T5> : EventArgs<T1, T2, T3, T4>
	{
		public EventArgs(T1 aValue, T2 aValue2, T3 aValue3, T4 aValue4, T5 aValue5)
			: base(aValue, aValue2, aValue3, aValue4)
		{
			Value5 = aValue5;
		}

		public T5 Value5 { get; private set; }
	}
}
