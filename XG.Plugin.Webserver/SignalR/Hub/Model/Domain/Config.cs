// 
//  Config.cs
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

using Newtonsoft.Json;
using System.Collections.Generic;
using XG.Config.Properties;

namespace XG.Plugin.Webserver.SignalR.Hub.Model.Domain
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Config
	{
		#region VARIABLES

		public bool AutoRegisterNickserv { get; set; }
		public string ElasticSearchHost { get; set; }
		public int ElasticSearchPort { get; set; }
		public bool EnableMultiDownloads { get; set; }
		public IEnumerable<FileHandler> FileHandlers { get; set; }
		public string IrcNick { get; set; }
		public string IrcPasswort { get; set; }
		public string IrcRegisterEmail { get; set; }
		public string JabberPassword { get; set; }
		public string JabberServer { get; set; }
		public string JabberUser { get; set; }
		public int MaxDownloadSpeedInKB { get; set; }
		public string Password { get; set; }
		public string ReadyPath { get; set; }
		public string TempPath { get; set; }
		public bool UseElasticSearch { get; set; }
		public bool UseJabberClient { get; set; }
		public bool UseWebserver { get; set; }
		public int WebserverPort { get; set; }
		public int MaxDownloads { get; set; }

		#endregion
	}
}
