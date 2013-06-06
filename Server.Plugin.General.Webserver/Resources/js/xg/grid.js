// 
//  grid.js
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

var Grid = null;
var XGGrid = (function ()
{
	var formatter, helper, dataview, translate;

	var serverGrid, channelGrid, botGrid, packetGrid, externalGrid, fileGrid, notificationsGrid;
	var grids = [];

	var sortColumn = { Server: null, Channel: null, Bot: null, Packet: null, ExternalSearch: null, File: null };
	var filterOfflineBots = false;
	var combineBotAndPacketGrid = false;

	var channelFilter = {}, botFilter = {}, packetFilter = {}, externalFilter = {};

	/**
	 * @param {String} gridName
	 * @param {Slick.Data.DataView} dataView
	 * @param {Array} columns
	 * @param {Function} comparer
	 * @param {Integer} rowHeight
	 * @return {Slick.Grid}
	 */
	function buildGrid (gridName, dataView, columns, comparer, rowHeight)
	{
		var grid = new Slick.Grid("#" + gridName + "Grid", dataView, columns,
			{
				editable: false,
				enableAddRow: false,
				enableCellNavigation: true,
				forceFitColumns: true,
				rowHeight: rowHeight != undefined ? rowHeight : 32
			}
		);
		grid.setSelectionModel(new Slick.RowSelectionModel());

		dataView.onRowCountChanged.subscribe(function (e, args)
		{
			grid.updateRowCount();
			grid.render();
		});

		dataView.onRowsChanged.subscribe(function (e, args)
		{
			grid.invalidateRows(args.rows);
			grid.render();
		});

		grid.onClick.subscribe($.proxy(function (e, args)
		{
			var obj = {
				Data: grid.getDataItem(args.row),
				DataType: gridName,
				Cell: $(grid.getCellNode(args.row, args.cell))
			};
			self.onClick.notify(obj, null, this);
		}, this));

		if (comparer != null)
		{
			grid.onSort.subscribe(function (e, args)
			{
				sortColumn[gridName] = args.sortCol.id;
				dataView.sort(comparer, args.sortAsc);
			});
		}
		grid.setSortColumn("Name", true);

		grids.push(grid);
		return grid;
	}

	/**
	 * @param {String} id
	 * @param {Integer} width
	 * @param {Boolean} sortable
	 * @param {Function} formatter
	 * @param {Boolean} alignRight
	 * @param {String} cssClass
	 * @return {Object}
	 */
	function buildRow (id, width, sortable, formatter, alignRight, cssClass)
	{
		cssClass = cssClass == undefined ? "" : cssClass + " ";
		return {
			name: translate._(id),
			id: id,
			width: width > 0 ? width : undefined,
			minWidth: width > 0 ? width : undefined,
			maxWidth: width > 0 ? width : undefined,
			cssClass: cssClass + (alignRight ? "alignRight" : undefined),
			sortable: sortable,
			cannotTriggerInsert: id == "Name",
			autoHeight: true,
			formatter: function (row, cell, value, columnDef, obj)
			{
				return formatter(obj);
			}
		};
	}

	/**
	 * @param {String} grid
	 */
	function applyFilter (grid)
	{
		var dataView;
		var filter;

		switch (grid)
		{
			case Enum.Grid.Server:
				dataView = serverGrid.getData();
				filter = {};
				break;
			case Enum.Grid.Channel:
				dataView = channelGrid.getData();
				filter = channelFilter;
				break;
			case Enum.Grid.Bot:
				dataView = botGrid.getData();
				filter = botFilter;
				filter.OfflineBots = filterOfflineBots;
				break;
			case Enum.Grid.Packet:
				dataView = packetGrid.getData();
				filter = packetFilter;
				filter.OfflineBots = filterOfflineBots;
				break;
			case Enum.Grid.ExternalSearch:
				dataView = externalGrid.getData();
				filter = externalFilter;
				break;
			case Enum.Grid.File:
				dataView = fileGrid.getData();
				filter = {};
				break;
		}

		dataView.setFilterArgs(filter);
		dataView.refresh();
		dataView.reSort();
	}

	function compareServers (a, b)
	{
		return compare(a, b, sortColumn[Enum.Grid.Server]);
	}

	function compareChannels (a, b)
	{
		return compare(a, b, sortColumn[Enum.Grid.Channel]);
	}

	function compareBots (a, b)
	{
		return compare(a, b, sortColumn[Enum.Grid.Bot]);
	}

	function comparePackets (a, b)
	{
		var name = sortColumn[Enum.Grid.Packet];
		switch (name)
		{
			case "Time Missing":
				name = "TimeMissing";
				break;

			case "Last Updated":
				name = "LastUpdated";
				break;

			case "#":
				name = "Id";
				break;
		}
		return compare(a, b, name);
	}

	function compareExternals (a, b)
	{
		return compare(a, b, sortColumn[Enum.Grid.ExternalSearch]);
	}

	function compareFiles (a, b)
	{
		return compare(a, b, sortColumn[Enum.Grid.File]);
	}

	function compare (a, b, name)
	{
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	}

	var self = {
		onClick: new Slick.Event(),
		onFlipObject: new Slick.Event(),
		onRemoveObject: new Slick.Event(),
		onDownloadLink: new Slick.Event(),

		/**
		 * @param {XGFormatter} formatter1
		 * @param {XGHelper} helper1
		 * @param {XGDataView} dataview1
		 * @param {XGTranslate} translate1
		 * @param {Boolean} combineBotAndPacketGrid1
		 */
		initialize: function (formatter1, helper1, dataview1, translate1, combineBotAndPacketGrid1)
		{
			formatter = formatter1;
			helper = helper1;
			dataview = dataview1;
			translate = translate1;
			combineBotAndPacketGrid = combineBotAndPacketGrid1;
			Grid = this;
		},

		/**
		 * @param {string} name
		 * @return {SlickGrid}
		 */
		getGrid: function (name)
		{
			switch (name)
			{
				case Enum.Grid.Server:
					return serverGrid;
				case Enum.Grid.Channel:
					return channelGrid;
				case Enum.Grid.Bot:
					return botGrid;
				case Enum.Grid.Packet:
					return packetGrid;
				case Enum.Grid.ExternalSearch:
					return externalGrid;
				case Enum.Grid.File:
					return fileGrid;
				case Enum.Grid.Notification:
					return notificationsGrid;
			}

			return null;
		},

		build: function ()
		{
			/**************************************************************************************************************/

			serverGrid = buildGrid(Enum.Grid.Server, dataview.getDataView(Enum.Grid.Server), [
				buildRow("", 38, false, function (obj)
				{
					return formatter.formatServerIcon(obj, "Grid.flipObject(\"" + Enum.Grid.Server + "\", \"" + obj.Guid + "\");");
				}, false, "icon"),
				buildRow("Name", 0, true, $.proxy(formatter.formatServerName, formatter), false),
				buildRow("", 30, false, function (obj)
				{
					return formatter.formatRemoveIcon(Enum.Grid.Server, obj);
				}, false)
			], compareServers);
			serverGrid.onClick.subscribe(function (e, args)
			{
				var obj = serverGrid.getDataItem(args.row);
				if (obj != undefined)
				{
					channelFilter = { ParentGuid: obj.Guid };
					applyFilter(Enum.Grid.Channel);
				}
			});

			/**************************************************************************************************************/

			channelGrid = buildGrid(Enum.Grid.Channel, dataview.getDataView(Enum.Grid.Channel), [
				buildRow("", 40, false, function (obj)
				{
					return formatter.formatChannelIcon(obj, "Grid.flipObject(\"" + Enum.Grid.Channel + "\", \"" + obj.Guid + "\");");
				}, false, "icon"),
				buildRow("Name", 0, true, $.proxy(formatter.formatChannelName, formatter), false, "small-line"),
				buildRow("User", 80, false, function (obj)
				{
					return obj.UserCount;
				}, true),
				buildRow("", 30, false, function (obj)
				{
					return formatter.formatRemoveIcon(Enum.Grid.Channel, obj);
				}, false)
			], compareChannels);

			/**************************************************************************************************************/

			botGrid = buildGrid(Enum.Grid.Bot, dataview.getDataView(Enum.Grid.Bot), [
				buildRow("", 38, false, $.proxy(formatter.formatBotIcon, formatter), false, "icon"),
				buildRow("Name", 0, true, $.proxy(formatter.formatBotName, formatter), false, "small-line"),
				buildRow("Speed", 80, true, function (obj)
				{
					return helper.speed2Human(obj.Speed);
				}, true),
				buildRow("Q-Position", 70, true, function (obj)
				{
					return obj.QueuePosition > 0 ? obj.QueuePosition : "&nbsp;"
				}, true),
				buildRow("Q-Time", 120, true, function (obj)
				{
					return helper.time2Human(obj.QueueTime);
				}, true),
				buildRow("Speed", 130, true, $.proxy(formatter.formatBotSpeed, formatter), true),
				buildRow("Slots", 60, true, $.proxy(formatter.formatBotSlots, formatter), true),
				buildRow("Queue", 80, true, $.proxy(formatter.formatBotQueue, formatter), true)
			], compareBots);
			botGrid.onClick.subscribe(function (e, args)
			{
				packetFilter = { ParentGuid: botGrid.getDataItem(args.row).Guid };
				applyFilter(Enum.Grid.Packet);
			});

			/**************************************************************************************************************/

			packetGrid = buildGrid(Enum.Grid.Packet, dataview.getDataView(Enum.Grid.Packet), [
				buildRow("", 42, false, function (obj)
				{
					if (obj instanceof Slick.Group)
					{
						var bot = dataview.getItem(Enum.Grid.Bot, obj.groupingKey);
						var ret = "";
						if (bot != undefined)
						{
							ret += formatter.formatBotIcon(bot, true);
							ret += bot.Name;
						}
						return ret;
					}
					else
					{
						return  formatter.formatPacketIcon(obj, "Grid.flipObject(\"" + Enum.Grid.Packet + "\", \"" + obj.Guid + "\");");
					}
				}, false, "icon"),
				buildRow("", 55, true, $.proxy(formatter.formatPacketId, formatter), true),
				buildRow("Name", 0, true, $.proxy(formatter.formatPacketName, formatter), false),
				buildRow("Size", 70, true, $.proxy(formatter.formatPacketSize, formatter), true),
				buildRow("Speed", 80, true, $.proxy(formatter.formatPacketSpeed, formatter), true),
				buildRow("Time Missing", 120, true, $.proxy(formatter.formatPacketTimeMissing, formatter), true),
				buildRow("Last Updated", 155, true, function (obj)
				{
					return helper.date2Human(obj.LastUpdated);
				}, true)
			], comparePackets);
			self.setCombineBotAndPacketGrid(combineBotAndPacketGrid);

			/**************************************************************************************************************/

			externalGrid = buildGrid(Enum.Grid.ExternalSearch, dataview.getDataView(Enum.Grid.ExternalSearch), [
				buildRow("", 42, false, function (obj)
				{
					return formatter.formatPacketIcon(obj, "Grid.downloadLink(\"" + obj.Guid + "\");");
				}, false, "icon"),
				buildRow("", 55, true, $.proxy(formatter.formatPacketId, formatter), true),
				buildRow("Name", 0, true, $.proxy(formatter.formatPacketName, formatter), false),
				buildRow("LastMentioned", 155, true, function (obj)
				{
					return helper.date2Human(obj.LastMentioned);
				}, true),
				buildRow("Size", 70, true, function (obj)
				{
					return helper.size2Human(obj.Size);
				}, true),
				buildRow("BotName", 180, true, function (obj)
				{
					return obj.BotName;
				}, false),
				buildRow("BotSpeed", 80, true, function (obj)
				{
					return helper.speed2Human(obj.BotSpeed);
				}, true)
			], compareExternals);

			/**************************************************************************************************************/

			fileGrid = buildGrid(Enum.Grid.File, dataview.getDataView(Enum.Grid.File), [
				buildRow("", 28, false, $.proxy(formatter.formatFileIcon, formatter), false, "icon"),
				buildRow("Name", 0, true, $.proxy(formatter.formatFileName, formatter), false),
				buildRow("Size", 70, true, $.proxy(formatter.formatFileSize, formatter), true),
				buildRow("Speed", 80, true, $.proxy(formatter.formatFileSpeed, formatter), true),
				buildRow("Time Missing", 155, true, $.proxy(formatter.formatFileTimeMissing, formatter), true)
			], compareFiles);

			/**************************************************************************************************************/

			notificationsGrid = buildGrid(Enum.Grid.Notification, dataview.getDataView(Enum.Grid.Notification), [
				buildRow("", 28, false, $.proxy(formatter.formatNotificationIcon, formatter), false, "icon"),
				buildRow("Content", 0, true, $.proxy(formatter.formatNotificationContent, formatter), false, "two-line-text"),
				buildRow("Time", 155, true, $.proxy(formatter.formatNotificationTime, formatter), true, "two-line-text")
			], null, 48);

			/**************************************************************************************************************/

			// default filter
			self.applySearchFilter({ Guid: "00000000-0000-0000-0000-000000000002" });
		},

		/**
		 * @param {Boolean} filterOfflineBots1
		 */
		setFilterOfflineBots: function (filterOfflineBots1)
		{
			filterOfflineBots = filterOfflineBots1;
			applyFilter(Enum.Grid.Bot);
			applyFilter(Enum.Grid.Packet);
		},

		invalidate: function (grid)
		{
			if (grid != undefined)
			{
				self.getGrid(grid).invalidate();
			}
			else
			{
				$.each(grids, function (i, grid)
				{
					grid.invalidate();
				});
			}
		},

		resize: function ()
		{
			$.each(grids, function (i, grid)
			{
				grid.resizeCanvas();
			});
		},

		flipObject: function (grid, guid)
		{
			var obj = dataview.getDataView(grid).getItemById(guid);
			this.onFlipObject.notify({ DataType: grid, Data: obj }, null, this);
		},

		removeObject: function (grid, guid)
		{
			var obj = dataview.getDataView(grid).getItemById(guid);
			this.onRemoveObject.notify({ DataType: grid, Data: obj }, null, this);
		},

		downloadLink: function (guid)
		{
			var obj = dataview.getDataView(Enum.Grid.ExternalSearch).getItemById(guid);
			this.onDownloadLink.notify(obj, null, this);
		},

		applySearchFilter: function (obj)
		{
			dataview.resetBotFilter();

			packetFilter = { SearchGuid: obj.Guid, Name: obj.Name };
			applyFilter(Enum.Grid.Packet);

			externalFilter = packetFilter;
			applyFilter(Enum.Grid.ExternalSearch);

			applyFilter(Enum.Grid.Bot);
		},

		setCombineBotAndPacketGrid: function (enable)
		{
			var dataView = dataview.getDataView(Enum.Grid.Packet);
			var grid = $("#" + Enum.Grid.Bot + "Grid").hide();
			if (enable)
			{
				grid.hide();
				dataView.setGrouping(
				{
					getter: "ParentGuid",
					formatter: function (g)
					{
						return g.Value;
					}
				});
			}
			else
			{
				grid.show();
				dataView.setGrouping([]);
			}
		}
	};
	return self;
}());
