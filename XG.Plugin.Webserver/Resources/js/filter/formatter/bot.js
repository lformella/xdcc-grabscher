//
//  bot.js
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

	ng.filter('formatBotIcon', ['$filter', function ($filter)
	{
		return function (bot)
		{
			if (bot == undefined)
			{
				return "";
			}

			var icon = "user";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "";

			if (!bot.Connected)
			{
				iconClass = "Aluminium1Dark";

				if (bot.HasNetworkProblems)
				{
					overlay = "warning-sign";
					overlayClass = "ScarletRedMiddle";
				}
			}
			else
			{
				switch (bot.State)
				{
					case 0:
						if (bot.InfoSlotCurrent > 0)
						{
							overlay = "ok-circle";
							overlayClass = "ChameleonMiddle";
							overlayStyle = "opacity: 0.6";
						}
						else if (bot.InfoSlotCurrent == 0 && bot.InfoSlotCurrent)
						{
							overlay = "remove-circle";
							overlayClass = "OrangeMiddle";
						}
						break;

					case 1:
						iconClass = "SkyBlueDark";
						overlay = "download";
						overlayClass = "SkyBlueMiddle";
						overlayStyle = "opacity: " + $filter('speed2Overlay')(bot.Speed);
						break;

					case 2:
						overlay = "time";
						overlayClass = "OrangeMiddle";
						break;
				}
			}

			if (bot.Active)
			{
				overlay = "asterisk";
				overlayClass = "ScarletRedMiddle animate-spin";
				overlayStyle = "";
			}
			else if (bot.Waiting)
			{
				overlay = "asterisk";
				overlayClass = "ScarletRedLight animate-spin";
				overlayStyle = "";
			}

			return $filter('formatIcon')(icon, iconClass, overlay, overlayClass, overlayStyle);
		}
	}]);

	ng.filter('formatBotName', ['$filter', function ($filter)
	{
		return function (bot)
		{
			if (bot == undefined)
			{
				return "";
			}

			var ret = "<strong>" + bot.Name + "</strong>";
			if (bot.LastMessage != "")
			{
				ret += "<br /><small title='" + bot.LastMessage + "'>" + $filter('date2Human')(bot.LastMessageTime) + ": " + bot.LastMessage + "</small>";
			}
			return ret;
		}
	}]);

	ng.filter('formatBotSpeed', ['$filter', function ($filter)
	{
		return function (bot)
		{
			if (bot == undefined)
			{
				return "";
			}

			var ret = "";
			if (bot.InfoSpeedCurrent > 0)
			{
				ret += $filter('speed2Human')(bot.InfoSpeedCurrent);
			}
			if (bot.InfoSpeedCurrent > 0 && bot.InfoSpeedMax > 0)
			{
				ret += " / ";
			}
			if (bot.InfoSpeedMax > 0)
			{
				ret += $filter('speed2Human')(bot.InfoSpeedMax);
			}
			return ret;
		}
	}]);

	ng.filter('formatBotSlots', function ()
	{
		return function (bot)
		{
			if (bot == undefined)
			{
				return "";
			}

			var ret = "";
			ret += bot.InfoSlotCurrent;
			ret += " / ";
			ret += bot.InfoSlotTotal;
			return ret;
		}
	});

	ng.filter('formatBotQueue', function ()
	{
		return function (bot)
		{
			if (bot == undefined)
			{
				return "";
			}

			var ret = "";
			ret += bot.InfoQueueCurrent;
			ret += " / ";
			ret += bot.InfoQueueTotal;
			return ret;
		}
	});
});
