//  xg.refresh.js
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

var XGRefresh = Class.create(
{
	/**
	 * @param {XGHelper} helper
	 */
	initialize: function(helper)
	{
		this.helper = helper;

		this.snapshots = {};
	},

	setStatistics: function (result)
	{
		$("#BytesLoaded").html(this.helper.size2Human(result.BytesLoaded));

		$("#PacketsCompleted").html(result.PacketsCompleted);
		$("#PacketsIncompleted").html(result.PacketsIncompleted);
		$("#PacketsBroken").html(result.PacketsBroken);

		$("#PacketsRequested").html(result.PacketsRequested);
		$("#PacketsRemoved").html(result.PacketsRemoved);

		$("#FilesCompleted").html(result.FilesCompleted);
		$("#FilesBroken").html(result.FilesBroken);

		$("#ServerConnectsOk").html(result.ServerConnectsOk);
		$("#ServerConnectsFailed").html(result.ServerConnectsFailed);

		$("#ChannelConnectsOk").html(result.ChannelConnectsOk);
		$("#ChannelConnectsFailed").html(result.ChannelConnectsFailed);
		$("#ChannelsJoined").html(result.ChannelsJoined);
		$("#ChannelsParted").html(result.ChannelsParted);
		$("#ChannelsKicked").html(result.ChannelsKicked);

		$("#BotConnectsOk").html(result.BotConnectsOk);
		$("#BotConnectsFailed").html(result.BotConnectsFailed);

		$("#SpeedMax").html(this.helper.speed2Human(result.SpeedMax));
	},

	/* ************************************************************************************************************** */
	/* SNAPSHOT STUFF                                                                                                 */
	/* ************************************************************************************************************** */

	setSnapshots: function (result)
	{
		$.each(result, function(index, item) {
			item.color = index;
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

		this.snapshots = result;
		this.updateSnapshotPlot();
	},

	updateSnapshotPlot: function ()
	{
		var self = this;

		var days = parseInt($("input[name='snapshot_time']:checked").val());
		var snapshotsMinDate = days > 0 ? new Date().getTime() - (60 * 60 * 24 * days * 1000) : days;

		var data = [];
		var currentSnapshots = $.extend(true, [], this.snapshots);
		$.each(currentSnapshots, function(index, item) {
			if (index == 0 || $("#snapshot_checkbox_" + (index + 1)).attr('checked'))
			{
				var itemData = [];
				$.each(item.data, function(index2, item2) {
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
				markerFunction = function (axes) {
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
				markerFunction = function (axes) {
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
				markerFunction = function (axes) {
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
				markerFunction = function (axes) {
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
				axisLabel: _('Time'),
				mode: "time",
				timeformat: timeFormat,
				minTickSize: tickSize,
				monthNames: moment.monthsShort
			},
			yaxes: [
				{
					axisLabel: _('Server / Channels'),
					min: 0
				},
				{
					axisLabel: _('Bots'),
					min: 0
				},
				{
					axisLabel: _('Packets'),
					min: 0
				},
				{
					axisLabel: _('Size'),
					min: 0,
					alignTicksWithAxis: 1,
					tickFormatter: function (val) {
						if (val <= 1)
						{
							return "";
						}
						return self.helper.size2Human(val);
					}
				},
				{
					axisLabel: _('Speed'),
					min: 0,
					alignTicksWithAxis: 1,
					position: "right",
					tickFormatter: function (val) {
						if (val <= 1)
						{
							return "";
						}
						return self.helper.speed2Human(val);
					}
				}
			],
			legend: { position: "sw" },
			grid: { markings: markerFunction }
		};

		$.plot($("#snapshot"), data, snapshotOptions);
	}
});
