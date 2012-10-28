//
//  Extensions.cs
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
using System.Text;

namespace XG.Core
{
	public static class Extensions
	{
		public static string ToAscii(this string aString)
		{
			StringBuilder output = new StringBuilder(string.Empty);
			if (!string.IsNullOrEmpty(aString))
			{
				for (int i = 0; i < aString.Length; i++)
				{
					output.AppendFormat("&#{0};", Encoding.ASCII.GetBytes(aString.Substring(i, 1))[0]);
				}
			}
			return output.ToString();
		}

		public static Int64 ToTimestamp(this DateTime aDate)
		{
			DateTime date = new DateTime (1970, 1, 1);
			TimeSpan ts = new TimeSpan (aDate.Ticks - date.Ticks);
			return (Convert.ToInt64 (ts.TotalSeconds));
		}
		
		public static DateTime ToDate(this Int64 aTimestamp)
		{
			DateTime date = new DateTime (1970, 1, 1);
			date.AddSeconds (aTimestamp);
			return date;
		}

		public static bool IsEqualWith(this byte[] aBytes1, byte[] aBytes2)
		{
			if (aBytes1 == null || aBytes2 == null) { return false; }
			for (int i = 0; i < aBytes1.Length; i++)
			{
				if (aBytes1[i] != aBytes2[i]) { return false; }
			}
			return true;
		}
	}
}

