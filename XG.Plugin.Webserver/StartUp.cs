// 
//  StartUp.cs
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

using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using System.Text;
using XG.Config.Properties;

namespace XG.Plugin.Webserver
{
	using AppFunc = Func<IDictionary<string, object>, Task>;

	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			var hubConfiguration = new HubConfiguration();
			hubConfiguration.EnableJavaScriptProxies = false;
#if DEBUG
			hubConfiguration.EnableDetailedErrors = true;
#endif
			app.MapSignalR(hubConfiguration);

			app.Use(async (aContext, aNext) => 
			{
				if (aContext.Request.Path.Value == "/login" && aContext.Request.Method == "POST")
				{
					var password = new StreamReader(aContext.Request.Body).ReadToEnd();
					aContext.Response.StatusCode = password == Settings.Default.Password ? 200 : 403;
				}
				else
				{
					await aNext();
				}
			});

			app.Use(async (aContext, aNext) => 
			{
				var lang = aContext.Request.Headers.Get("Accept-Language").Substring(0, 2);
				string file = aContext.Request.Path.Value;
				var content = new byte[0];

				if (file.Contains("?"))
				{
					file = file.Split('?')[0];
				}

				if (file == "/")
				{
					file = "/Resources/index.html";
				}
				else if (file == "/favicon.ico")
				{
					file = "/Resources/favicon.ico";
				}
				else if (file.StartsWith("/template/"))
				{
					file = "/Resources" + file;
				}

				if (file.EndsWith(".png"))
				{
					aContext.Response.ContentType = "image/png";
				}
				else if (file.EndsWith(".gif"))
				{
					aContext.Response.ContentType = "image/gif";
				}
				else if (file.EndsWith(".ico"))
				{
					aContext.Response.ContentType = "image/ico";
				}
				else if (file.EndsWith(".css"))
				{
					aContext.Response.ContentType = "text/css";
				}
				else if (file.EndsWith(".js"))
				{
					aContext.Response.ContentType = "application/x-javascript";
				}
				else if (file.EndsWith(".html"))
				{
					aContext.Response.ContentType = "text/html;charset=UTF-8";
				}
				else if (file.EndsWith(".woff"))
				{
					aContext.Response.ContentType = "application/x-font-woff";
				}
				else if (file.EndsWith(".ttf"))
				{
					aContext.Response.ContentType = "application/x-font-ttf";
				}

				content = EmbeddedFileLoader.Load(file);
				if (content.Length == 0)
				{
					aContext.Response.StatusCode = 404;
				}

				if (file == "/Resources/js/i18n/index.js")
				{
					content = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(content).Replace("##LANGUAGE##", lang));
				}
				aContext.Response.ContentLength = content.Length;

				await aContext.Response.WriteAsync(content);
			});
		}
	}
}
