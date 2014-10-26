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
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
#else
using System.Runtime.InteropServices;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using XG.Config.Properties;
using XG.Business;

namespace XG.Application
{
	class Programm
	{
#if !__MonoCS__
		//http://stackoverflow.com/questions/4646827/on-exit-for-a-console-application
		static bool ConsoleEventCallback(int eventType)
		{
			// http://msdn.microsoft.com/en-us/library/ms683242%28v=vs.85%29.aspx
			// CTRL_C_EVENT = 0
			// CTRL_BREAK_EVENT = 1
			// CTRL_CLOSE_EVENT = 2
			// CTRL_LOGOFF_EVENT = 5
			// CTRL_SHUTDOWN_EVENT = 6
			if (eventType == 0 || eventType == 2 || eventType == 6)
			{
				app.Shutdown(handler);
			}
			return false;
		}
		static ConsoleEventDelegate handler;
		delegate bool ConsoleEventDelegate(int eventType);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
#endif

		static App app;

		public static void Main(string[] args)
		{
#if !__MonoCS__
			handler = new ConsoleEventDelegate(ConsoleEventCallback);
			SetConsoleCtrlHandler(handler, true);
#else
			// http://stackoverflow.com/questions/6546509/detect-when-console-application-is-closing-killed
			var signums = new[]
			{
				Signum.SIGABRT,
				Signum.SIGINT,
				Signum.SIGKILL,
				Signum.SIGQUIT,
				Signum.SIGTERM,
				Signum.SIGSTOP,
				Signum.SIGTSTP
			};

			var signals = new List<UnixSignal>();
			foreach (var signum in signums)
			{
				try
				{
					signals.Add(new UnixSignal(signum));
				}
				catch(Exception)
				{
					// ...
				}
			}
#if !DEBUG
			new Thread (delegate ()
			{
				// Wait for a signal to be delivered
				UnixSignal.WaitAny(signals.ToArray(), -1);
				app.Shutdown("UnixSignal");
			}).Start();
#endif
#endif

			if (string.IsNullOrWhiteSpace(Settings.Default.TempPath))
			{
				Settings.Default.TempPath = Settings.Default.GetAppDataPath() + "tmp";
			}
			if (!Settings.Default.TempPath.EndsWith("" + Path.DirectorySeparatorChar, StringComparison.CurrentCulture))
			{
				Settings.Default.TempPath += Path.DirectorySeparatorChar;
			}
			new DirectoryInfo(Settings.Default.TempPath).Create();

			if (string.IsNullOrWhiteSpace(Settings.Default.ReadyPath))
			{
				Settings.Default.ReadyPath = Settings.Default.GetAppDataPath() + "dl";
			}
			if (!Settings.Default.ReadyPath.EndsWith("" + Path.DirectorySeparatorChar, StringComparison.CurrentCulture))
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
			try
			{
				if ((id == PlatformID.Unix || id == PlatformID.MacOSX) && new UnixUserInfo(UnixEnvironment.UserName).UserId == 0)
				{
					LogManager.GetLogger(typeof(Programm)).Fatal("Sorry, you can't run XG with these permissions. Safety first!");
					Environment.Exit(-1);
				}
			}
			catch (ArgumentException)
			{
				// arch linux fix
				// https://github.com/lformella/xdcc-grabscher/issues/36
			}
#endif

			app = new App();

			app.AddPlugin(new Plugin.Irc.Plugin());
			if (Settings.Default.UseJabberClient)
			{
				app.AddPlugin(new Plugin.Jabber.Plugin());
			}
			if (Settings.Default.UseElasticSearch)
			{
				app.AddPlugin(new Plugin.ElasticSearch.Plugin());
			}
			if (Settings.Default.UseWebserver)
			{
				var webServer = new Plugin.Webserver.Plugin { RrdDB = app.RrdDb };
				webServer.OnShutdown += delegate { app.Shutdown(webServer); };
				app.AddPlugin(webServer);
			}

			app.OnShutdownComplete += delegate { Environment.Exit(0); };
			app.Start(typeof(App).ToString());
		}
	}
}
