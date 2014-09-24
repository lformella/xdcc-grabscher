// 
//  Packets.cs
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
using System.Collections.Generic;
using System.Linq;
using Nancy.ModelBinding;
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Webserver.Nancy.Api
{
	public class Packets : ApiModule
	{
		public Packets()
		{
			InitializeGet(Helper.Servers, "packets");
			InitializeGetAll(Helper.Servers, "packets");
			InitializeEnable(Helper.Servers, "packets");

            Put["/packets", true] = async (_, ct) =>
			{
				Request.PacketDownload request;
				try
				{
					var config = new BindingConfig { BodyOnly = true, IgnoreErrors = false };
					request = this.Bind<Request.PacketDownload>(config);
					request.ApiKey = Guid.Parse(Context.CurrentUser.UserName);

					var results = Validate(request);
					if (results.Count > 0)
					{
						return CreateErrorResponseAndUpdateApiKey((from result in results select result.ErrorMessage).Implode(", "));
					}
				}
				catch (Exception ex)
				{
					return CreateErrorResponseAndUpdateApiKey(ex.Message);
				}

				return ExecuteDownloadRequest(request);
			};
		}

		protected override object SearchObjects(AObjects aObjects)
		{
			var request = new Request.PacketSearch();
			request.ApiKey = Guid.Parse(Context.CurrentUser.UserName);
			try
			{
				request.Page = Request.Query.page;
			}
			catch (Exception)
			{
				request.Page = 1;
			}
			try
			{
				request.MaxResults = Request.Query.maxResults;
			}
			catch (Exception)
			{
				request.MaxResults = 20;
			}

			request.SearchTerm = Request.Query.searchTerm;
			request.ShowOfflineBots = Request.Query.showOfflineBots;
			request.Sort = Request.Query.sort;
			request.SortBy = Request.Query.sortBy;

			if (string.IsNullOrEmpty(request.SortBy))
			{
				request.SortBy = "Name";
			}
			if (string.IsNullOrEmpty(request.Sort))
			{
				request.Sort = "asc";
			}

			var results = Validate(request);
			if (results.Count > 0)
			{
				return CreateErrorResponseAndUpdateApiKey((from result in results select result.ErrorMessage).Implode(", "));
			}

			return ExecuteSearchRequest(request);
		}

		object ExecuteSearchRequest(Request.PacketSearch request)
		{
			try
			{
				var result = new Result.Objects();

				var packets =
					(
						from server in Helper.Servers.All
						from channel in server.Channels
						from bot in channel.Bots where (request.ShowOfflineBots || bot.Connected)
						from packet in bot.Packets where packet.Name.ContainsAll(request.SearchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) select packet
					).ToList();
				int length;

				result.Results = Helper.FilterAndLoadObjects<Nancy.Api.Model.Domain.Packet>(packets, request.MaxResults, request.Page, request.SortBy, request.Sort, out length);
				result.ResultCount = length;

				return CreateSuccessResponseAndUpdateApiKey(result);
			}
			catch (Exception ex)
			{
				return CreateErrorResponseAndUpdateApiKey(ex.Message);
			}
		}

		object ExecuteDownloadRequest(Request.PacketDownload request)
		{
			try
			{
				// checking server
				Server serv = Helper.Servers.Server(request.Server);
				if (serv == null)
				{
					Helper.Servers.Add(request.Server);
					serv = Helper.Servers.Server(request.Server);
				}
				serv.Enabled = true;

				// checking channel
				Channel chan = serv.Channel(request.Channel);
				if (chan == null)
				{
					serv.AddChannel(request.Channel);
					chan = serv.Channel(request.Channel);
				}
				chan.Enabled = true;

				// checking bot
				Bot tBot = chan.Bot(request.Bot);
				if (tBot == null)
				{
					tBot = new Bot { Name = request.Bot };
					chan.AddBot(tBot);
				}

				// checking packet
				Packet pack = tBot.Packet(request.PacketId);
				if (pack == null)
				{
					pack = new Packet { Id = request.PacketId, Name = request.PacketName };
					tBot.AddPacket(pack);
				}
				pack.Enabled = true;

				return CreateSuccessResponseAndUpdateApiKey();
			}
			catch (Exception ex)
			{
				return CreateErrorResponseAndUpdateApiKey(ex.Message);
			}
		}
	}
}
