// 
//  RedirectModule.cs
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

using System.Linq;
using System.Reflection;
using System.Text;
using Nancy;
using Nancy.Responses;
using Newtonsoft.Json;
using XG.Config.Properties;

namespace XG.Plugin.Webserver.Nancy
{
	public class RedirectModule : NancyModule
	{
#if !DEBUG
		string _basePath = "/" + Settings.Default.XgVersion + "/";
#else
		string _basePath = "/";
#endif
		string _config = "define(['./module'], function (ng) { 'use strict'; ng.constant('LANGUAGE', '##LANGUAGE##'); ng.constant('SALT', '##SALT##'); ng.constant('VERSION', '##VERSION##'); ng.constant('REMOTE_SETTINGS', ##REMOTE_SETTINGS##); ng.constant('ONLINE', true); });";

		public RedirectModule()
		{
			Get["/", true] = async (_, ct) => new RedirectResponse(_basePath + "Resources/index.html");

			Get[_basePath + "Resources/js/config/config.js", true] = async (parameters, ct) =>
			{
				var language = Request.Headers.AcceptLanguage.FirstOrDefault().Item1.Substring(0, 2);
				if (language != "en" && language != "de")
				{
					language = "en";
				}
				string ret = _config;
				ret = ret.Replace("##LANGUAGE##", language);
				ret = ret.Replace("##SALT##", Helper.Salt);
				ret = ret.Replace("##VERSION##", Settings.Default.XgVersion);
				ret = ret.Replace("##REMOTE_SETTINGS##", JsonConvert.SerializeObject(Helper.RemoteSettings));

				return new TextResponse(ret, "application/x-javascript");
			};
		}
	}
}
