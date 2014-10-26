//
//  flot.js
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

define(['./module', 'jqFlot', 'jqFlot.time', 'jqFlot.pie', 'jqFlot.axislabels'], function (ng) {
	'use strict';

	ng.directive('flot', ['$filter', '$translate', function ($filter, $translate)
	{
		var $el = undefined;
		var days = undefined;
		var groups = undefined;
		var snapshots = undefined;

		var isChecked = function (id)
		{
			var ret = false;
			$.each(groups, function (index, group)
			{
				$.each(group.types, function (index, type)
				{
					if (type.id == id)
					{
						ret = type.enabled;
					}
				});
			});
			return ret;
		};

		var render = function ()
		{
			var snapshotsMinDate = days > 0 ? new Date().getTime() - (60 * 60 * 24 * days * 1000) : days;

			var data = [];
			var currentSnapshots = $.extend(true, [], snapshots);
			$.each(currentSnapshots, function (index, item)
			{
				if (index == 0 || isChecked(index + 1, groups))
				{
					var itemData = [];
					$.each(item.data, function (index2, item2)
					{
						if (snapshotsMinDate < item2[0])
						{
							itemData.push(item2);
						}
					});
					item.data = itemData;

					data.push(item);
				}
			});

			var markerFunction;
			var tickSize;
			var timeFormat;
			switch (days)
			{
				case 1:
					timeFormat = "%H:%M";
					tickSize = [2, "hour"];
					markerFunction = function (axes)
					{
						var markings = [];
						var d = new Date(axes.xaxis.min);
						d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
						d.setUTCSeconds(0);
						d.setUTCMinutes(0);
						d.setUTCHours(0);
						var i = d.getTime();
						do
						{
							markings.push({
								xaxis: {
									from: i,
									to: i + 2 * 60 * 60 * 1000
								}
							});
							i += 4 * 60 * 60 * 1000;
						} while (i < axes.xaxis.max);

						return markings;
					};
					break;

				case 7:
					timeFormat = "%d. %b";
					tickSize = [1, "day"];
					markerFunction = function (axes)
					{
						var markings = [];
						var d = new Date(axes.xaxis.min);
						d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
						d.setUTCSeconds(0);
						d.setUTCMinutes(0);
						d.setUTCHours(0);
						var i = d.getTime();
						do
						{
							markings.push({
								xaxis: {
									from: i,
									to: i + 2 * 24 * 60 * 60 * 1000
								}
							});
							i += 7 * 24 * 60 * 60 * 1000;
						} while (i < axes.xaxis.max);

						return markings;
					};
					break;

				case 31:
					timeFormat = "%d. %b";
					tickSize = [7, "day"];
					markerFunction = function (axes)
					{
						var markings = [];
						var d = new Date(axes.xaxis.min);
						d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
						d.setUTCSeconds(0);
						d.setUTCMinutes(0);
						d.setUTCHours(0);
						var i = d.getTime();
						do
						{
							markings.push({
								xaxis: {
									from: i,
									to: i + 7 * 24 * 60 * 60 * 1000
								}
							});
							i += 14 * 24 * 60 * 60 * 1000;
						} while (i < axes.xaxis.max);

						return markings;
					};
					break;

				default:
					timeFormat = "%b %y";
					tickSize = [1, "month"];
					markerFunction = function (axes)
					{
						var markings = [];
						var d = new Date(axes.xaxis.min);
						d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
						d.setUTCSeconds(0);
						d.setUTCMinutes(0);
						d.setUTCHours(0);
						var i = d.getTime();
						do
						{
							markings.push({
								xaxis: {
									from: i,
									to: i + 7 * 24 * 60 * 60 * 1000
								}
							});
							i += 14 * 24 * 60 * 60 * 1000;
						} while (i < axes.xaxis.max);

						return markings;
					};
					break;
			}

			var snapshotOptions = {
				xaxis: {
					axisLabel: $translate('Time'),
					mode: "time",
					timeformat: timeFormat,
					minTickSize: tickSize,
					monthNames: moment.monthsShort()
				},
				yaxes: [
					{
						axisLabel: $translate('Server / Channels'),
						min: 0
					},
					{
						axisLabel: $translate('Bots'),
						min: 0
					},
					{
						axisLabel: $translate('Packets'),
						min: 0
					},
					{
						axisLabel: $translate('Size'),
						min: 0,
						alignTicksWithAxis: 1,
						tickFormatter: function (val)
						{
							if (val <= 1)
							{
								return "";
							}
							return $filter('size2Human')(val);
						}
					},
					{
						axisLabel: $translate('Speed'),
						min: 0,
						alignTicksWithAxis: 1,
						position: "right",
						tickFormatter: function (val)
						{
							if (val <= 1)
							{
								return "";
							}
							return $filter('speed2Human')(val);
						}
					}
				],
				legend: { position: "sw" },
				grid: { markings: markerFunction }
			};

			$.plot($el, data, snapshotOptions);
		};

		var width, height;
		var resize = function ()
		{
			// just resize if necessary
			if ($(window).width() != width || $(window).height() != height)
			{
				width = $(window).width();
				height = $(window).height();

				$el.width(width - 300).height(height - 100);
				render();
			}
		};

		$(window).resize(function ()
		{
			resize();
		});

		return {
			restrict: 'A',
			scope:
			{
				days: '=',
				groups: '=',
				snapshots: '='
			},
			link: function(scope, element, attrs)
			{
				$el = $(element);

				scope.$parent.render = function()
				{
					render();
				};

				scope.$watch('days', function (newDays)
				{
					days = newDays;
					render();
				});

				scope.$watch('groups', function (newGroups)
				{
					groups = newGroups;
					render();
				}, true);

				scope.$watch('snapshots', function (newSnapshots)
				{
					snapshots = newSnapshots;
					render();
				});

				resize();
			}
		};
	}]);
});
