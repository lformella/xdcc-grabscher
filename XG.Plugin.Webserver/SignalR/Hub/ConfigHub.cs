// 
//  ConfigHub.cs
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

using XG.Config.Properties;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	public class ConfigHub : Microsoft.AspNet.SignalR.Hub
	{
		public SignalR.Hub.Model.Domain.Config Load()
		{
			var config = new SignalR.Hub.Model.Domain.Config();
			config.AutoRegisterNickserv = Settings.Default.AutoRegisterNickserv;
			config.ElasticSearchHost = Settings.Default.ElasticSearchHost;
			config.ElasticSearchPort = Settings.Default.ElasticSearchPort;
			config.FileHandlers = Settings.Default.GetFileHandlers();
			config.IrcNick = Settings.Default.IrcNick;
			config.IrcPasswort = Settings.Default.IrcPasswort;
			config.IrcRegisterEmail = Settings.Default.IrcRegisterEmail;
			config.JabberPassword = Settings.Default.JabberPassword;
			config.JabberServer = Settings.Default.JabberServer;
			config.JabberUser = Settings.Default.JabberUser;
			config.MaxDownloadSpeedInKB = Settings.Default.MaxDownloadSpeedInKB;
			config.Password = Settings.Default.Password;
			config.ReadyPath = Settings.Default.ReadyPath;
			config.TempPath = Settings.Default.TempPath;
			config.UseElasticSearch = Settings.Default.UseElasticSearch;
			config.UseJabberClient = Settings.Default.UseJabberClient;
			config.UseWebserver = Settings.Default.UseWebserver;
			config.WebserverPort = Settings.Default.WebserverPort;
			config.MaxDownloads = Settings.Default.MaxDownloads;
			return config;
		}

		public void Save(SignalR.Hub.Model.Domain.Config aConfig)
		{
			Settings.Default.AutoRegisterNickserv = aConfig.AutoRegisterNickserv;
			Settings.Default.ElasticSearchHost = aConfig.ElasticSearchHost;
			Settings.Default.ElasticSearchPort = aConfig.ElasticSearchPort;
			Settings.Default.SetFileHandlers(aConfig.FileHandlers);
			Settings.Default.IrcNick = aConfig.IrcNick;
			Settings.Default.IrcPasswort = aConfig.IrcPasswort;
			Settings.Default.IrcRegisterEmail = aConfig.IrcRegisterEmail;
			Settings.Default.JabberPassword = aConfig.JabberPassword;
			Settings.Default.JabberServer = aConfig.JabberServer;
			Settings.Default.JabberUser = aConfig.JabberUser;
			Settings.Default.MaxDownloadSpeedInKB = aConfig.MaxDownloadSpeedInKB;
			Settings.Default.Password = aConfig.Password;
			Settings.Default.ReadyPath = aConfig.ReadyPath;
			Settings.Default.TempPath = aConfig.TempPath;
			Settings.Default.UseElasticSearch = aConfig.UseElasticSearch;
			Settings.Default.UseJabberClient = aConfig.UseJabberClient;
			Settings.Default.UseWebserver = aConfig.UseWebserver;
			Settings.Default.WebserverPort = aConfig.WebserverPort;
			Settings.Default.MaxDownloads = aConfig.MaxDownloads;
			Settings.Default.Save();
		}
	}
}
