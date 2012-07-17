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

/**/

using System.IO;
using HtmlAgilityPack;
using log4net;
using XG.Core;

namespace XG.Server.Backend.MySql
{
	public class Importer
	{
		private static readonly ILog myLog = LogManager.GetLogger(typeof(MySqlBackend));

		private RootObject myRootObject;

		public event ObjectObjectDelegate ObjectAddedEvent;

		public Importer (RootObject aRootObject)
		{
			this.myRootObject = aRootObject;
		}

		public void Import(string aFile)
		{
			if(!File.Exists(aFile))
			{
				return;
			}
#if !WINDOWS			
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
						this.myRootObject.AddServer(server);
						s = this.GetServer(server);
						this.ObjectAddedEvent(this.myRootObject, s);
						myLog.Debug("-> " + server);
					}

					if(this.GetChannelFromServer(s, channel) == null)
					{
						s.AddChannel(channel);
						XGChannel c = this.GetChannelFromServer(s, channel);
						this.ObjectAddedEvent(s, c);
						myLog.Debug("-> " + server + " - " + channel);
					}
					//Thread.Sleep(500);
				}
			}
#endif
		}

		private XGServer GetServer(string aServerName)
		{
			foreach(XGObject obj in this.myRootObject.Servers)
			{
				if(obj.Name == aServerName)
				{
					return (XGServer)obj;
				}
			}
			return null;
		}

		private XGChannel GetChannelFromServer(XGServer aServer, string aChannelName)
		{
			foreach(XGChannel chan in aServer.Channels)
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

/**/
