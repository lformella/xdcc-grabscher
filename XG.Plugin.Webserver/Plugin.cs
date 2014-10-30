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
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Owin.Hosting;
using SharpRobin.Core;
using XG.Config.Properties;

namespace XG.Plugin.Webserver
{
	[Export(typeof(APlugin))]
	[ExportMetadata(PluginMetaData.NAME, "XG.Plugin.Webserver")]
	[ExportMetadata(PluginMetaData.DESCRIPTION, "This plugin enables the webfrontend.")]
	[ExportMetadata(PluginMetaData.VERSION, "3.3.0.0")]
	[ExportMetadata(PluginMetaData.AUTHOR, "Lars Formella")]
	[ExportMetadata(PluginMetaData.WEBSITE, "http://xg.bitpir.at/")]
	public class Plugin : APlugin
	{
		#region VARIABLES

		IDisposable _server;
		SignalR.EventForwarder _eventForwarder;

		public RrdDb RrdDB { get; set; }

		SHA256Managed _sha256 = new SHA256Managed();

		#endregion

		string Hash(string aStr = null)
		{
			byte[] bytes = aStr == null ? BitConverter.GetBytes(new Random().Next()) : Encoding.UTF8.GetBytes(aStr);
			return BitConverter.ToString(_sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
		}

		#region AWorker

		protected override void StartRun()
		{
			Search.Packets.Servers = Servers;
			Search.Packets.Initialize();

			AddRepeatingJob(typeof(Job.SearchUpdater), "SearchUpdater", "WebserverPlugin", Settings.Default.TakeSnapshotTimeInMinutes * 60,
				new JobItem("Searches", Searches));

			string salt = Hash();
			string passwortHash = Hash(salt + Settings.Default.Password + salt);

			SignalR.Hub.Helper.Servers = Servers;
			SignalR.Hub.Helper.Files = Files;
			SignalR.Hub.Helper.Searches = Searches;
			SignalR.Hub.Helper.Notifications = Notifications;
			SignalR.Hub.Helper.RrdDb = RrdDB;
			SignalR.Hub.Helper.ApiKeys = ApiKeys;
			SignalR.Hub.Helper.PasswortHash = passwortHash;

			Nancy.Helper.Servers = Servers;
			Nancy.Helper.Files = Files;
			Nancy.Helper.Searches = Searches;
			Nancy.Helper.ApiKeys = ApiKeys;
			Nancy.Helper.Salt = salt;
			Nancy.Helper.PasswortHash = passwortHash;
			Nancy.Helper.OnShutdown += FireShutdown;

			var options = new StartOptions("http://*:" + Settings.Default.WebserverPort)
			{
				ServerFactory = "Nowin"
			};
			_server = WebApp.Start<Startup>(options);

			_eventForwarder = new SignalR.EventForwarder();
			_eventForwarder.Servers = Servers;
			_eventForwarder.Files = Files;
			_eventForwarder.Searches = Searches;
			_eventForwarder.Notifications = Notifications;
			_eventForwarder.ApiKeys = ApiKeys;
			_eventForwarder.Start();

			var settings = new RemoteSettings { Version = new Version(), ExternalSearch = new ExternalSearch { Enabled = false } };
			SignalR.Hub.Helper.RemoteSettings = settings;
			Nancy.Helper.RemoteSettings = settings;
			AddRepeatingJob(typeof(Job.RemoteSettingsLoader), "RemoteSettingsLoader", "WebserverPlugin", 60 * 60 * 24);
		}

		protected override void StopRun()
		{
			Nancy.Helper.OnShutdown -= FireShutdown;
			_eventForwarder.Stop();
			_server.Dispose();
		}

		#endregion
	}
}
