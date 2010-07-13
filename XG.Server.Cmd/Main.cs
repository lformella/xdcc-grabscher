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
using XG.Server;
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
				runner.Start();

				if (Settings.Instance.StartTCPServer) { runner.AddServerPlugin(new TCPServer()); }
				if (Settings.Instance.StartWebServer) { runner.AddServerPlugin(new WebServer()); }
				if (Settings.Instance.StartJabberClient) { runner.AddServerPlugin(new JabberClient()); }
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
