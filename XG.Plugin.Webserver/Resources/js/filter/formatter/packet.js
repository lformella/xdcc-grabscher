//
//  packet.js
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

	ng.filter('formatPacketIcon', ['$filter', function ($filter)
	{
		return function (packet)
		{
			if (packet == undefined)
			{
				return "";
			}

			var icon = "file";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "";

			var name = packet.Name;
			var ext = name.toLowerCase().substr(-3);
			if (ext == "avi" || ext == "wmv" || ext == "mkv" || ext == "mpg" || ext == "mov" || ext == "mp4")
			{
				icon = "film";
			}
			else if (ext == "mp3" || ext == "ogg" || ext == "wav")
			{
				icon = "headphones";
			}
			else if (ext == "rar" || ext == "tar" || ext == "zip")
			{
				icon = "compressed";
			}

			if (!packet.Enabled)
			{
				iconClass = "Aluminium1Dark";
			}
			else
			{
				if (packet.Connected)
				{
					iconClass = "SkyBlueDark";
					overlay = "download";
					overlayClass = "SkyBlueMiddle";
					overlayStyle = "opacity: " + $filter('speed2Overlay')(packet.Speed);
				}
				else if (packet.Next)
				{
					overlay = "time";
					overlayClass = "OrangeMiddle";
				}
				else
				{
					overlay = "time";
					overlayClass = "ButterMiddle";
				}
			}

			if (packet.Active)
			{
				overlay = "asterisk";
				overlayClass = "ScarletRedMiddle animate-spin";
				overlayStyle = "";
			}

			return $filter('formatIcon')(icon, iconClass, overlay, overlayClass, overlayStyle);
		}
	}]);

	ng.filter('formatPacketName', function ()
	{
		return function (packet)
		{
			if (packet == undefined)
			{
				return "";
			}

			var name = packet.Name;

			if (name == undefined)
			{
				return "";
			}

			var ret = "<span title='" + name + "'>" + name + "</span>";

			if (packet.Connected)
			{
				var a = ((packet.StartSize) / packet.Size).toFixed(2) * 100;
				var b = ((packet.CurrentSize - packet.StartSize) / packet.Size).toFixed(2) * 100;
				var c = ((packet.StopSize - packet.CurrentSize) / packet.Size).toFixed(2) * 100;
				if (a + b + c > 100)
				{
					c = 100 - a - b;
				}

				ret += "<div class='progress progress-striped'>" +
					"<div style='width: " + a + "%' class='progress-bar'></div>" +
					"<div style='width: " + b + "%' class='progress-bar " + (packet.IsChecked ? "progress-bar-success" : "progress-bar-warning") + "'></div>" +
					"<div style='width: " + c + "%' class='progress-bar " + (packet.IsChecked ? "progress-bar-success" : "progress-bar-warning") + " progress-bar-light'></div>" +
					"</div>";
				//ret += "<progress max='" + packet.Size + "' value='" + packet.CurrentSize + "'></progress>";
			}

			return ret;
		}
	});
});
