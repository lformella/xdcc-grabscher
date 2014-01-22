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

using Nancy;
using Nancy.Responses;
using System.Linq;
using XG.Config.Properties;

namespace XG.Plugin.Webserver.Nancy
{
	public class RedirectModule : NancyModule
	{
		public RedirectModule()
		{
			Get["/"] = _ => new RedirectResponse("/Resources/index.html");

			string config = "define(['./module'], function (ng) { 'use strict'; ng.constant('LANGUAGE', '##LANGUAGE##'); ng.constant('SALT', '##SALT##'); ng.constant('VERSION', '##VERSION##'); });";

			Get["/Resources/js/config/config.js"] = _ => {
				var language = Request.Headers.AcceptLanguage.FirstOrDefault().Item1.Substring(0, 2);
				if (language != "en" && language != "de")
				{
					language = "en";
				}
				string ret = config;
				ret = ret.Replace("##LANGUAGE##", language);
				ret = ret.Replace("##SALT##", Helper.Salt);
				ret = ret.Replace("##VERSION##", Settings.Default.XgVersion);
				return new TextResponse(ret, "application/x-javascript");
			};
		}
	}
}
