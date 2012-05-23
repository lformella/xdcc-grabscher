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

#if !WINDOWS
using Mono.Unix;
#endif
using System;
using System.IO;
using System.Threading;
using log4net.Config;
using XG.Server;
using XG.Server.Backend.File;
using XG.Server.Backend.MySql;
using XG.Server.Jabber;
using XG.Server.Web;

namespace XG.Server.Cmd
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			if(File.Exists("./log4net"))
			{
				// BasicConfigurator replaced with XmlConfigurator.
				XmlConfigurator.Configure(new System.IO.FileInfo("./log4net"));
			}
			else
			{
				// Set up a simple configuration that logs on the console.
				BasicConfigurator.Configure();
			}

#if !WINDOWS
			PlatformID id  = Environment.OSVersion.Platform;
			// Don't allow running as root on Linux or Mac
			if ((id == PlatformID.Unix || id == PlatformID.MacOSX) && new UnixUserInfo (UnixEnvironment.UserName).UserId == 0)
			{
				Console.WriteLine ("Sorry, you can't run XG with these permissions. Safety first!");
				Environment.Exit (-1);
			}
#endif

			ServerRunner runner = new ServerRunner();

			if (Settings.Instance.StartMySqlBackend) { runner.AddServerPlugin(new MySqlBackend(runner)); }
			else { runner.AddServerPlugin(new FileBackend(runner)); }

			if (Settings.Instance.StartWebServer) { runner.AddServerPlugin(new WebServer()); }
			if (Settings.Instance.StartJabberClient) { runner.AddServerPlugin(new JabberClient()); }

			runner.Start();
		}
	}
}
