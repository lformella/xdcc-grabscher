// 
//  Importer.cs
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

using HtmlAgilityPack;

using log4net;

using XG.Core;
using XG.Server.Helper;

namespace XG.Server.Plugin.Backend.MySql
{
	public class Importer
	{
		static readonly ILog myLog = LogManager.GetLogger(typeof(Importer));

		XG.Core.Servers _servers;

		public event ObjectsDelegate ObjectAddedEvent;

		public Importer (XG.Core.Servers aRootObject)
		{
			_servers = aRootObject;
		}

		public void Import(string aFile)
		{
#if !WINDOWS			
			// import routine
			string str = FileSystem.ReadFile(aFile);
			if(str != "")
			{
				return;
			}

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

					XG.Core.Server s = GetServer(server);
					if(s == null)
					{
						_servers.Add(server);
						s = GetServer(server);
						ObjectAddedEvent(_servers, s);
						myLog.Debug("-> " + server);
					}

					if(GetChannelFromServer(s, channel) == null)
					{
						s.AddChannel(channel);
						Channel c = GetChannelFromServer(s, channel);
						ObjectAddedEvent(s, c);
						myLog.Debug("-> " + server + " - " + channel);
					}
				}
			}
#endif
		}

		XG.Core.Server GetServer(string aServerName)
		{
			foreach(AObject obj in _servers.All)
			{
				if(obj.Name == aServerName)
				{
					return (XG.Core.Server)obj;
				}
			}
			return null;
		}

		Channel GetChannelFromServer(XG.Core.Server aServer, string aChannelName)
		{
			foreach(Channel chan in aServer.Channels)
			{
				if((chan.Name == aChannelName || chan.Name == "#" + aChannelName) && chan.ParentGuid == aServer.Guid)
				{
					return chan;
				}
			}
			return null;
		}
	}
}
