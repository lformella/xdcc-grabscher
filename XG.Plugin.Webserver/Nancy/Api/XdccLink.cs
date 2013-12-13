// 
//  XdccLink.cs
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
using XG.Model.Domain;
using Nancy.Responses;
using Nancy.Serialization.JsonNet;

namespace XG.Plugin.Webserver.Nancy.Api
{
	public class XdccLink : ApiModule
	{
		public XdccLink()
		{
			Get["/api/{apiKey}/parseXdccLink/{server}/{servername}/{channel}/{bot}/{id}/{file}/"] = _ => {
				if (!IsApiKeyValid(_.apiKey))
				{
					return new JsonResponse(new Result { ReturnValue = -1 }, new JsonNetSerializer());
				}

				// TODO validate
				try
				{
					string serverName = _.server;
					string channelName = _.channel;
					string botName = _.bot;
					int packetId = int.Parse(_.id);

					// checking server
					Server serv = SignalR.Hub.Helper.Servers.Server(serverName);
					if (serv == null)
					{
						SignalR.Hub.Helper.Servers.Add(serverName);
						serv = SignalR.Hub.Helper.Servers.Server(serverName);
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
						pack = new Packet { Id = packetId, Name = _.file };
						tBot.AddPacket(pack);
					}
					pack.Enabled = true;

					IncreaseSuccessCount(_.apiKey);
					return new JsonResponse(new Result { ReturnValue = 1 }, new JsonNetSerializer());
				}
				catch (Exception)
				{
					IncreaseErrorCount(_.apiKey);
					return new JsonResponse(new Result { ReturnValue = 0 }, new JsonNetSerializer());
				}
			};
		}
	}
}
