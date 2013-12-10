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

namespace XG.Plugin.Webserver.Nancy
{
	public class RedirectModule : NancyModule
	{
		public RedirectModule()
		{
			Get["/"] = _ => new RedirectResponse("/Resources/index.html");

			string languageSetter = "define(['./module', 'moment'], function (i18n, moment) {\n\t'use strict';\n\n\tmoment.lang('##LANGUAGE##');\n\ti18n.config(['$translateProvider',\n\t\tfunction ($translateProvider) {\n\t\t\t$translateProvider.preferredLanguage('##LANGUAGE##');\n\t\t}\n\t]);\n});";

			Get["/Resources/js/i18n/init.js"] = _ => {
				var language = Request.Headers.AcceptLanguage.FirstOrDefault().Item1.Substring(0, 2);
				return new TextResponse(languageSetter.Replace("##LANGUAGE##", language), "application/x-javascript");
			};
		}
	}
}
