// 
//  Programm.cs
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

#if __MonoCS__
using Mono.Unix;
#endif
using System;
using System.IO;
using System.Threading;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using XG.Business.Helper;
using XG.Config.Properties;
using XG.Business;

namespace XG.Application
{
	class Programm
	{
		public static void Main(string[] args)
		{
			if (string.IsNullOrWhiteSpace(Settings.Default.TempPath))
			{
				Settings.Default.TempPath = Settings.Default.GetAppDataPath() + "tmp";
			}
			if (!Settings.Default.TempPath.EndsWith("" + Path.DirectorySeparatorChar))
			{
				Settings.Default.TempPath += Path.DirectorySeparatorChar;
			}
			new DirectoryInfo(Settings.Default.TempPath).Create();

			if (string.IsNullOrWhiteSpace(Settings.Default.ReadyPath))
			{
				Settings.Default.ReadyPath = Settings.Default.GetAppDataPath() + "dl";
			}
			if (!Settings.Default.ReadyPath.EndsWith("" + Path.DirectorySeparatorChar))
			{
				Settings.Default.ReadyPath += Path.DirectorySeparatorChar;
			}
			new DirectoryInfo(Settings.Default.ReadyPath).Create();

			Settings.Default.Save();

			if (File.Exists(Settings.Default.GetAppDataPath() + "log4net.xml"))
			{
				// load settings from file
				XmlConfigurator.Configure(new FileInfo(Settings.Default.GetAppDataPath() + "log4net.xml"));
			}
			else
			{
				// build our own, who logs only fatals to console
				Logger root = ((Hierarchy)LogManager.GetRepository()).Root;

				var lAppender = new ConsoleAppender
				{
					Name = "Console",
					Layout = new PatternLayout("%date{dd-MM-yyyy HH:mm:ss,fff} %5level [%2thread] %line:%logger.%message%n"),
#if DEBUG
					Threshold = Level.Info
#else
					Threshold = Level.Fatal
#endif
				};
				lAppender.ActivateOptions();

				root.AddAppender(lAppender);
				root.Repository.Configured = true;
			}

#if __MonoCS__
			PlatformID id = Environment.OSVersion.Platform;
			// Don't allow running as root on Linux or Mac
			if ((id == PlatformID.Unix || id == PlatformID.MacOSX) && new UnixUserInfo(UnixEnvironment.UserName).UserId == 0)
			{
				LogManager.GetLogger(typeof(Programm)).Fatal("Sorry, you can't run XG with these permissions. Safety first!");
				Environment.Exit(-1);
			}
#endif

			var app = new App();

			app.AddWorker(new Plugin.Irc.Plugin());
			if (Settings.Default.UseJabberClient)
			{
				app.AddWorker(new Plugin.Jabber.Plugin());
			}
			if (Settings.Default.UseElasticSearch)
			{
				app.AddWorker(new Plugin.ElasticSearch.Plugin());
			}
			if (Settings.Default.UseWebserver)
			{
				var webServer = new Plugin.Webserver.Plugin { RrdDB = app.RrdDb };
				app.AddWorker(webServer);
			}

			app.Start("App");

			string shutdownFile = Settings.Default.GetAppDataPath() + "shutdown";
			while (true)
			{
				if (File.Exists(shutdownFile))
				{
					FileSystem.DeleteFile(shutdownFile);
					app.Stop();
					break;
				}
				Thread.Sleep(1000);
			}

			Environment.Exit(0);
		}
	}
}
