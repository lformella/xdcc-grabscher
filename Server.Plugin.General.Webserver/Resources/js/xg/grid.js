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
var XGGrid = (function()
{
	var formatter, helper, dataview;

	var serverGrid, channelGrid, botGrid, packetGrid, searchGrid, externalGrid, fileGrid;
	var grids = [];

	var sortColumn = { Server: null, Channel: null, Bot: null, Packet: null, ExternalSearch: null, File: null };
	var filterOfflineBots = false;

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
				forceFitColumns : true,
				rowHeight: rowHeight
			}
		);
		grid.setSelectionModel(new Slick.RowSelectionModel());

		dataView.onRowCountChanged.subscribe(function (e, args) {
			grid.updateRowCount();
			grid.render();
		});

		dataView.onRowsChanged.subscribe(function (e, args) {
			grid.invalidateRows(args.rows);
			grid.render();
		});

		grid.onClick.subscribe($.proxy(function (e, args) {
			var obj = {
				Data: grid.getDataItem(args.row),
				DataType: gridName,
				Cell: $(grid.getCellNode(args.row, args.cell))
			};
			self.onClick.notify(obj, null, this);
		}, this));

		if (comparer != null)
		{
			grid.onSort.subscribe(function (e, args) {
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
			name: _(id),
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
			case Enum.Grid.Search:
				dataView = searchGrid.getData();
				filter = {};
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
		 */
		initialize: function(formatter1, helper1, dataview1)
		{
			formatter = formatter1;
			helper = helper1;
			dataview = dataview1;
			Grid = this;
		},

		/**
		 * @param {string} name
		 * @return {SlickGrid}
		 */
		getGrid: function(name)
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
				case Enum.Grid.Search:
					return searchGrid;
				case Enum.Grid.ExternalSearch:
					return externalGrid;
				case Enum.Grid.File:
					return fileGrid;
			}

			return null;
		},

		build: function()
		{
			/**************************************************************************************************************/

			serverGrid = buildGrid(Enum.Grid.Server, dataview.getDataView(Enum.Grid.Server), [
				buildRow("Icon", 38, false, function (obj)
				{
					return formatter.formatServerIcon(obj, "Grid.flipObject(\"" + Enum.Grid.Server + "\", \"" + obj.Guid + "\");");
				}, false, "small"),
				buildRow("Name", 0, true, $.proxy(formatter.formatServerName, formatter), false),
				buildRow("", 20, false, function (obj)
				{
					return formatter.formatRemoveIcon(Enum.Grid.Server, obj);
				}, false)
			], compareServers);
			serverGrid.onClick.subscribe(function (e, args) {
				var obj = serverGrid.getDataItem(args.row);
				if (obj != undefined)
				{
					channelFilter = { ParentGuid: obj.Guid };
					applyFilter(Enum.Grid.Channel);
				}
			});

			/**************************************************************************************************************/

			channelGrid = buildGrid(Enum.Grid.Channel, dataview.getDataView(Enum.Grid.Channel), [
				buildRow("Icon", 40, false, function (obj)
				{
					return formatter.formatChannelIcon(obj, "Grid.flipObject(\"" + Enum.Grid.Channel + "\", \"" + obj.Guid + "\");");
				}, false, "small"),
				buildRow("Name", 0, true, $.proxy(formatter.formatChannelName, formatter), false),
				buildRow("", 20, false, function (obj)
				{
					return formatter.formatRemoveIcon(Enum.Grid.Channel, obj);
				}, false)
			], compareChannels);

			/**************************************************************************************************************/

			botGrid = buildGrid(Enum.Grid.Bot, dataview.getDataView(Enum.Grid.Bot), [
				buildRow("Icon", 38, false, $.proxy(formatter.formatBotIcon, formatter), false, "small"),
				buildRow("Name", 0, true, $.proxy(formatter.formatBotName, formatter), false, "small"),
				buildRow("Speed", 70, true, function (obj)
				{
					return helper.speed2Human(obj.Speed);
				}, true),
				buildRow("Q-Position", 70, true, function (obj)
				{
					return obj.QueuePosition > 0 ? obj.QueuePosition : "&nbsp;"
				}, true),
				buildRow("Q-Time", 70, true, function (obj)
				{
					return helper.time2Human(obj.QueueTime);
				}, true),
				buildRow("Speed", 100, true, $.proxy(formatter.formatBotSpeed, formatter), true),
				buildRow("Slots", 60, true, $.proxy(formatter.formatBotSlots, formatter), true),
				buildRow("Queue", 60, true, $.proxy(formatter.formatBotQueue, formatter), true)
			], compareBots);
			botGrid.onClick.subscribe(function (e, args) {
				packetFilter = { ParentGuid: botGrid.getDataItem(args.row).Guid };
				applyFilter(Enum.Grid.Packet);
			});

			/**************************************************************************************************************/

			packetGrid = buildGrid(Enum.Grid.Packet, dataview.getDataView(Enum.Grid.Packet), [
				buildRow("Icon", 42, false, function (obj)
				{
					return formatter.formatPacketIcon(obj, "Grid.flipObject(\"" + Enum.Grid.Packet + "\", \"" + obj.Guid + "\");");
				}, false, "small"),
				buildRow("#", 40, true, $.proxy(formatter.formatPacketId, formatter), true),
				buildRow("Name", 0, true, $.proxy(formatter.formatPacketName, formatter), false, "medium"),
				buildRow("Size", 70, true, $.proxy(formatter.formatPacketSize, formatter), true),
				buildRow("Speed", 70, true, $.proxy(formatter.formatPacketSpeed, formatter), true),
				buildRow("Time Missing", 90, true, $.proxy(formatter.formatPacketTimeMissing, formatter), true),
				buildRow("Last Updated", 135, true, function (obj)
				{
					return helper.date2Human(obj.LastUpdated);
				}, true)
			], comparePackets);

			/**************************************************************************************************************/
/*
			searchGrid = buildGrid(Enum.Grid.Search, dataview.getDataView(Enum.Grid.Search), [
				buildRow("Data", 0, false, $.proxy(formatter.formatSearchCell, formatter))
			], null, 30);
			searchGrid.onClick.subscribe(function (e, args) {
				var obj = searchGrid.getDataItem(args.row);
				applySearchFilter(obj);
			}, false);
			$("#SearchGrid .slick-header-columns").css("height", "0px");
			searchGrid.resizeCanvas();
*/
			/**************************************************************************************************************/

			externalGrid = buildGrid(Enum.Grid.ExternalSearch, dataview.getDataView(Enum.Grid.ExternalSearch), [
				buildRow("Icon", 28, false, function (obj)
				{
					return formatter.formatPacketIcon(obj, "Grid.downloadLink(\"" + obj.Guid + "\");");
				}, false, "small"),
				buildRow("#", 40, true, $.proxy(formatter.formatPacketId, formatter), true),
				buildRow("Name", 0, true, $.proxy(formatter.formatPacketName, formatter), false),
				buildRow("LastMentioned", 140, true, function (obj)
				{
					return helper.date2Human(obj.LastMentioned);
				}, true),
				buildRow("Size", 70, true, function (obj)
				{
					return helper.size2Human(obj.Size);
				}, true),
				buildRow("BotName", 160, true, function (obj)
				{
					return obj.BotName;
				}, false),
				buildRow("BotSpeed", 70, true, function (obj)
				{
					return helper.speed2Human(obj.BotSpeed);
				}, true)
			], compareExternals);

			/**************************************************************************************************************/

			fileGrid = buildGrid(Enum.Grid.File, dataview.getDataView(Enum.Grid.File), [
				buildRow("Icon", 28, false, $.proxy(formatter.formatFileIcon, formatter), false, "small"),
				buildRow("Name", 0, true, $.proxy(formatter.formatFileName, formatter), false, "medium"),
				buildRow("Size", 70, true, $.proxy(formatter.formatFileSize, formatter), true),
				buildRow("Speed", 70, true, $.proxy(formatter.formatFileSpeed, formatter), true),
				buildRow("Time Missing", 90, true, $.proxy(formatter.formatFileTimeMissing, formatter), true)
			], compareFiles);

			/**************************************************************************************************************/

			// default filter
			self.applySearchFilter({ Guid: "00000000-0000-0000-0000-000000000002" });
		},

		/**
		 * @param {Boolean} filterOfflineBots1
		 */
		setFilterOfflineBots: function(filterOfflineBots1)
		{
			filterOfflineBots = filterOfflineBots1;
			applyFilter(Enum.Grid.Bot);
			applyFilter(Enum.Grid.Packet);
		},

		invalidate: function()
		{
			$.each(grids, function (i, grid)
			{
				grid.invalidate();
			});
		},

		resize: function()
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

		applySearchFilter: function  (obj)
		{
			dataview.resetBotFilter();

			packetFilter = { SearchGuid: obj.Guid, Name: obj.Name };
			applyFilter(Enum.Grid.Packet);

			externalFilter = packetFilter;
			applyFilter(Enum.Grid.ExternalSearch);

			applyFilter(Enum.Grid.Bot);
		}
	};
	return self;
}());
