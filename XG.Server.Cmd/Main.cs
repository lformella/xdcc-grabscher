//  
//  Copyright(C) 2009 Lars Formella <ich@larsformella.de>
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
using System.Threading;
using XG.Server;
using XG.Server.Backend.MySql;
using XG.Server.Jabber;
using XG.Server.TCP;
using XG.Server.Web;
	
namespace XG.Server.Cmd
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			try
			{
				ServerRunner runner = new ServerRunner();

				if (Settings.Instance.StartTCPServer) { runner.AddServerPlugin(new TCPServer()); }
				if (Settings.Instance.StartWebServer) { runner.AddServerPlugin(new WebServer()); }
				if (Settings.Instance.StartJabberClient) { runner.AddServerPlugin(new JabberClient()); }
				if (Settings.Instance.StartMySqlBackend)
				{
					runner.AddServerPlugin(new MySqlBackend());
					// sleep a minute to let the mysql plugin do the initial stuff
					Thread.Sleep(60000);
				}

/*
				// import routine
				StreamReader reader = new StreamReader("./import");
				string str = reader.ReadToEnd();
				reader.Close();
				HtmlDocument doc = new HtmlDocument();
				doc.LoadHtml(str);
				HtmlNodeCollection col = doc.DocumentNode.SelectNodes("//a");
				string last_server = "";
				int count = 0;
				foreach(HtmlNode node in col)
				{
					string href = node.Attributes["href"].Value;
					if(href.StartsWith("irc://"))
					{
						string[] strs = href.Split(new char[] {'/'});
						string server = strs[2].ToLower();
						string channel = strs[3].ToLower();
						if(last_server != server)
						{
							last_server = server;
							runner.AddServer(server);
							Console.WriteLine("-> " + server);
						}
						List<XGObject> list = runner.GetServersChannels();
						XGObject s = null;
						foreach(XGObject obj in list)
						{
							if(obj.Name == server)
							{
								s = obj;
							}
						}
						runner.AddChannel(s.Guid, channel);
						Console.WriteLine("-> " + server + " - " + channel);
						count++;
					}
				}
				Console.WriteLine("----> " + count);
*/

				runner.Start();
			}
			catch (Exception ex)
			{
				// die bitch, but stay there
				Console.WriteLine(ex.ToString());
				Console.ReadLine();
			}
		}
	}
}
