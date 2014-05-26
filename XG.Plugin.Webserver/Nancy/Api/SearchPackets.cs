// 
//  SearchPackets.cs
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
using XG.Model;
using XG.Model.Domain;
using Nancy.Responses;
using Nancy.Serialization.JsonNet;
using Nancy.ModelBinding;
using System.Collections.Generic;

namespace XG.Plugin.Webserver.Nancy.Api
{
	public class SearchPackets : ApiModule
	{
		public SearchPackets()
		{
			Get["/api/searchPackets"] = _ =>
			{
				Request.SearchPackets request;
				try
				{
					var config = new BindingConfig { BodyOnly = true, IgnoreErrors = false };
					request = this.Bind<Request.SearchPackets>(config);
				}
				catch (Exception ex)
				{
					return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = ex.Message }, new JsonNetSerializer());
				}

				return ExecuteRequest(request);
			};
		}

		JsonResponse ExecuteRequest(Request.SearchPackets request)
		{
			if (!IsApiKeyValid(request.ApiKey))
			{
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.ApiKeyInvalid }, new JsonNetSerializer());
			}

			#region VALIDATION

			if (request.MaxResults == 0)
			{
				request.MaxResults = 20;
			}
			if (request.Page == 0)
			{
				request.Page = 1;
			}
			if (string.IsNullOrEmpty(request.SortBy))
			{
				request.SortBy = "Name";
			}
			if (string.IsNullOrEmpty(request.Sort))
			{
				request.Sort = "asc";
			}
				
			var messages = new List<string>();
			if (string.IsNullOrEmpty(request.SearchTerm))
			{
				messages.Add("searchTerm is empty");
			}
			if (request.SortBy != "Name" && request.SortBy != "Id" && request.SortBy != "Size")
			{
				messages.Add("sortBy just [Name|Id|Size] is allowed");
			}
			if (request.Sort != "asc" && request.Sort != "desc")
			{
				messages.Add("sort just [asc|desc] is allowed");
			}

			if (messages.Count > 0)
			{
				IncreaseErrorCount(request.ApiKey);
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = messages.Implode(", ") }, new JsonNetSerializer());
			}

			#endregion

			try
			{
				var result = new Result.SearchPackets();

				var packets =
					(
						from server in SignalR.Hub.Helper.Servers.All
						from channel in server.Channels
						from bot in channel.Bots where (request.ShowOfflineBots || bot.Connected)
						from packet in bot.Packets where packet.Name.ContainsAll(request.SearchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) select packet
					).ToList();
				int length;
				result.Packets = SignalR.Hub.Helper.FilterAndLoadObjects<SignalR.Hub.Model.Domain.Packet>(packets, request.MaxResults, request.Page, request.SortBy, request.Sort, out length);
				result.ResultCount = length;

				IncreaseSuccessCount(request.ApiKey);
				return new JsonResponse(result, new JsonNetSerializer());
			}
			catch (Exception ex)
			{
				IncreaseErrorCount(request.ApiKey);
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = ex.Message }, new JsonNetSerializer());
			}
		}
	}
}
