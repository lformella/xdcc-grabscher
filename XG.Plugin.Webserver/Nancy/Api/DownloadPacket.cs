// 
//  DownloadPacket.cs
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
using XG.Model;
using XG.Model.Domain;
using Nancy.Responses;
using Nancy.Serialization.JsonNet;
using Nancy.ModelBinding;
using System.Collections.Generic;

namespace XG.Plugin.Webserver.Nancy.Api
{
	public class DownloadPacket : ApiModule
	{
		public DownloadPacket()
		{
			Put["/api/downloadPacket"] = _ =>
			{
				Request.DownloadPacket request;
				try
				{
					var config = new BindingConfig { BodyOnly = true, IgnoreErrors = false };
					request = this.Bind<Request.DownloadPacket>(config);
				}
				catch (Exception ex)
				{
					return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = ex.Message }, new JsonNetSerializer());
				}

				return ExecuteRequest(request);
			};
		}

		JsonResponse ExecuteRequest(Request.DownloadPacket request)
		{
			if (!IsApiKeyValid(request.ApiKey))
			{
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.ApiKeyInvalid }, new JsonNetSerializer());
			}

			#region VALIDATION

			var messages = new List<string>();
			if (string.IsNullOrEmpty(request.Server))
			{
				messages.Add("server is empty");
			}
			if (string.IsNullOrEmpty(request.Channel))
			{
				messages.Add("channel is empty");
			}
			if (string.IsNullOrEmpty(request.Bot))
			{
				messages.Add("bot is empty");
			}
			if (request.PacketId == 0)
			{
				messages.Add("packetId is empty");
			}
			if (string.IsNullOrEmpty(request.PacketName))
			{
				messages.Add("packetName is empty");
			}

			if (messages.Count > 0)
			{
				IncreaseErrorCount(request.ApiKey);
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = messages.Implode(", ") }, new JsonNetSerializer());
			}

			#endregion

			try
			{
				// checking server
				Server serv = SignalR.Hub.Helper.Servers.Server(request.Server);
				if (serv == null)
				{
					SignalR.Hub.Helper.Servers.Add(request.Server);
					serv = SignalR.Hub.Helper.Servers.Server(request.Server);
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

				IncreaseSuccessCount(request.ApiKey);
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Ok }, new JsonNetSerializer());
			}
			catch (Exception ex)
			{
				IncreaseErrorCount(request.ApiKey);
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = ex.Message }, new JsonNetSerializer());
			}
		}
	}
}
