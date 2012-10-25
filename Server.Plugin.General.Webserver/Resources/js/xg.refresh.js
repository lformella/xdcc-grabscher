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
	 * @param {XGUrl} url
	 * @param {XGHelper} helper
	 */
	initialize: function(url, helper)
	{
		this.url = url;
		this.helper = helper;

		this.snapshots = {};
	},

	/**
	 * @param {String} grid
	 * @param {String} guid
	 * @return {object}
	 */
	getRowData: function (grid, guid)
	{
		return $.parseJSON($("#" + grid).getRowData(guid).Object);
	},

	/**
	 * @param {string} grid
	 * @param {String} url
	 */
	reloadGrid: function (grid, url)
	{
		var gridElement = $("#" + grid);
		gridElement.clearGridData();
		if(url != undefined && url != "")
		{
			gridElement.setGridParam({url: url, page: 1});
		}
		gridElement.trigger("reloadGrid");
	},

	/**
	 * @param {Integer} count
	 */
	refreshGrid: function (count)
	{
		var self = this;

		// connected things every 2,5 seconds, waiting just every 25 seconds
		var mod = !!(count % 10 == 0);

		// every 5 minutes
		if (!!(count % 120 == 0))
		{
			this.updateSnapshots();
		}

		switch(XG.activeTab)
		{
			case 0:
				// refresh bot grid
				$.each($("#bots_table").getDataIDs(), function(i, id)
				{
					var bot = self.getRowData("bots_table", id);
					if(bot.State == 1 || (mod && bot.State == 2))
					{
						self.refreshObject("bots_table", id);
					}
				});

				// refresh packet grid
				$.each($("#packets_table").getDataIDs(), function(i, id)
				{
					var pack = self.getRowData("packets_table", id);
					if(pack.Connected || (mod && pack.Enabled))
					{
						self.refreshObject("packets_table", id);
					}
				});
				break;

			case 1:
				break;

			case 2:
				$.each($("#files_table").getDataIDs(), function(i, id)
				{
					var file = self.getRowData("files", id);

					var state = -1;
					$.each(file.Parts, function(i, part)
					{
						return part.State != 0;
					});

					if(state == 0)
					{
						self.refreshObject("files", id);
					}
				});
				break;
		}

		setTimeout(function() { self.refreshGrid(count + 1); }, 2500);
	},

	/**
	 * @param {String} grid
	 * @param {String} guid
	 */
	refreshObject: function (grid, guid)
	{
		$.getJSON(this.url.guidUrl(Enum.TCPClientRequest.GetObject, guid),
			function(result)
			{
				result.cell.Object = JSON.stringify(result.cell);
				result.cell.Icon = "";

				if(grid == "packets_table")
				{
					result.cell.Speed = "";
					result.cell.TimeMissing = "";
				}

				$("#" + grid).setRowData(guid, result.cell);
			}
		);
	},

	refreshStatistic: function ()
	{
		var self = this;

		$.getJSON(this.url.jsonUrl(Enum.TCPClientRequest.GetStatistics),
			function(result)
			{
				$("#BytesLoaded").html(self.helper.size2Human(result.BytesLoaded));

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

				$("#SpeedMax").html(self.helper.speed2Human(result.SpeedMax));
			}
		);
	},

	/* ************************************************************************************************************** */
	/* SNAPSHOT STUFF                                                                                                 */
	/* ************************************************************************************************************** */

	updateSnapshots: function ()
	{
		var self = this;

		$.getJSON(this.url.jsonUrl(Enum.TCPClientRequest.GetSnapshots),
			function(result)
			{
				$.each(result, function(index, item) {
					item.color = index;
					switch (index + 1)
					{
						case Enum.SnapshotValue.Speed:
						case Enum.SnapshotValue.BotsAverageCurrentSpeed:
						case Enum.SnapshotValue.BotsAverageMaxSpeed:
							item.yaxis = 3;
							break;

						case Enum.SnapshotValue.PacketsSize:
						case Enum.SnapshotValue.PacketsSizeConnected:
						case Enum.SnapshotValue.PacketsSizeDisconnected:
							item.yaxis = 2;
							break;

						default:
							item.yaxis = 1;
							break;
					}
				});

				self.snapshots = result;
				self.updateSnapshotPlot();
			}
		);
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
				mode: "time",
				timeformat: timeFormat,
				minTickSize: tickSize,
				monthNames: LANG_MONTH_SHORT
			},
			yaxes: [
				{
					min: 0
				},
				{
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
