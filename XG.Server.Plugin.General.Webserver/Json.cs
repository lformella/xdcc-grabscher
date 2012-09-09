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
	/// <summary>
	/// JSON Serialization and Deserialization Assistant Class
	/// </summary>
	public class Json
	{
		/// <summary>
		/// JSON Serialization
		/// </summary>
		public static string Serialize<T> (T t)
		{
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
			MemoryStream ms = new MemoryStream();
			ser.WriteObject(ms, t);
			string jsonString = Encoding.UTF8.GetString(ms.ToArray());
			ms.Close();
			//Replace Json Date String
			string p = @"\\/Date\(([0-9-]+)\)\\/";
			MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertJsonDateToDateString);
			Regex reg = new Regex(p);
			jsonString = reg.Replace(jsonString, matchEvaluator);
			return jsonString;
		}

		/// <summary>
		/// JSON Deserialization
		/// </summary>
		public static T Deserialize<T> (string jsonString)
		{
			//Convert "yyyy-MM-dd HH:mm:ss" String as "\/Date(1319266795390+0800)\/"
			string p = @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}";
			MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertDateStringToJsonDate);
			Regex reg = new Regex(p);
			jsonString = reg.Replace(jsonString, matchEvaluator);
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
			MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
			T obj = (T) ser.ReadObject(ms);
			return obj;
		}
  
		/// <summary>
		/// Convert Serialization Time /Date(1319266795390+0800) as String
		/// </summary>
		static string ConvertJsonDateToDateString (Match m)
		{
			string result = string.Empty;
			DateTime dt = new DateTime(1970, 1, 1);
			dt = dt.AddMilliseconds(long.Parse(m.Groups[1].Value));
			result = dt.ToString("yyyy-MM-dd HH:mm:ss");
			return result;
		}
 
		/// <summary>
		/// Convert Date String as Json Time
		/// </summary>
		static string ConvertDateStringToJsonDate (Match m)
		{
			string result = string.Empty;
			DateTime dt = DateTime.Parse(m.Groups[0].Value);
			TimeSpan ts = dt - DateTime.Parse("1970-01-01");
			result = string.Format("\\/Date({0})\\/", ts.TotalMilliseconds);
			return result;
		}

		static Regex myClearRegex = new Regex(@"[^A-Za-z0-9äÄöÖüÜß _.\[\]\{\}\(\)-]");
		static string ClearString(string aString)
		{
			string str = myClearRegex.Replace(aString, "");
			str = str.Replace("Ä", "&Auml;");
			str = str.Replace("ä", "&auml;");
			str = str.Replace("Ö", "&Ouml;");
			str = str.Replace("ö", "&ouml;");
			str = str.Replace("Ü", "&Uuml;");
			str = str.Replace("ü", "&uuml;");
			str = str.Replace("ß", "&szlig;");
			return str;
		}
	}
}

