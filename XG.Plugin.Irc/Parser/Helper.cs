// 
//  Helper.cs
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
using System.Text.RegularExpressions;

namespace XG.Plugin.Irc.Parser
{
	public static class Helper
	{
		public static string Magicstring = @"((\*|:){2,3}|->|<-|)";

		public static string RemoveSpecialIrcChars(string aData)
		{
			string tData = Regex.Replace(aData, @"[\x02\x1F\x0F\x16]|\x03(\d\d?(,\d\d?)?)?", String.Empty);
			return tData.Trim();
		}

		public static Match Match(string aMessage, string[] aRegexes)
		{
			Match match = null;
			foreach (string regex in aRegexes)
			{
				match = Match(aMessage, regex);
				if (match.Success)
				{
					return match;
				}
			}
			return match;
		}

		public static Match Match(string aMessage, string aRegex)
		{
			return Regex.Match(aMessage, aRegex, RegexOptions.IgnoreCase);
		}
	}
}
