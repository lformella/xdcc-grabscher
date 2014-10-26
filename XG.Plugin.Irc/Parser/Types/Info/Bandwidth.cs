// 
//  Bandwitdh.cs
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
using System.Threading;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types.Info
{
	public class Bandwitdh : AParserWithExistingBot
	{
		protected override bool ParseInternal(Bot aBot, string aMessage)
		{
			string[] regexes =
			{
				Helper.Magicstring + " ((Bandwidth Usage|Bandbreite) " + Helper.Magicstring + "|)\\s*(Current|Derzeit): (?<speed_cur>[0-9.]*)(?<speed_cur_end>(K|)(i|)B)(\\/s|s)(,|)(.*Record: (?<speed_max>[0-9.]*)(?<speed_max_end>(K|)(i|))B(\\/s|s)|)"
			};
			var match = Helper.Match(aMessage, regexes);
			if (match.Success)
			{
				string speedCurEnd = match.Groups["speed_cur_end"].ToString().ToLower();
				string speedMaxEnd = match.Groups["speed_max_end"].ToString().ToLower();
				string speedCur = match.Groups["speed_cur"].ToString();
				string speedMax = match.Groups["speed_max"].ToString();
				if (Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
				{
					speedCur = speedCur.Replace('.', ',');
					speedMax = speedMax.Replace('.', ',');
				}
				double valueDouble;
				if (double.TryParse(speedCur, out valueDouble))
				{
					aBot.InfoSpeedCurrent = speedCurEnd.StartsWith("k", StringComparison.CurrentCulture) ? (Int64) (valueDouble * 1024) : (Int64) valueDouble;
				}
				if (double.TryParse(speedMax, out valueDouble))
				{
					aBot.InfoSpeedMax = speedMaxEnd.StartsWith("k", StringComparison.CurrentCulture) ? (Int64) (valueDouble * 1024) : (Int64) valueDouble;
				}
			}
			return match.Success;
		}
	}
}
