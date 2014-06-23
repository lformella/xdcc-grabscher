// 
//  ApiKeyAuthentication.cs
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
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Security;

namespace XG.Plugin.Webserver.Nancy.Authentication
{
	public static class ApiKeyAuthentication
	{
		public static void Enable(IPipelines pipelines)
		{
			if (pipelines == null)
			{
				throw new ArgumentNullException("pipelines");
			}

			pipelines.BeforeRequest.AddItemToStartOfPipeline(GetCredentialRetrievalHook());
		}

		public static void Enable(INancyModule module)
		{
			if (module == null)
			{
				throw new ArgumentNullException("module");
			}

			module.RequiresAuthentication();
			module.Before.AddItemToStartOfPipeline(GetCredentialRetrievalHook());
		}

		static Func<NancyContext, Response> GetCredentialRetrievalHook()
		{
			return context =>
			{
				RetrieveCredentials(context);
				return null;
			};
		}

		static void RetrieveCredentials(NancyContext context)
		{
			var credentials = context.Request.Headers.Authorization;

			if (!String.IsNullOrWhiteSpace(credentials))
			{
				if (IsRequestValid(credentials))
				{
					context.CurrentUser = new ApiKeyUserIdentity { UserName = credentials };
				}
			}
		}

		static bool IsRequestValid(string aCredentials)
		{
			try
			{
				var apiKey = Helper.ApiKeys.WithGuid(Guid.Parse(aCredentials));
				if (apiKey != null)
				{
					return apiKey.Enabled;
				}
			}
			catch (Exception)
			{
				return false;
			}
			return false;
		}
	}
}
