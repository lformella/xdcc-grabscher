// 
//  Plugin.cs
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

using System;
using SharpRobin.Core;
using XG.Config.Properties;
using Microsoft.Owin.Hosting;
using XG.Plugin.Webserver.SignalR;
using XG.Plugin.Webserver.SignalR.Hub;

namespace XG.Plugin.Webserver
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		IDisposable _server;
		EventForwarder _eventForwarder;

		public RrdDb RrdDB { get; set; }

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			Helper.Servers = Servers;
			Helper.Files = Files;
			Helper.Searches = Searches;
			Helper.Notifications = Notifications;
			Helper.RrdDb = RrdDB;
			Helper.ApiKeys = ApiKeys;

			var options = new StartOptions("http://*:" + Settings.Default.WebserverPort)
			{
				ServerFactory = "Nowin"
			};
			_server = WebApp.Start<Startup>(options);

			_eventForwarder = new EventForwarder();
			_eventForwarder.Servers = Servers;
			_eventForwarder.Files = Files;
			_eventForwarder.Searches = Searches;
			_eventForwarder.Notifications = Notifications;
			_eventForwarder.ApiKeys = ApiKeys;
			_eventForwarder.Start();
		}

		protected override void StopRun()
		{
			_eventForwarder.Stop();
			_server.Dispose();
		}

		#endregion
	}
}
