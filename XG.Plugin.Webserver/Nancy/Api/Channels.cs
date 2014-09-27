// 
//  Channels.cs
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
	public class Channels : ApiModule
	{
		public Channels()
		{
			InitializeGet(Helper.Servers, "channels");
			InitializeGetAll(Helper.Servers, "channels");
			InitializeEnable(Helper.Servers, "channels");
			InitializeDelete(Helper.Servers, "channels");

			Put["/channels", true] = async(_, ct) =>
			{
				Request.ChannelAdd request;
				try
				{
					var config = new BindingConfig { BodyOnly = true, IgnoreErrors = false };
					request = this.Bind<Request.ChannelAdd>(config);
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

				return ExecuteRequest(request);
			};
		}

		protected override object SearchObjects(AObjects aObjects)
		{
			var channels = from server in Helper.Servers.All from channel in server.Channels select channel;
			var objs = Helper.XgObjectsToNancyObjects(channels);
			var result = new Result.Objects { Results = objs, ResultCount = objs.Count() };
			return CreateSuccessResponseAndUpdateApiKey(result);
		}

		object ExecuteRequest(Request.ChannelAdd request)
		{
			try
			{
				Server serv = Helper.Servers.Server(request.Server);
				if (serv != null)
				{
					if (serv.AddChannel(request.Channel))
					{
						return CreateSuccessResponseAndUpdateApiKey();
					}
					else
					{
						return CreateErrorResponseAndUpdateApiKey("channel already there");
					}
				}
				else
				{
					return CreateErrorResponseAndUpdateApiKey("server not found");
				}
			}
			catch (Exception ex)
			{
				return CreateErrorResponseAndUpdateApiKey(ex.Message);
			}
		}
	}
}
