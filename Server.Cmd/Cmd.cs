// 
//  Cmd.cs
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

#if !WINDOWS
using Mono.Unix;
#endif

using System;
using System.IO;
using System.Threading;

using XG.Server.Plugin;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace XG.Server.Cmd
{
	class Cmd
	{
		public static void Main (string[] args)
		{
			if (File.Exists(Settings.Instance.AppDataPath + "log4net"))
			{
				// load settings from file
				XmlConfigurator.Configure(new FileInfo(Settings.Instance.AppDataPath + "log4net"));
			}
			else
			{
				// build our own, who logs only fatals to console
				Logger root = ((Hierarchy)LogManager.GetRepository()).Root;

				var lAppender = new ConsoleAppender
				{
					Name = "Console",
					Layout = new PatternLayout("%date{dd-MM-yyyy HH:mm:ss,fff} %5level [%2thread] %line:%logger.%message%n"),
					Threshold = Level.Fatal
				};
				lAppender.ActivateOptions();

				root.AddAppender(lAppender);
				root.Repository.Configured = true;
			}

#if !WINDOWS
			PlatformID id = Environment.OSVersion.Platform;
			// Don't allow running as root on Linux or Mac
			if ((id == PlatformID.Unix || id == PlatformID.MacOSX) && new UnixUserInfo(UnixEnvironment.UserName).UserId == 0)
			{
				LogManager.GetLogger(typeof(Main)).Fatal("Sorry, you can't run XG with these permissions. Safety first!");
				Environment.Exit(-1);
			}
#endif

			var instance = new Main();

			instance.AddBackendPlugin(new Plugin.Backend.File.BackendPlugin());

			if (Settings.Instance.UseWebServer)
			{
				instance.AddWorker(new Plugin.General.Webserver.Plugin());
			}
			if (Settings.Instance.UseJabberClient)
			{
				instance.AddWorker(new Plugin.General.Jabber.Plugin());
			}
			if (Settings.Instance.UseElasticSearch)
			{
				instance.AddWorker(new Plugin.General.ElasticSearch.Plugin());
			}

			instance.Start();

			string shutdownFile = Settings.Instance.AppDataPath + "shutdown";
			while (true)
			{
				if (File.Exists(shutdownFile))
				{
					try
					{
						File.Delete(shutdownFile);
					}
					catch (Exception ex)
					{
						LogManager.GetLogger(typeof (Main)).Fatal("Cant delete shutdown file", ex);
					}
					instance.Stop();
					break;
				}
				Thread.Sleep(1000);
			}

			Environment.Exit(0);
		}
	}
}
