//
//  TimedList.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
//
//  Copyright (c) 2013 Lars Formella
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace XG.Plugin.Irc
{
	public class TimedList<T> : IEnumerable<T>
	{
		readonly ConcurrentDictionary<T, DateTime> _queue = new ConcurrentDictionary<T, DateTime>();

		public IEnumerator<T> GetEnumerator ()
		{
			return _queue.Keys.ToList().GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T aObj, DateTime aDate)
		{
			RemoveExpiredItems();

			if (!Contains(aObj))
			{
				_queue.TryAdd(aObj, aDate);
			}
			else
			{
				_queue[aObj] = aDate;
			}
		}

		public IEnumerable<T> GetExpiredItems(bool aRemoveExpiredItems = true)
		{
			var keys = (from kvp in _queue where (kvp.Value - DateTime.Now).TotalSeconds < 0 select kvp.Key).ToArray();
			if (aRemoveExpiredItems)
			{
				RemoveExpiredItems();
			}
			return keys;
		}

		public bool Contains(T aObj)
		{
			return _queue.ContainsKey(aObj);
		}

		public double GetMissingSeconds(T aObj)
		{
			DateTime date;
			_queue.TryGetValue(aObj, out date);
			double seconds = (date - DateTime.Now).TotalSeconds;
			return seconds > 0 ? seconds : 0;
		}

		public void RemoveExpiredItems()
		{
			DateTime date;
			var keys = (from kvp in _queue where (kvp.Value - DateTime.Now).TotalSeconds < 0 select kvp.Key).ToArray();
			foreach (var key in keys)
			{
				_queue.TryRemove(key, out date);
			}
		}
	}
}

