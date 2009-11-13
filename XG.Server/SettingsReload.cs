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
using System.IO;
using System.Xml.Serialization;
using XG.Core;

namespace XG.Server
{
	[Serializable()]
	public class SettingsReload
	{
		private static SettingsReload instance = null;

		public static SettingsReload Instance
		{
			get
			{
				try
				{
					XmlSerializer ser = new XmlSerializer(typeof(SettingsReload));
					StreamReader sr = new StreamReader("./settingsreload.xml");
					instance = (SettingsReload)ser.Deserialize(sr);
					sr.Close();
				}
				catch (Exception ex)
				{
					XGHelper.Log("SettingsReload.Instance: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					instance = new SettingsReload();
				}
				return instance;
			}
		}

		private SettingsReload()
		{
			this.fileHandler = new string[] {""};
		}

		string[] fileHandler;
		public string[] FileHandler 
		{
			get { return fileHandler; }
			set { fileHandler = value; }
		}
	}
}
		