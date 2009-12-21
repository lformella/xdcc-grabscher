//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;

namespace XG.Client.Widgets.GTK
{
	public static class WidgetHelper
	{
		public static string Size2Human(long aSize)
		{
			if (aSize == 0) { return ""; }
			if (aSize < 1024) { return aSize + " B"; }
			else if (aSize < 1024 * 1024) { return (aSize / 1024).ToString() + " KB"; }
			else if (aSize < 1024 * 1024 * 1024) { return (aSize / (1024 * 1024)).ToString() + " MB"; }
			else { return (aSize / (1024 * 1024 * 1024)).ToString() + " GB"; }
		}

		public static string Speed2Human(double aSpeed)
		{
			if (aSpeed == 0) { return ""; }
			if (aSpeed < 1024) { return aSpeed.ToString("0.00") + " B"; }
			else if (aSpeed < 1024 * 1024) { return (aSpeed / 1024).ToString("0.00") + " KB"; }
			else { return (aSpeed / (1024 * 1024)).ToString("0.00") + " MB"; }
		}

		public static string Time2Human(Int64 aTime)
		{
			string str = "";
			if (aTime == Int64.MaxValue) { return str; }

			int buff = 0;

			if (aTime > 86400)
			{
				buff = (int)(aTime / 86400);
				str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
				aTime -= buff * 86400;
			}
			//else { str += "00:"; }

			if (aTime > 3600)
			{
				buff = (int)(aTime / 3600);
				str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
				aTime -= buff * 3600;
			}
			//else { str += "00:"; }

			if (aTime > 60)
			{
				buff = (int)(aTime / 60);
				str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
				aTime -= buff * 60;
			}
			//else { str += "00:"; }

			if (aTime > 0)
			{
				buff = (int)aTime;
				str += buff >= 10 ? "" + buff : "0" + buff;
				aTime -= buff;
			}
			else { str += "00"; }

			return str;
		}
	}
}
