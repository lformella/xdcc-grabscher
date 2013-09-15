//
//  graph.js
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

var XGGraph = (function ()
{
	var dataView, helper, translate;
	var snapshots, liveSnapshot = {};

	function updateDashboardItem (element, values)
	{
		var data, options = [];

		options = {
			series: {
				pie: {
					radius: 1,
					innerRadius: 0.6,
					show: true,
					label: {
						show: true,
						formatter: function labelFormatter (label, series)
						{
							return "<div>" + Math.round(series.percent) + "%</div>";
						},
						background: {
							opacity: 0.5,
							color: '#000'
						}
					}
				}
			}
		};
		data = [
			{color: "#73d216", label: translate._(element + "Connected"), data: values.connected},
			{color: "#cc0000", label: translate._(element + "Disconnected"), data: values.disconnected}
		];
		$.plot("#dashboard" + element + " > div:nth-child(1)", data, options);

		if (values.enabled != undefined && values.disabled != undefined)
		{
			options = {
				series: {
					pie: {
						radius: 0.6,
						innerRadius: 0.5,
						show: true
					}
				}
			};
			data = [
				{color: "#8ae234", label: "", data: values.enabled},
				{color: "#ef2929", label: "", data: values.disabled}
			];
			$.plot("#dashboard" + element + " > div:nth-child(2)", data, options);
		}
	}

	return {
		/**
		 * @param {XGHelper} helper1
		 * @param {XGTranslate} translate1
		 */
		initialize: function (helper1, translate1)
		{
			helper = helper1;
			translate = translate1;
		},

		setSnapshots: function (result)
		{
			$.each(result, function (index, item)
			{
				item.color = index;
				item.label = translate._(item.label);
				switch (index + 1)
				{
					case Enum.SnapshotValue.Bots:
					case Enum.SnapshotValue.BotsConnected:
					case Enum.SnapshotValue.BotsDisconnected:
					case Enum.SnapshotValue.BotsFreeQueue:
					case Enum.SnapshotValue.BotsFreeSlots:
						item.yaxis = 2;
						break;

					case Enum.SnapshotValue.Packets:
					case Enum.SnapshotValue.PacketsConnected:
					case Enum.SnapshotValue.PacketsDisconnected:
						item.yaxis = 3;
						break;

					case Enum.SnapshotValue.PacketsSize:
					case Enum.SnapshotValue.PacketsSizeDownloading:
					case Enum.SnapshotValue.PacketsSizeNotDownloading:
					case Enum.SnapshotValue.PacketsSizeConnected:
					case Enum.SnapshotValue.PacketsSizeDisconnected:
						item.yaxis = 4;
						break;

					case Enum.SnapshotValue.Speed:
					case Enum.SnapshotValue.BotsAverageCurrentSpeed:
					case Enum.SnapshotValue.BotsAverageMaxSpeed:
						item.yaxis = 5;
						break;

					default:
						item.yaxis = 1;
						break;
				}
			});

			snapshots = result;
			this.updateSnapshotPlot();
		},

		updateSnapshotPlot: function ()
		{
			var days = parseInt($("input[name='snapshotTime']:checked").val()) * -1;
			var snapshotsMinDate = days > 0 ? new Date().getTime() - (60 * 60 * 24 * days * 1000) : days;

			var data = [];
			var currentSnapshots = $.extend(true, [], snapshots);
			$.each(currentSnapshots, function (index, item)
			{
				if (index == 0 || $("#snapshotCheckbox" + (index + 1)).prop('checked'))
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
					axisLabel: translate._('Time'),
					mode: "time",
					timeformat: timeFormat,
					minTickSize: tickSize,
					monthNames: moment.monthsShort
				},
				yaxes: [
					{
						axisLabel: translate._('Server / Channels'),
						min: 0
					},
					{
						axisLabel: translate._('Bots'),
						min: 0
					},
					{
						axisLabel: translate._('Packets'),
						min: 0
					},
					{
						axisLabel: translate._('Size'),
						min: 0,
						alignTicksWithAxis: 1,
						tickFormatter: function (val)
						{
							if (val <= 1)
							{
								return "";
							}
							return helper.size2Human(val);
						}
					},
					{
						axisLabel: translate._('Speed'),
						min: 0,
						alignTicksWithAxis: 1,
						position: "right",
						tickFormatter: function (val)
						{
							if (val <= 1)
							{
								return "";
							}
							return helper.speed2Human(val);
						}
					}
				],
				legend: { position: "sw" },
				grid: { markings: markerFunction }
			};

			$.plot($("#snapshot"), data, snapshotOptions);
		},

		setLiveSnapshot: function (snapshot)
		{
			liveSnapshot = {};

			$.each(snapshot, function (index, item)
			{
				liveSnapshot[item.label] = item.data[0][1];
			});

			this.updateDashboard();
		},

		updateDashboard: function ()
		{
			updateDashboardItem("Servers", {
				"enabled": liveSnapshot.ServersEnabled,
				"disabled": liveSnapshot.ServersDisabled,
				"connected": liveSnapshot.ServersConnected,
				"disconnected": liveSnapshot.ServersDisconnected
			});

			updateDashboardItem("Channels", {
				"enabled": liveSnapshot.ChannelsEnabled,
				"disabled": liveSnapshot.ChannelsDisabled,
				"connected": liveSnapshot.ChannelsConnected,
				"disconnected": liveSnapshot.ChannelsDisconnected
			});

			updateDashboardItem("Bots", {
				"connected": liveSnapshot.BotsConnected,
				"disconnected": liveSnapshot.BotsDisconnected
			});

			var data, options = [];

			if (liveSnapshot.FileTimeMissing > 0)
			{
				$("#dashboardFiles").show();

				options = {
					series: {
						pie: {
							radius: 1,
							innerRadius: 0.6,
							show: true,
							label: {
								show: true,
								formatter: function labelFormatter (label, series)
								{
									return "<div>" + helper.size2Human(series.data[0][1], 2) + "</div>";
								},
								background: {
									opacity: 0.5,
									color: '#000'
								}
							}
						}
					}
				};
				data = [
					{color: "#73d216", label: translate._("FileSizeDownloaded"), data: liveSnapshot.FileSizeDownloaded},
					{color: "#cc0000", label: translate._("FileSizeMissing"), data: liveSnapshot.FileSizeMissing}
				];

				$.plot("#dashboardFiles > div:nth-child(1)", data, options);
				$("#timeMissing").html(helper.time2Human(liveSnapshot.FileTimeMissing));
			}
			else
			{
				$("#dashboardFiles").hide();
			}
		},

		resize: function ()
		{
			this.updateSnapshotPlot();
			this.updateDashboard();
		}
	}
}());
