// 
//  Extensions.cs
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
using System.Text.RegularExpressions;

namespace XG.Extensions
{
	public static class Extensions
	{
		public static Int64 ToTimestamp(this DateTime aDate)
		{
			var date = new DateTime(1970, 1, 1);
			var ts = new TimeSpan(aDate.Ticks - date.Ticks);
			return (Convert.ToInt64(ts.TotalSeconds));
		}

		public static DateTime ToDate(this Int64 aTimestamp)
		{
			var date = new DateTime(1970, 1, 1);
			return date.AddSeconds(aTimestamp);
		}

		public static bool IsEqualWith(this byte[] aBytes1, byte[] aBytes2)
		{
			if (aBytes1 == null || aBytes2 == null)
			{
				return false;
			}
			for (int i = 0; i < aBytes1.Length; i++)
			{
				if (aBytes1[i] != aBytes2[i])
				{
					return false;
				}
			}
			return true;
		}

		public static string RemoveSpecialChars(this string aStr)
		{
			return Regex.Replace(aStr, @"[^a-z0-9,.;:_\(\)\[\]\s-]", "", RegexOptions.IgnoreCase).Trim();
		}

		public static string Implode(this IEnumerable<string> aList, string aDelimiter)
		{
			string result = "";
			int count = 0;
			foreach (var str in aList)
			{
				if (count > 0)
				{
					result += aDelimiter;
				}
				result += str;
				count++;
			}
			return result;
		}

		public static double Difference(this String a, String b)
		{
			if (string.IsNullOrEmpty(a))
			{
				return string.IsNullOrEmpty(b) ? 0 : b.Length;
			}

			if (string.IsNullOrEmpty(b))
			{
				return string.IsNullOrEmpty(a) ? 0 : a.Length;
			}

			int cost;
			var d = new int[a.Length + 1, b.Length + 1];
			int min1;
			int min2;
			int min3;

			for (int i = 0; i <= d.GetUpperBound(0); i += 1)
			{
				d[i, 0] = i;
			}

			for (int i = 0; i <= d.GetUpperBound(1); i += 1)
			{
				d[0, i] = i;
			}

			for (int i = 1; i <= d.GetUpperBound(0); i += 1)
			{
				for (int j = 1; j <= d.GetUpperBound(1); j += 1)
				{
					cost = Convert.ToInt32(a[i - 1] != b[j - 1]);

					min1 = d[i - 1, j] + 1;
					min2 = d[i, j - 1] + 1;
					min3 = d[i - 1, j - 1] + cost;
					d[i, j] = Math.Min(Math.Min(min1, min2), min3);
				}
			}

			int levenshtein = d[d.GetUpperBound(0), d.GetUpperBound(1)];
			return Math.Round(levenshtein * 1.0 / (a.Length > b.Length ? a.Length : b.Length), 2);
		}
	}
}
