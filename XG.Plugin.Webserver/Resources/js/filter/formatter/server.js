//
//  server.js
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

define(['./module'], function (ng) {
	'use strict';

	ng.filter('formatServerIcon', ['$filter', function ($filter)
	{
		return function (server)
		{
			if (server == undefined)
			{
				return "";
			}
			var icon = "globe";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "opacity: 0.6";

			if (!server.Enabled)
			{
				iconClass = "Aluminium1Dark";
			}
			else if (server.Connected)
			{
				overlay = "ok-circle";
				overlayClass = "ChameleonMiddle";
			}
			else if (server.ErrorCode != "" && server.ErrorCode != "None" && server.ErrorCode != "0")
			{
				overlay = "warning-sign";
				overlayClass = "ScarletRedMiddle";
			}
			else
			{
				overlay = "time";
				overlayClass = "OrangeMiddle";
			}

			if (server.Active)
			{
				overlay = "asterisk";
				overlayClass = "ScarletRedMiddle animate-spin";
				overlayStyle = "";
			}

			return $filter('formatIcon')(icon, iconClass, overlay, overlayClass, overlayStyle);
		}
	}]);

	ng.filter('formatServerName', ['$filter', '$translate', function ($filter, $translate)
	{
		return function (server)
		{
			if (server == undefined)
			{
				return "";
			}

			var str = server.Name; // + ":" + server.Port;
			if (server.ErrorCode != "" && server.ErrorCode != "None" && server.ErrorCode != "0")
			{
				str += " - <small>" + $translate("Error") + ": " + server.ErrorCode + "</small>";
			}
			return str;
		}
	}]);
});
