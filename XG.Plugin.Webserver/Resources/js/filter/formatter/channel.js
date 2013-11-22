//
//  channel.js
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

define(['./module'], function (filter) {
	'use strict';

	filter.filter('formatChannelIcon', ['$filter', function ($filter)
	{
		return function (channel)
		{
			var icon = "comment";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "opacity: 0.6";

			if (!channel.Enabled)
			{
				iconClass = "Aluminium1Dark";
			}
			else if (channel.Connected)
			{
				overlay = "ok-circle";
				overlayClass = "ChameleonMiddle";
			}
			else if (channel.ErrorCode != "" && channel.ErrorCode != "None" && channel.ErrorCode != "0")
			{
				overlay = "warning-sign";
				overlayClass = "ScarletRedMiddle";
			}

			if (channel.Active)
			{
				overlay = "asterisk";
				overlayClass = "ScarletRedMiddle animate-spin";
				overlayStyle = "";
			}

			return $filter('formatIcon')(icon, iconClass, overlay, overlayClass, overlayStyle);
		}
	}]);

	filter.filter('formatChannelName', ['$filter', '$translate', function ($filter, $translate)
	{
		return function (channel)
		{
			if (channel == undefined)
			{
				return "";
			}

			var str = channel.Name;
			if (channel.ErrorCode != "" && channel.ErrorCode != "None" && channel.ErrorCode != "0")
			{
				str += " - <small>" + $translate("Error") + ": " + channel.ErrorCode + "</small>";
			}
			if (channel.Topic != null)
			{
				str += "<br /><small title='" + channel.Topic + "'>" + channel.Topic + "</small>";
			}
			return str;
		}
	}]);
});
