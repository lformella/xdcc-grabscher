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
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Nancy;

namespace XG.Plugin.Webserver
{
	using AppFunc = Func<IDictionary<string, object>, Task>;

	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			var settings = new JsonSerializerSettings
			{
				DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
				DateParseHandling = DateParseHandling.DateTime,
				DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
			};
			settings.Converters.Add(new DoubleConverter());

			GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => JsonSerializer.Create(settings));

			var hubConfiguration = new HubConfiguration();
#if DEBUG
			hubConfiguration.EnableDetailedErrors = true;
			StaticConfiguration.EnableRequestTracing = true;
			StaticConfiguration.DisableErrorTraces = false;
			StaticConfiguration.Caching.EnableRuntimeViewDiscovery = true;
			StaticConfiguration.Caching.EnableRuntimeViewUpdates = true;
#endif
			app.MapSignalR(hubConfiguration);
			app.UseNancy();
		}
	}
}
