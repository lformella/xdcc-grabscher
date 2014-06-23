// 
//  Plugin.cs
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
using HtmlAgilityPack;
using log4net;
using XG.Model.Domain;
using XG.Config.Properties;
using System.IO;

namespace XG.Plugin.Import
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(Plugin));

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			// import routine
			string file = Settings.Default.GetAppDataPath() + "import";
			string str = "";
			if (System.IO.File.Exists(file))
			{
				try
				{
					using (var reader = new StreamReader(file))
					{
						str = reader.ReadToEnd();
						reader.Close();
						if(String.IsNullOrWhiteSpace(str))
						{
							return;
						}
					}
				}
				catch (Exception)
				{
					return;
				}
			}

			var doc = new HtmlDocument();
			doc.LoadHtml(str);

			HtmlNodeCollection col = doc.DocumentNode.SelectNodes("//a");
			foreach(HtmlNode node in col)
			{
				string href = node.Attributes["href"].Value;
				if (href.StartsWith("irc://", StringComparison.CurrentCulture))
				{
					string[] strs = href.Split(new[] { '/' });
					string serverName = strs [2].ToLower();
					string channelName = strs [3].ToLower();

					Server s = Servers.Server(serverName);
					if (s == null)
					{
						Servers.Add(serverName);
						s = Servers.Server(serverName);
						_log.Debug("-> " + serverName);
					}

					Channel c = s.Channel(channelName);
					if (c == null)
					{
						s.AddChannel(channelName);
						_log.Debug("-> " + serverName + " - " + channelName);
					}
				}
			}
		}

		#endregion
	}
}
