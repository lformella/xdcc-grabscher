// 
//  Json.cs
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
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace XG.Server.Plugin.General.Webserver
{
	public class Json
	{
		public static string Serialize<T> (T t)
		{
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
			MemoryStream ms = new MemoryStream();
			ser.WriteObject(ms, t);
			string jsonString = Encoding.UTF8.GetString(ms.ToArray());
			ms.Close();

			string p = @"\\/Date\(([0-9-]+)(((\+|-)[0-9]+)|)\)\\/";
			MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertJsonDateToDateString);
			Regex reg = new Regex(p);
			jsonString = reg.Replace(jsonString, matchEvaluator);
			return jsonString;
		}

		static string ConvertJsonDateToDateString (Match m)
		{
			string result = string.Empty;
			DateTime dt = new DateTime(1970, 1, 1);
			dt = dt.AddMilliseconds(long.Parse(m.Groups[1].Value));
			result = dt.ToString("HH:mm:ss dd.MM.yyyy");
			return result;
		}
	}
}

