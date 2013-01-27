// 
//  grid.js
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

var XGGridInstance = null;
var XGGrid = Class.create(
{
	/**
	 * @param {XGFormatter} formatter
	 * @param {XGHelper} helper
	 * @param {XGDataView} dataview
	 */
	initialize: function(formatter, helper, dataview)
	{
		XGGridInstance = this;

		this.formatter = formatter;
		this.helper = helper;
		this.dataview = dataview;
		this.grids = [];

		this.onClick = new Slick.Event();

		this.filterArgs = {
			ParentGuid: "",
			Name: ""
		};

		this.sortColumn = {};

		this.cache = {};
		this.searchTemplate = this.tmpl("searchTemplate");
	},

	build: function()
	{
		var self = this;

		/**************************************************************************************************************/

		this.serverGrid = this.buildGrid("#serverGrid", this.dataview.getDataView("Server"), [
			this.buildRow("Icon", 38, "icon-cell", false, function (obj)
			{
				return self.formatter.formatServerIcon(obj, "XG.flipPacket(\"" + obj.Guid + "\", \"servers_table\");");
			}, false),
			this.buildRow("Name", 214, "", true, $.proxy(self.formatter.formatServerChannelName, self.formatter), false)
		]);
		this.serverGrid.onClick.subscribe(function (e, args) {
			self.filterArgs.ParentGuid = self.serverGrid.getDataItem(args.row).Guid;
			self.filterArgs.Name = "";
			this.applyFilter(self.channelGrid.getData());
		}, self.compareServers);

		/**************************************************************************************************************/

		this.channelGrid = this.buildGrid("#channelGrid", this.dataview.getDataView("Channel"), [
			this.buildRow("Icon", 40, "icon-cell", false, function (obj)
			{
				return self.formatter.formatServerIcon(obj, "XG.flipPacket(\"" + obj.Guid + "\", \"channels_table\");");
			}, false),
			this.buildRow("Name", 212, "", true, $.proxy(self.formatter.formatServerChannelName, self.formatter), false)
		], self.compareChannels);

		/**************************************************************************************************************/

		this.botGrid = this.buildGrid("#botGrid", this.dataview.getDataView("Bot"), [
			this.buildRow("Icon", 34, "icon-cell", false, $.proxy(self.formatter.formatBotIcon, self.formatter), false),
			this.buildRow("Name", 0, "", true, $.proxy(self.formatter.formatBotName, self.formatter), false),
			this.buildRow("Speed", 70, "", true, function (obj)
			{
				return self.helper.speed2Human(obj.Speed);
			}, true),
			this.buildRow("Q-Position", 70, "", true, function (obj)
			{
				return obj.QueuePosition > 0 ? obj.QueuePosition : "&nbsp;"
			}, true),
			this.buildRow("Q-Time", 70, "", true, function (obj)
			{
				return self.helper.time2Human(obj.QueueTime);
			}, true),
			this.buildRow("Speed", 100, "", true, $.proxy(self.formatter.formatBotSpeed, self.formatter), true),
			this.buildRow("Slots", 60, "", true, $.proxy(self.formatter.formatBotSlots, self.formatter), true),
			this.buildRow("Queue", 60, "", true, $.proxy(self.formatter.formatBotQueue, self.formatter), true)
		], self.compareBots);
		this.botGrid.onClick.subscribe(function (e, args) {
			self.filterArgs.ParentGuid = self.botGrid.getDataItem(args.row).Guid;
			self.filterArgs.Name = "";
			this.applyFilter(self.packetGrid.getData());
		});

		/**************************************************************************************************************/

		this.packetGrid = this.buildGrid("#packetGrid", this.dataview.getDataView("Packet"), [
			this.buildRow("Icon", 38, "icon-cell", false, function (obj)
			{
				return self.formatter.formatPacketIcon(obj, "XG.flipPacket(\"" + obj.Guid + "\");");
			}, false),
			this.buildRow("#", 40, "", true, $.proxy(self.formatter.formatPacketId, self.formatter), true),
			this.buildRow("Name", 0, "progress-cell", true, $.proxy(self.formatter.formatPacketName, self.formatter), false),
			this.buildRow("Size", 70, "", true, $.proxy(self.formatter.formatPacketSize, self.formatter), true),
			this.buildRow("Speed", 70, "", true, $.proxy(self.formatter.formatPacketSpeed, self.formatter), true),
			this.buildRow("Time Missing", 90, "", true, $.proxy(self.formatter.formatPacketTimeMissing, self.formatter), true),
			this.buildRow("Last Updated", 135, "", true, function (obj)
			{
				return self.helper.date2Human(obj.LastUpdated);
			}, true)
		], self.comparePackets);

		/**************************************************************************************************************/

		this.searchGrid = this.searchGrid = this.buildGrid("#searchGrid", this.dataview.getDataView("Search"), [
			/*this.buildRow("Icon", 26, "icon-cell", false, $.proxy(self.formatter.formatSearchIcon, self.formatter), false),
			this.buildRow("Name", 0, "", false, function (obj)
			{
				return _(obj.Name);
			}, false),
			this.buildRow("Action", 18, "", false, $.proxy(self.formatter.formatSearchAction, self.formatter), false)*/
			{id: "contact-card", name: "Contacts", formatter: function (row, cell, value, columnDef, obj)
			{
				return self.searchTemplate(obj);
			}, width: 500, cssClass: "contact-card-cell"}
		]);
		this.searchGrid.onClick.subscribe(function (e, args) {
			self.filterArgs.ParentGuid = "";
			self.filterArgs.Name = self.searchGrid.getDataItem(args.row).Name;
			self.applyFilter(self.packetGrid.getData());
		});

		/**************************************************************************************************************/

		this.externalGrid = this.buildGrid("#externalGrid", this.dataview.getDataView("ExternalSearch"), [
			this.buildRow("Icon", 24, "icon-cell", false, function (obj)
			{
				return self.formatter.formatPacketIcon(obj, "XG.downloadLink(\"" + obj.Guid + "\");");
			}, false),
			this.buildRow("Id", 40, "", true, $.proxy(self.formatter.formatPacketId, self.formatter), true),
			this.buildRow("Name", 0, "", true, $.proxy(self.formatter.formatPacketName, self.formatter), false),
			this.buildRow("LastMentioned", 140, "", true, function (obj)
			{
				return self.helper.date2Human(obj.LastMentioned);
			}, true),
			this.buildRow("Size", 70, "", true, function (obj)
			{
				return self.helper.size2Human(obj.Size);
			}, true),
			this.buildRow("BotName", 160, "", true, function (obj)
			{
				return obj.BotName;
			}, false),
			this.buildRow("BotSpeed", 70, "", true, function (obj)
			{
				return self.helper.speed2Human(obj.BotSpeed);
			}, true)
		], self.compareExternals);

		/**************************************************************************************************************/

		this.fileGrid = this.buildGrid("#fileGrid", this.dataview.getDataView("File"), [
			this.buildRow("Icon", 24, "icon-cell", false, $.proxy(self.formatter.formatFileIcon, self.formatter), false),
			this.buildRow("Name", 0, "", true, $.proxy(self.formatter.formatFileName, self.formatter), false),
			this.buildRow("Size", 70, "", true, $.proxy(self.formatter.formatFileSize, self.formatter), true),
			this.buildRow("Speed", 70, "", true, $.proxy(self.formatter.formatFileSpeed, self.formatter), true),
			this.buildRow("TimeMissing", 90, "", true, $.proxy(self.formatter.formatFileTimeMissing, self.formatter), true)
		], self.compareFiles);
	},

	applyFilter: function(dataView)
	{
		dataView.setFilterArgs(this.filterArgs);
		dataView.refresh();
		dataView.reSort();
	},

	/**
	 * @param {String} id
	 * @param {Slick.Data.DataView} dataView
	 * @param {Array} columns
	 * @param {Function} comparer
	 * @return {Slick.Grid}
	 */
	buildGrid: function(id, dataView, columns, comparer)
	{
		var self = this;

		var grid = new Slick.Grid(id, dataView, columns,
			{
				editable: true,
				enableAddRow: true,
				enableCellNavigation: true,
				forceFitColumns : true
			}
		);
		//grid.setSelectionModel(new Slick.RowSelectionModel());

		dataView.onRowCountChanged.subscribe(function (e, args) {
			grid.updateRowCount();
			grid.render();
		});

		dataView.onRowsChanged.subscribe(function (e, args) {
			grid.invalidateRows(args.rows);
			grid.render();
		});

		grid.onClick.subscribe(function (e, args) {
			var obj = {
				object: grid.getDataItem(args.row),
				grid: id.substring(1)
			};
			self.onClick.notify(obj, null, self);
		});

		if (comparer != undefined)
		{
			grid.onSort.subscribe(function (e, args) {
				self.sortColumn[id.substring(1)] = args.sortCol.id;
				dataView.sort(comparer, args.sortAsc);
			});
		}

		this.grids.push(grid);
		return grid;
	},

	resort: function(args)
	{
		this.sortColumn[id.substring(1)] = args.sortCol.id;
		args.dataView.sort(args.comparer, args.sortAsc);
	},

	resize: function()
	{
		$.each(this.grids, function (i, grid)
		{
			grid.resizeCanvas();
		});
	},

	/**
	 * @param {String} id
	 * @param {Integer} width
	 * @param {String} cssClass
	 * @param {Boolean} sortable
	 * @param {Function} formatter
	 * @param {Boolean} alignRight
	 * @return {Object}
	 */
	buildRow: function (id, width, cssClass, sortable, formatter, alignRight)
	{
		if (alignRight)
		{
			cssClass = cssClass + " alignRight";
		}
		return {
			name: _(id),
			id: id,
			width: width > 0 ? width : undefined,
			minWidth: width > 0 ? width : undefined,
			maxWidth: width > 0 ? width : undefined,
			cssClass: cssClass != "" ? cssClass : undefined,
			sortable: sortable,
			cannotTriggerInsert: id == "Name",
			autoHeight:true,
			//resizable: false,
			formatter: function (row, cell, value, columnDef, obj)
			{
				return formatter(obj);
			}
		};
	},

    tmpl: function (str, data) {
		var self = this;
      // Figure out if we're getting a template, or if we need to
      // load the template - and be sure to cache the result.
      var fn = !/\W/.test(str) ?
          self.cache[str] = self.cache[str] ||
          self.tmpl(document.getElementById(str).innerHTML) :

        // Generate a reusable function that will serve as a template
        // generator (and which will be cached).
        new Function("obj",
            "var p=[],print=function(){p.push.apply(p,arguments);};" +

            // Introduce the data as local variables using with(){}
            "with(obj){p.push('" +

            // Convert the template into pure JavaScript
              str
                  .replace(/[\r\t\n]/g, " ")
                  .split("<%").join("\t")
                  .replace(/((^|%>)[^\t]*)'/g, "$1\r")
                  .replace(/\t=(.*?)%>/g, "',$1,'")
                  .split("\t").join("');")
                  .split("%>").join("p.push('")
                  .split("\r").join("\\'") + "');}return p.join('');");

      // Provide some basic currying to the user
      return data ? fn(data) : fn;
    },

	compareServers: function(a, b)
	{
		var name = XGGridInstance.sortColumn.serverGrid;
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	},

	compareChannels: function(a, b)
	{
		var name = XGGridInstance.sortColumn.channelGrid;
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	},

	compareBots: function(a, b)
	{
		var name = XGGridInstance.sortColumn.botGrid;
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	},

	comparePackets: function(a, b)
	{
		var name = XGGridInstance.sortColumn.packetGrid;
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
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	},

	compareExternals: function(a, b)
	{
		var name = XGGridInstance.sortColumn.externalGrid;
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	},

	compareFiles: function(a, b)
	{
		var name = XGGridInstance.sortColumn.fileGrid;
		var x = a[name], y = b[name];
		return (x == y ? 0 : (x > y ? 1 : -1));
	}
});
