// 
//  Enable.cs
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
	public class Enable : ApiModule
	{
		public Enable()
		{
			Post["/api/enable"] = _ =>
			{
				Request.Enable request;
				try
				{
					var config = new BindingConfig { BodyOnly = true, IgnoreErrors = false };
					request = this.Bind<Request.Enable>(config);
				}
				catch (Exception ex)
				{
					return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = ex.Message }, new JsonNetSerializer());
				}

				return ExecuteRequest(request);
			};
		}

		JsonResponse ExecuteRequest(Request.Enable request)
		{
			if (!IsApiKeyValid(request.ApiKey))
			{
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.ApiKeyInvalid }, new JsonNetSerializer());
			}

			try
			{
				var obj = SignalR.Hub.Helper.Servers.WithGuid(request.Guid);
				if (obj != null)
				{
					obj.Enabled = request.Enabled;

					IncreaseSuccessCount(request.ApiKey);
					return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Ok }, new JsonNetSerializer());
				}
				else
				{
					IncreaseErrorCount(request.ApiKey);
					return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error }, new JsonNetSerializer());
				}
			}
			catch (Exception ex)
			{
				IncreaseErrorCount(request.ApiKey);
				return new JsonResponse(new Result.Default { ReturnValue = Result.Default.States.Error, Message = ex.Message }, new JsonNetSerializer());
			}
		}
	}
}
