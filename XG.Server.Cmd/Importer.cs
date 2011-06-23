//  
//  Copyright (C) 2011 Lars Formella <ich@larsformella.de>
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

/** /

using System;
using System.Collections.Generic;
using System.IO;
using HtmlAgilityPack;
using XG.Core;
using System.Threading;

namespace XG.Server.Cmd
{
	public class Importer
	{
		private ServerRunner myRunner;

		public Importer (ServerRunner aRunner)
		{
			this.myRunner = aRunner;
		}

		public void Import(string aFile)
		{
			// import routine
			StreamReader reader = new StreamReader(aFile);

			string str = reader.ReadToEnd();
			reader.Close();

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(str);

			HtmlNodeCollection col = doc.DocumentNode.SelectNodes("//a");
			foreach(HtmlNode node in col)
			{
				string href = node.Attributes["href"].Value;
				if(href.StartsWith("irc://"))
				{
					string[] strs = href.Split(new char[] {'/'});
					string server = strs[2].ToLower();
					string channel = strs[3].ToLower();

					XGServer s = this.GetServer(server);
					if(s == null)
					{
						this.myRunner.AddServer(server);
						s = this.GetServer(server);
						Console.WriteLine("-> " + server);
					}

					if(this.GetChannelFromServer(s.Guid, channel) == null)
					{
						this.myRunner.AddChannel(s.Guid, channel);
						Console.WriteLine("-> " + server + " - " + channel);
					}
					//Thread.Sleep(500);
				}
			}
		}

		private XGServer GetServer(string aServerName)
		{
			List<XGObject> list = this.myRunner.GetServersChannels();
			foreach(XGObject obj in list)
			{
				if(obj.Name == aServerName)
				{
					return (XGServer)obj;
				}
			}
			return null;
		}

		private XGChannel GetChannelFromServer(Guid aServerGuid, string aChannelName)
		{
			List<XGObject> list = this.myRunner.GetServersChannels();
			foreach(XGObject obj in list)
			{
				if(obj.Name == aChannelName && obj.ParentGuid == aServerGuid)
				{
					return (XGChannel)obj;
				}
			}
			return null;
		}
	}
}

/**/
