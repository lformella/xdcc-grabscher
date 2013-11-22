// 
//  ExternalHub.cs
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
using System.Linq;
using XG.Model.Domain;
using XG.Plugin.Webserver.SignalR.Hub;
using System.Collections.Generic;
using XG.Model;
using log4net;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Reflection;
using XG.Config.Properties;
using XG.Plugin.Webserver.SignalR.Hub.Model;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	public class ExternalHub : AObjectHub
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
		{
			DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
		};

		#region Client Handling

		protected override void AddClient(Client aClient)
		{
		}

		protected override void RemoveClient(string connectionId)
		{
		}

		protected override Client GetClient(string connectionId)
		{
			return null;
		}

		#endregion

		public void ParseXdccLink(String aLink)
		{
			string[] link = aLink.Substring(7).Split('/');
			string serverName = link[0];
			string channelName = link[2];
			string botName = link[3];
			int packetId = int.Parse(link[4].Substring(1));

			// checking server
			Server serv = Helper.Servers.Server(serverName);
			if (serv == null)
			{
				Helper.Servers.Add(serverName);
				serv = Helper.Servers.Server(serverName);
			}
			serv.Enabled = true;

			// checking channel
			Channel chan = serv.Channel(channelName);
			if (chan == null)
			{
				serv.AddChannel(channelName);
				chan = serv.Channel(channelName);
			}
			chan.Enabled = true;

			// checking bot
			Bot tBot = chan.Bot(botName);
			if (tBot == null)
			{
				tBot = new Bot { Name = botName };
				chan.AddBot(tBot);
			}

			// checking packet
			Packet pack = tBot.Packet(packetId);
			if (pack == null)
			{
				pack = new Packet { Id = packetId, Name = link[5] };
				tBot.AddPacket(pack);
			}
			pack.Enabled = true;
		}

		public IEnumerable<Hub.Model.Domain.ExternalSearch> Search (string search)
		{
			var objects = new List<Hub.Model.Domain.ExternalSearch>();

			int start = 0;
			int limit = 25;
			do
			{
				try
				{
					var uri = new Uri("http://xg.bitpir.at/index.php?show=search&action=external&xg=" + Settings.Default.XgVersion + "&start=" + start + "&limit=" + limit + "&search=" + search);
					var req = HttpWebRequest.Create(uri);

					var response = req.GetResponse();
					StreamReader sr = new StreamReader(response.GetResponseStream());
					string text = sr.ReadToEnd();
					response.Close();

					var results = JsonConvert.DeserializeObject<Hub.Model.Domain.ExternalSearch[]>(text, _jsonSerializerSettings);

					if (results.Length > 0)
					{
						objects.AddRange(results);
					}

					if (results.Length == 0 || results.Length < limit)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					Log.Fatal("OnSearchExternal(" + search + ") cant load external search", ex);
					break;
				}
				start += limit;
			} while (true);

			return objects;

			
			/*int length;
			var objects = FilterAndLoadObjects<Model.Domain.File>(Helper.Files.All, aCount, aPage, aSortBy, aSort, out length);
			return new Model.Domain.Result { Total = length, Results = objects };*/
		}
	}
}
