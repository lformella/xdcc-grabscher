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
using log4net;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Reflection;
using XG.Config.Properties;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	public class ExternalHub : Microsoft.AspNet.SignalR.Hub
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
		{
			DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
		};

		public void ParseXdccLink(string aLink)
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

		public Model.Domain.Result LoadByGuid(Guid aGuid, bool aOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			var search = Helper.Searches.All.SingleOrDefault(s => s.Guid == aGuid);
			if (search != null)
			{
				return LoadByParameter(search.Name, search.Size, aOfflineBots, aCount, aPage, aSortBy, aSort);
			}
			return new Model.Domain.Result { Total = 0, Results = new List<Hub.Model.Domain.AObject>() };
		}

		public Model.Domain.Result LoadByParameter(string aSearch, Int64 aSize, bool aOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			aPage--;

			try
			{
				var url = Helper.RemoteSettings.ExternalSearch.Url
					.Replace("##VERSION##", Settings.Default.XgVersion)
					.Replace("##START##", "" + aPage * aCount)
					.Replace("##LIMIT##", "" + aCount)
					.Replace("##MIN_SIZE##", "" + aSize)
					.Replace("##BOT_STATE##", "" + (aOfflineBots ? 3 : 0))
					.Replace("##SORT_BY##", aSortBy.Length > 1 ? aSortBy.Substring(0, 1).ToLower() + aSortBy.Substring(1) : "")
					.Replace("##SORT##", aSort)
					.Replace("##SEARCH##", aSearch);

				var req = WebRequest.Create(new Uri(url));

				var response = req.GetResponse();
				StreamReader sr = new StreamReader(response.GetResponseStream());
				string text = sr.ReadToEnd();
				response.Close();

				var result = JsonConvert.DeserializeObject<Hub.Model.Domain.ExternalSearch>(text, _jsonSerializerSettings);

				if (result.Data.Count() > 0)
				{
					return new Model.Domain.Result { Total = result.Count, Results = result.Data };
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("OnSearchExternal(" + aSearch + ") cant load external search", ex);
			}

			return new Model.Domain.Result { Total = 0, Results = new List<Hub.Model.Domain.AObject>() };
		}
	}
}
