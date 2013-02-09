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

	var sortColumn = { serverGrid: null, channelGrid: null, botGrid: null, packetGrid: null, externalGrid: null, fileGrid: null };
	var filterOfflineBots = false;

	var channelFilter = {}, botFilter = {}, packetFilter = {}, externalFilter = {};

	/**
	 * @param {String} id
	 * @param {Slick.Data.DataView} dataView
	 * @param {Array} columns
	 * @param {Function} comparer
	 * @return {Slick.Grid}
	 */
	function buildGrid (id, dataView, columns, comparer)
	{
		var grid = new Slick.Grid(id, dataView, columns,
			{
				editable: false,
				enableAddRow: false,
				enableCellNavigation: true,
				forceFitColumns : true
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
				object: grid.getDataItem(args.row),
				grid: id.substring(1),
				cell: $(grid.getCellNode(args.row, args.cell))
			};
			self.onClick.notify(obj, null, this);
		}, this));

		if (comparer != null)
		{
			grid.onSort.subscribe(function (e, args) {
				sortColumn[id.substring(1)] = args.sortCol.id;
				dataView.sort(comparer, args.sortAsc);
			});
		}
		grid.setSortColumn("Name",true);

		grids.push(grid);
		return grid;
	}

	/**
	 * @param {String} id
	 * @param {Integer} width
	 * @param {Boolean} sortable
	 * @param {Function} formatter
	 * @param {Boolean} alignRight
	 * @return {Object}
	 */
	function buildRow (id, width, sortable, formatter, alignRight)
	{
		return {
			name: _(id),
			id: id,
			width: width > 0 ? width : undefined,
			minWidth: width > 0 ? width : undefined,
			maxWidth: width > 0 ? width : undefined,
			cssClass: alignRight ? "alignRight" : undefined,
			sortable: sortable,
			cannotTriggerInsert: id == "Name",
			autoHeight: true,
			//resizable: false,
			formatter: function (row, cell, value, columnDef, obj)
			{
				return formatter(obj);
			}
		};
	}

	/**
	 * @param obj {Object}
	 */
	function applySearchFilter (obj)
	{
		dataview.resetBotFilter();

		packetFilter = { SearchGuid: obj.Guid, Name: obj.Name };
		applyFilter(Enum.Grid.Packet);

		externalFilter = packetFilter;
		applyFilter(Enum.Grid.ExternalSearch);

		applyFilter(Enum.Grid.Bot);
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
		return compare(a, b, sortColumn.serverGrid);
	}

	function compareChannels (a, b)
	{
		return compare(a, b, sortColumn.channelGrid);
	}

	function compareBots (a, b)
	{
		return compare(a, b, sortColumn.botGrid);
	}

	function comparePackets (a, b)
	{
		var name = sortColumn.packetGrid;
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
		return compare(a, b, sortColumn.externalGrid);
	}

	function compareFiles (a, b)
	{
		return compare(a, b, sortColumn.fileGrid);
	}

	function compare (a, b, name)
	{
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	}

	var self = {
		onClick: new Slick.Event(),
		onFlipObject: new Slick.Event(),
		onFlipPacket: new Slick.Event(),
		onRemoveSearch: new Slick.Event(),
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

			serverGrid = buildGrid("#serverGrid", dataview.getDataView(Enum.Grid.Server), [
				buildRow("Icon", 38, false, function (obj)
				{
					return formatter.formatServerIcon(obj, "Grid.flipServer(\"" + obj.Guid + "\", \"servers_table\");");
				}, false),
				buildRow("Name", 0, true, $.proxy(formatter.formatServerChannelName, formatter), false)
			], compareServers);
			/*serverGrid.onClick.subscribe(function (e, args) {
				var obj = serverGrid.getDataItem(args.row);
				if (obj != undefined)
				{
					channelFilter = { ParentGuid: obj.Guid };
					applyFilter(Enum.Grid.Channel);
				}
			});*/
			serverGrid.onAddNewRow.subscribe(function (e, args) {
				var item = args.item;
				/*
				serverGrid.invalidateRow(data.length);
				serverGrid.push(item);
				serverGrid.updateRowCount();
				serverGrid.render();
				*/
			});
	
			/**************************************************************************************************************/
	
			channelGrid = buildGrid("#channelGrid", dataview.getDataView(Enum.Grid.Channel), [
				buildRow("Icon", 40, false, function (obj)
				{
					return formatter.formatServerIcon(obj, "Grid.flipChannel(\"" + obj.Guid + "\", \"channels_table\");");
				}, false),
				buildRow("Name", 0, true, $.proxy(formatter.formatServerChannelName, formatter), false)
			], compareChannels);
	
			/**************************************************************************************************************/
	
			botGrid = buildGrid("#botGrid", dataview.getDataView(Enum.Grid.Bot), [
				buildRow("Icon", 40, false, $.proxy(formatter.formatBotIcon, formatter), false),
				buildRow("Name", 0, true, $.proxy(formatter.formatBotName, formatter), false),
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
	
			packetGrid = buildGrid("#packetGrid", dataview.getDataView(Enum.Grid.Packet), [
				buildRow("Icon", 42, false, function (obj)
				{
					return formatter.formatPacketIcon(obj, "Grid.flipPacket(\"" + obj.Guid + "\");");
				}, false),
				buildRow("#", 40, true, $.proxy(formatter.formatPacketId, formatter), true),
				buildRow("Name", 0, true, $.proxy(formatter.formatPacketName, formatter), false),
				buildRow("Size", 70, true, $.proxy(formatter.formatPacketSize, formatter), true),
				buildRow("Speed", 70, true, $.proxy(formatter.formatPacketSpeed, formatter), true),
				buildRow("Time Missing", 90, true, $.proxy(formatter.formatPacketTimeMissing, formatter), true),
				buildRow("Last Updated", 135, true, function (obj)
				{
					return helper.date2Human(obj.LastUpdated);
				}, true)
			], comparePackets);
	
			/**************************************************************************************************************/
	
			searchGrid = buildGrid("#searchGrid", dataview.getDataView(Enum.Grid.Search), [
				buildRow("Icon", 28, false, $.proxy(formatter.formatSearchIcon, formatter), false),
				buildRow("Name", 0, false, function (obj)
				{
					return _(obj.Name);
				}, false),
				buildRow("Action", 20, false, $.proxy(formatter.formatSearchAction, formatter), false)
			], null);
			searchGrid.onClick.subscribe(function (e, args) {
				var obj = searchGrid.getDataItem(args.row);
				applySearchFilter(obj);
			}, false);
			$("#searchGrid .slick-header-columns").css("height", "0px");
			searchGrid.resizeCanvas();

			/**************************************************************************************************************/
	
			externalGrid = buildGrid("#externalGrid", dataview.getDataView(Enum.Grid.ExternalSearch), [
				buildRow("Icon", 24, false, function (obj)
				{
					return formatter.formatPacketIcon(obj, "Grid.downloadLink(\"" + obj.Guid + "\");");
				}, false),
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
	
			fileGrid = buildGrid("#fileGrid", dataview.getDataView(Enum.Grid.File), [
				buildRow("Icon", 24, false, $.proxy(formatter.formatFileIcon, formatter), false),
				buildRow("Name", 0, true, $.proxy(formatter.formatFileName, formatter), false),
				buildRow("Size", 70, true, $.proxy(formatter.formatFileSize, formatter), true),
				buildRow("Speed", 70, true, $.proxy(formatter.formatFileSpeed, formatter), true),
				buildRow("TimeMissing", 90, true, $.proxy(formatter.formatFileTimeMissing, formatter), true)
			], compareFiles);

			/**************************************************************************************************************/

			// default filter
			applySearchFilter({ Guid: "00000000-0000-0000-0000-000000000004" });
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

		flipServer: function (guid)
		{
			var obj = dataview.getDataView(Enum.Grid.Server).getItemById(guid);
			this.onFlipObject.notify(obj, null, this);
		},

		flipChannel: function (guid)
		{
			var obj = dataview.getDataView(Enum.Grid.Channel).getItemById(guid);
			this.onFlipObject.notify(obj, null, this);
		},

		flipPacket: function (guid)
		{
			var obj = dataview.getDataView(Enum.Grid.Packet).getItemById(guid);
			this.onFlipPacket.notify(obj, null, this);
		},

		removeSearch: function (guid)
		{
			var obj = dataview.getDataView(Enum.Grid.Search).getItemById(guid);
			this.onRemoveSearch.notify(obj, null, this);
		},

		downloadLink: function (guid)
		{
			var obj = dataview.getDataView(Enum.Grid.ExternalSearch).getItemById(guid);
			this.onDownloadLink.notify(obj, null, this);
		}
	};
	return self;
}());
