// 
//  Servers.cs
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

namespace XG.Plugin.Webserver.Nancy.Api
{
	public class Servers : ApiModule
	{
		public Servers()
		{
			InitializeGet(Helper.Servers, "servers");
			InitializeGetAll(Helper.Servers, "servers");
			InitializeEnable(Helper.Servers, "servers");
			InitializeDelete(Helper.Servers, "servers");

			Put["/servers", true] = async(_, ct) =>
			{
				Request.ServerAdd request;
				try
				{
					var config = new BindingConfig { BodyOnly = true, IgnoreErrors = false };
					request = this.Bind<Request.ServerAdd>(config);
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

		object ExecuteRequest(Request.ServerAdd request)
		{
			try
			{
				if (Helper.Servers.Add(request.Server, request.Port))
				{
					return CreateSuccessResponseAndUpdateApiKey();
				}
				else
				{
					return CreateErrorResponseAndUpdateApiKey("server already there");
				}
			}
			catch (Exception ex)
			{
				return CreateErrorResponseAndUpdateApiKey(ex.Message);
			}
		}
	}
}
