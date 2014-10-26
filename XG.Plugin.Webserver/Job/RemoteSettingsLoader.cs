// 
//  RemoteSettingsLoader.cs
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
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Quartz;
using XG.Config.Properties;

namespace XG.Plugin.Webserver.Job
{
	public class RemoteSettingsLoader : IJob
	{
		public void Execute (IJobExecutionContext context)
		{
			try
			{
				//var uri = new Uri("/Users/lars/Projekte/C#/xdcc-grabscher/settings.json");
				var uri = new Uri("https://raw.githubusercontent.com/lformella/xdcc-grabscher/master/settings.json");
				var req = WebRequest.Create(uri);
				var response = req.GetResponse();
				StreamReader sr = new StreamReader(response.GetResponseStream());
				string settings = sr.ReadToEnd();
				response.Close();

				var result = JsonConvert.DeserializeObject<RemoteSettings>(settings);
				if (ToInt(Settings.Default.XgVersion) < ToInt(result.Version.Latest))
				{
					result.Version.ShowWarning = true;
				}

				SignalR.Hub.Helper.RemoteSettings = result;
				Nancy.Helper.RemoteSettings = result;
			}
			catch(Exception) {}
		}

		int ToInt(string aVersion)
		{
			return int.Parse(aVersion.Replace(".", ""));
		}
	}
}
