//
//  client.js
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

	ng.filter('formatClientIcon', ['$filter', function ($filter)
	{
		return function (client)
		{
			if (client == undefined)
			{
				return "";
			}

			var icon = "fire";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "opacity: 0.6";

			if (!client.Enabled)
			{
				iconClass = "Aluminium1Dark";
			}

			if (client.Active)
			{
				overlay = "asterisk";
				overlayClass = "ScarletRedMiddle animate-spin";
				overlayStyle = "";
			}

			return $filter('formatIcon')(icon, iconClass, overlay, overlayClass, overlayStyle);
		}
	}]);
});
