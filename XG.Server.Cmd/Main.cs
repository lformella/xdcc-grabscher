// 
//  Main.cs
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

#if !WINDOWS
using Mono.Unix;
#endif

using System;
using System.IO;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;

using XG.Server;
using XG.Server.Plugin;

namespace XG.Server.Cmd
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			if(File.Exists("./log4net"))
			{
				// load settings from file
				XmlConfigurator.Configure(new FileInfo("./log4net"));
			}
			else
			{
				// build our own, who logs only fatals to console
				Logger root = ((Hierarchy)LogManager.GetRepository()).Root;

				ConsoleAppender lAppender = new ConsoleAppender();
				lAppender.Name = "Console";
				lAppender.Layout = new
				log4net.Layout.PatternLayout("%date{dd-MM-yyyy HH:mm:ss,fff} %5level [%2thread] %message (%logger{1}:%line)%n");
				lAppender.Threshold = log4net.Core.Level.Fatal;
				lAppender.ActivateOptions();

				root.AddAppender(lAppender);
				root.Repository.Configured = true;
			}

#if !WINDOWS
			PlatformID id  = Environment.OSVersion.Platform;
			// Don't allow running as root on Linux or Mac
			if ((id == PlatformID.Unix || id == PlatformID.MacOSX) && new UnixUserInfo (UnixEnvironment.UserName).UserId == 0)
			{
				LogManager.GetLogger(typeof(Main)).Fatal("Sorry, you can't run XG with these permissions. Safety first!");
				Environment.Exit (-1);
			}
#endif

			Main instance = new Main();

			ABackendPlugin backend = null;
			if (Settings.Instance.StartMySqlBackend)
			{
				backend = new XG.Server.Plugin.Backend.MySql.BackendPlugin();
			}
			else
			{
				backend = new XG.Server.Plugin.Backend.File.BackendPlugin();
			}
			instance.AddBackendPlugin(backend);

			if (Settings.Instance.StartWebServer)
			{
				instance.AddPlugin(new XG.Server.Plugin.General.Webserver.Plugin());
			}
			if (Settings.Instance.StartJabberClient)
			{
				instance.AddPlugin(new XG.Server.Plugin.General.Jabber.Plugin());
			}

			instance.Start();
		}
	}
}
