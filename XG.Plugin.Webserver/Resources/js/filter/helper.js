//
//  helper.js
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

define(['./module', 'moment'], function (ng, moment) {
	'use strict';

	ng.filter('size2Human', function ()
	{
		return function (size, decimal)
		{
			if (decimal == undefined)
			{
				decimal = 0;
			}
			if (size == 0)
			{
				return "";
			}
			if (size < 1024)
			{
				return size + " B";
			}
			else if (size < 1024 * 1024)
			{
				return (size / 1024).toFixed(decimal) + " K";
			}
			else if (size < 1024 * 1024 * 1024)
			{
				return (size / (1024 * 1024)).toFixed(decimal) + " M";
			}
			else if (size < 1024 * 1024 * 1024 * 1024)
			{
				return (size / (1024 * 1024 * 1024)).toFixed(decimal) + " G";
			}
			return (size / (1024 * 1024 * 1024 * 1024)).toFixed((size < 1024 * 1024 * 1024 * 1024 * 10) ? 1 : decimal) + " T";
		}
	});

	ng.filter('speed2Human', function ()
	{
		return function (speed)
		{
			if (speed == 0)
			{
				return "";
			}
			if (speed < 1024)
			{
				return speed + " B";
			}
			else if (speed < 1024 * 1024)
			{
				return (speed > 100 * 1024 ? (speed / 1024).toFixed(1) : (speed / 1024).toFixed(2)) + " K";
			}
			return (speed > 100 * 1024 * 1024 ? (speed / (1024 * 1024)).toFixed(1) : (speed / (1024 * 1024)).toFixed(2)) + " M";
		}
	});

	ng.filter('date2Human', ['$rootScope', function ($rootScope)
	{
		return function (date)
		{
			var momentDate = moment(date);
			if (momentDate.year() == 1)
			{
				return "";
			}
			return $rootScope.settings.humanDates ? momentDate.fromNow() : momentDate.format("L LT");
		}
	}]);

	ng.filter('time2Human', function ()
	{
		return function (time)
		{
			if (time <= 0 || time >= 106751991167300 || time == undefined || time == null)
			{
				return "";
			}

			return moment.duration(time, "seconds").humanize(true);
		}
	});

	ng.filter('trustAsHtml', ['$sce', function ($sce)
	{
		return function (input)
		{
			return $sce.trustAsHtml(input);
		}
	}]);
});
