//
//  xg.js
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

var XG;
var XGBase = Class.create(
{
	/**
	 * @param {XGHelper} helper
	 * @param {XGRefresh} refresh
	 * @param {XGCookie} cookie
	 * @param {XGFormatter} formatter
	 * @param {XGWebsocket} websocket
	 */
	initialize: function(helper, refresh, cookie, formatter, websocket)
	{
		XG = this;
		var self = this;

		this.refresh = refresh;
		this.cookie = cookie;
		this.helper = helper;
		this.formatter = formatter;

		this.websocket = websocket;
		this.websocket.onConnected = self.onWebsocketConnected;
		this.websocket.onDisconnected = self.onWebsocketDisconnected;
		this.websocket.onMessageReceived = self.onWebsocketMessageReceived;

		this.idServer = 0;
		this.activeTab = 0;

		this.initializeGrids();
		this.initializeDialogs();
		this.initializeOthers();
	},

	initializeGrids: function()
	{
		var self = this;

		/* ********************************************************************************************************** */
		/* SERVER GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#servers_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name')],
			colModel: [
				{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return self.formatter.formatServerIcon(r, "XG.flipObject(\"" + r.Guid + "\", \"servers_table\");"); }, width:36, sortable: false, classes: "icon-cell"},
				{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:216, editable:true, fixed:false}
			],
			onSelectRow: function(guid)
			{
				self.idServer = guid;
				var server = self.getRowData("servers_table", guid);
				self.websocket.sendGuid(Enum.Request.ChannelsFromServer, server.Guid);
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('servers.sort.index', index);
				self.cookie.setCookie('servers.sort.sortorder', sortorder);
			},
			pager: $('#servers_pager'),
			rowNum: 1000,
			pgbuttons: false,
			pginput: false,
			recordtext: '',
			pgtext: '',
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			height: 300,
			sortname: self.cookie.getCookie('servers.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('servers.sort.sortorder', 'asc'),
			caption: _("Servers"),
			hidegrid: false
		}).navGrid('#servers_pager', {edit:false, search:false}, {},
		{
			mtype: "GET",
			url: "/",
			serializeEditData: function (postdata)
			{
				return { request: Enum.Request.AddServer, name: postdata.Name };
			}
		},
		{
			mtype: "GET",
			url: "/",
			serializeDelData: function (postdata)
			{
				return { request: Enum.Request.RemoveServer, guid: postdata.id };
			}
		});

		/* ********************************************************************************************************** */
		/* CHANNEL GRID                                                                                               */
		/* ********************************************************************************************************** */

		$("#channels_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name')],
			colModel: [
				{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return self.formatter.formatChannelIcon(r, "XG.flipObject(\"" + r.Guid + "\", \"channels_table\");"); }, width:36, sortable: false, classes: "icon-cell"},
				{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:216, editable:true, fixed:false}
			],
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('channels.sort.index', index);
				self.cookie.setCookie('channels.sort.sortorder', sortorder);
			},
			pager: $('#channels_pager'),
			rowNum: 1000,
			pgbuttons: false,
			pginput: false,
			recordtext: '',
			pgtext: '',
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			height: 300,
			sortname: self.cookie.getCookie('channels.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('channels.sort.sortorder', 'asc'),
			caption: _("Channels"),
			hidegrid: false
		}).navGrid('#channels_pager', {edit:false, search:false}, {},
		{
			mtype: "GET",
			url: "/",
			serializeEditData: function (postdata)
			{
				return { request: Enum.Request.AddChannel, name: postdata.Name, guid: self.idServer };
			}
		},
		{
			mtype: "GET",
			url: "/",
			serializeDelData: function (postdata)
			{
				return { request: Enum.Request.RemoveChannel, guid: postdata.id };
			}
		});

		/* ********************************************************************************************************** */
		/* BOT GRID                                                                                                   */
		/* ********************************************************************************************************** */

		$("#bots_table").jqGrid(
		{
			datatype: "local",
			cmTemplate:{fixed:true},
			colNames: ['', '', _('Name'), _('Speed'), _('Q-Pos'), _('Q-Time'), _('max Speed'), _('Slots'), _('Queue')],
			colModel: [
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return self.formatter.formatBotIcon(r); }, width:32, sortable: false, classes: "icon-cell"},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatBotName(r); }, fixed:false},
				{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return self.helper.speed2Human(r.Speed); }, width:70, align:"right"},
				{name:'QueuePosition',	index:'QueuePosition',	formatter: function(c, o, r) { return r.QueuePosition > 0 ? r.QueuePosition : "&nbsp;"; }, width:70, align:"right"},
				{name:'QueueTime',		index:'QueueTime',		formatter: function(c, o, r) { return self.helper.time2Human(r.QueueTime); }, width:70, align:"right"},
				{name:'InfoSpeedMax',	index:'InfoSpeedMax',	formatter: function(c, o, r) { return self.formatter.formatBotSpeed(r); }, width:100, align:"right"},
				{name:'InfoSlotTotal',	index:'InfoSlotTotal',	formatter: function(c, o, r) { return self.formatter.formatBotSlots(r); }, width:60, align:"right"},
				{name:'InfoQueueTotal',	index:'InfoQueueTotal',	formatter: function(c, o, r) { return self.formatter.formatBotQueue(r); }, width:60, align:"right"}
			],
			onSelectRow: function(guid)
			{
				self.websocket.sendGuid(Enum.Request.PacketsFromBot, guid);
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('bots.sort.index', index);
				self.cookie.setCookie('bots.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager: $('#bots_pager'),
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			height: 300,
			sortname: self.cookie.getCookie('bots.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('bots.sort.sortorder', 'asc'),
			caption: _("Bots"),
			hidegrid: false
		}).navGrid('#bots_pager', {edit:false, add:false, del:false, search:false});

		/* ********************************************************************************************************** */
		/* PACKET GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#packets_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Id'), _('Name'), _('Size'), _('Speed'), _('Time'), _('Updated')],
			colModel: [
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return self.formatter.formatPacketIcon(r, "XG.flipPacket(\"" + o.rowId + "\");"); }, width:36, sortable: false, classes: "icon-cell"},
				{name:'Id',				index:'Id',				formatter: function(c, o, r) { return self.formatter.formatPacketId(r); }, width:40, align:"right"},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatPacketName(r); }, fixed:false},
				{name:'Size',			index:'Size',			formatter: function(c, o, r) { return self.formatter.formatPacketSize(r); }, width:70, align:"right"},
				{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return self.formatter.formatPacketSpeed(r); }, width:70, align:"right"},
				{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return self.formatter.formatPacketTimeMissing(r) }, width:90, align:"right"},
				{name:'LastUpdated',	index:'LastUpdated',	formatter: function(c, o, r) { return r.LastUpdated; }, width:135, align:"right"}
			],
			onSelectRow: function(guid)
			{
				var pack = self.getRowData('packets_table', guid);
				if(pack)
				{
					$('#bots_table').setSelection(pack.ParentGuid, false);
				}
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('packets.sort.index', index);
				self.cookie.setCookie('packets.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager: $('#packets_pager'),
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			height: 300,
			sortname: self.cookie.getCookie('packets.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('packets.sort.sortorder', 'asc'),
			caption: _("Packets"),
			hidegrid: false
		}).navGrid('#packets_pager', {edit:false, add:false, del:false, search:false});

		/* ********************************************************************************************************** */
		/* SEARCH GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#search_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', '', ''],
			colModel: [
				{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return self.formatter.formatSearchIcon(r); }, width:26, sortable: false, classes: "icon-cell"},
				{name:'Name',	index:'Name',	formatter: function(c, o, r) { return _(r.Name); }, fixed: false, sortable: false},
				{name:'Action',	index:'Action',	formatter: function(c, o, r) { return self.formatter.formatSearchAction(r); }, width:18, sortable: false}
			],
			onSelectRow: function(guid)
			{
				var search = self.getRowData("search_table", guid);

				switch(self.activeTab)
				{
					case 0:
						self.websocket.sendGuid(Enum.Request.Search, search.Guid);
						break;
					case 1:
						var gridElement = $("#searches_xg_bitpir_at");
						gridElement.clearGridData();
						gridElement.setGridParam({url: "http://xg.bitpir.at/index.php?show=search&action=json&do=search_packets&searchString=" + search.Name, page: 1});
						gridElement.trigger("reloadGrid");
						break;
				}
			},
			pager: $('#search_pager'),
			pgbuttons: false,
			pginput: false,
			recordtext: '',
			pgtext: '',
			sortname: 'Name',
			ExpandColumn : 'Name',
			viewrecords: true,
			autowidth: true,
			sortorder: "desc",
			hidegrid: false
		});

		$("#search-text").keyup( function (e)
		{
			if (e.which == 13)
			{
				self.addSearch();
			}
		});

		/**************************************************************************************************************/
		/* SEARCH GRID                                                                                                */
		/**************************************************************************************************************/

		$("#searches_xg_bitpir_at").jqGrid(
		{
			url: "",
			datatype:'jsonp',
			cmTemplate:{fixed:true},
			colNames:['', '', _('Id'), _('Name'), _('Last Mentioned'), _('Size'), _('Bot'), _('Speed'), ''],
			colModel:[
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return self.formatter.formatPacketIcon(r, "XG.downloadLink(\"" + o.rowId + "\");", true); }, width:24, sortable: false},
				{name:'Id',				index:'Id',				formatter: function(c, o, r) { return self.formatter.formatPacketId(r); }, width:38, align:"right"},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatPacketName(r); }, fixed:false},
				{name:'LastMentioned',	index:'LastMentioned',	formatter: function(c, o, r) { return self.helper.timeStampToHuman(r.LastMentioned); }, width:140, align:"right"},
				{name:'Size',			index:'Size',			formatter: function(c, o, r) { return self.helper.size2Human(r.Size); }, width:60, align:"right"},
				{name:'BotName',		index:'BotName',		formatter: function(c, o, r) { return r.BotName; }, width:160},
				{name:'BotSpeed',		index:'BotSpeed',		formatter: function(c, o, r) { return self.helper.speed2Human(r.BotSpeed); }, width:80, align:"right"},

				{name:'IrcLink',		index:'IrcLink',		formatter: function(c, o, r) { return r.IrcLink; }, hidden:true}
			],
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('searches_xg_bitpir_at.sort.index', index);
				self.cookie.setCookie('searches_xg_bitpir_at.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager:$('#searches_pager_xg_bitpir_at'),
			viewrecords:true,
			ExpandColumn:'Name',
			height:'100%',
			autowidth:true,
			sortname: self.cookie.getCookie('searches_xg_bitpir_at.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('searches_xg_bitpir_at.sort.sortorder', 'asc'),
			caption: _("Search via xg.bitpir.at"),
			hidegrid: false
		}).navGrid('#searches_pager_xg_bitpir_at', {edit:false, add:false, del:false, search:false});

		/* ********************************************************************************************************** */
		/* FILE GRID                                                                                                  */
		/* ********************************************************************************************************** */

		$("#files_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name'), _('Size'), _('Speed'), _('Time')],
			colModel: [
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return self.formatter.formatFileIcon(r); }, width:24, sortable: false},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatFileName(r); }, fixed:false},
				{name:'Size',			index:'Size',			formatter: function(c, o, r) { return self.formatter.formatFileSize(r); }, width:70, align:"right"},
				{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return self.formatter.formatFileSpeed(r); }, width:70, align:"right"},
				{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return self.formatter.formatFileTimeMissing(r) }, width:90, align:"right"}
			],
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('files.sort.index', index);
				self.cookie.setCookie('files.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager: $('#files_pager'),
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			height: 300,
			sortname: self.cookie.getCookie('files.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('files.sort.sortorder', 'asc'),
			caption: _("Files"),
			hidegrid: false
		}).navGrid('#files_pager', {edit:false, add:false, del:false, search:false});
	},

	initializeDialogs: function()
	{
		var self = this;

		/* ********************************************************************************************************** */
		/* SERVER / CHANNEL DIALOG                                                                                    */
		/* ********************************************************************************************************** */

		$("#server_channel_button")
			.button({icons: { primary: "ui-icon-gear" }})
			.click( function()
			{
				$("#dialog_server_channels").dialog("open");
			});

		$("#dialog_server_channels").dialog({
			autoOpen: false,
			width: 560,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* STATISTICS DIALOG                                                                                          */
		/* ********************************************************************************************************** */

		$("#statistics_button")
			.button({icons: { primary: "ui-icon-comment" }})
			.click( function()
			{
				self.refresh.refreshStatistic();
				$("#dialog_statistics").dialog("open");
			});

		$("#dialog_statistics").dialog({
			autoOpen: false,
			width: 545,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* SNAPSHOTS DIALOG                                                                                           */
		/* ********************************************************************************************************** */

		//$(".snapshot_checkbox").button();
		$(".snapshot_checkbox, input[name='snapshot_time']").click( function()
		{
			self.refresh.updateSnapshotPlot();
		});

		$("#snapshots_button")
			.button({icons: { primary: "ui-icon-comment" }})
			.click( function()
			{
				$("#dialog_snapshots").dialog("open");
			});

		$("#dialog_snapshots").dialog({
			autoOpen: false,
			width: $(window).width() - 20,
			height: $(window).height() - 20,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* ERROR DIALOG                                                                                               */
		/* ********************************************************************************************************** */

		$("#dialog_error").dialog({
			autoOpen: false,
			modal: true,
			resizable: false,
			close: function()
			{
				$('#dialog_error').dialog('open');
			}
		});
	},

	initializeOthers: function()
	{
		var self = this;

		$("#tabs").tabs({
			select: function(event, ui)
			{
				self.activeTab = ui.index;
			}
		});
		$("#show_offline_bots").button()
			.click( function()
			{
				self.cookie.setCookie("show_offline_bots", $("#show_offline_bots").attr('checked') ? "1" : "0" );
			});
	},

	addSearch: function ()
	{
		var tbox = $('#search-text');
		if(tbox.val() != "")
		{
			this.websocket.sendName(Enum.Request.AddSearch, tbox.val());
			tbox.val('');
		}
	},

	/**
	 * @param {String} guid
	 */
	removeSearch: function (guid)
	{
		this.websocket.sendGuid(Enum.Request.RemoveSearch, guid);
	},

	/**
	 * @param {String} guid
	 */
	flipPacket: function (guid)
	{
		var self = this;

		var pack = self.getRowData('packets_table', guid);
		if(pack)
		{
			if(!pack.Enabled)
			{
				$("#" + guid).effect("transfer", { to: $("#4") }, 500);
			}
			else
			{
				$("#4").effect("transfer", { to: $("#" + guid) }, 500);
			}
			self.flipObject(guid, "packets_table");
		}
	},

	/**
	 * @param {String} guid
	 * @param {String} grid
	 */
	flipObject: function (guid, grid)
	{
		var self = this;

		var obj = self.getRowData(grid, guid);
		if(obj)
		{
			if(!obj.Enabled)
			{
				self.websocket.sendGuid(Enum.Request.ActivateObject, guid);
			}
			else
			{
				self.websocket.sendGuid(Enum.Request.DeactivateObject, guid);
			}
		}
	},

	/**
	 * @param {String} guid
	 */
	downloadLink: function (guid)
	{
		var self = XG;

		$("#" + guid).effect("transfer", { to: $("#4") }, 500);

		var data = self.getRowData("searches_xg_bitpir_at", guid);
		self.websocket.sendName(Enum.Request.ParseXdccLink, data.IrcLink);
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

	onWebsocketConnected: function ()
	{
		var self = XG;

		self.websocket.send(Enum.Request.Searches);
		self.websocket.send(Enum.Request.Servers);
		self.websocket.send(Enum.Request.Files);
		//self.websocket.send(Enum.Request.GetSnapshots);
	},

	onWebsocketDisconnected: function ()
	{
		$("#dialog_error").dialog("open");
	},

	onWebsocketMessageReceived: function (json)
	{
		var self = XG;

		var grid = "";
		switch (json.DataType)
		{
			case "Server":
				grid = "servers_table";
				break;
			case "Channel":
				grid = "channels_table";
				break;
			case "Bot":
				grid = "bots_table";
				break;
			case "Packet":
				grid = "packets_table";
				break;
			case "Search":
				grid = "search_table";
				break;
			case "Snapshot":
				break;
		}

		switch (json.Type)
		{
			case Enum.Response.ObjectAdded:
				if (grid != "")
				{
					if (grid == "search_table")
					{
						self.addGridItem("search_table", json.Data, true);
						$("#search-text").effect("transfer", { to: $("#" + json.Data.Guid) }, 500);
					}
					else
					{
						self.addGridItem(grid, json.Data);
					}
				}
				break;
			case Enum.Response.ObjectRemoved:
				if (grid != "")
				{
					if (grid == "search_table")
					{
						$("#" + json.Data.Guid).effect("transfer", { to: $("#search-text") }, 500);
						self.removeGridItem("search_table", json.Data, true);
					}
					else
					{
						self.removeGridItem(grid, json.Data);
					}
				}
				break;
			case Enum.Response.ObjectChanged:
				if (grid != "")
				{
					self.updateGridItem(grid, json.Data);
				}
				break;

			case Enum.Response.SearchPacket:
				self.setGridData("packets_table", json.Data);
				break;
			case Enum.Response.SearchBot:
				self.setGridData("bots_table", json.Data);
				break;

			case Enum.Response.Servers:
				self.setGridData("servers_table", json.Data);
				break;
			case Enum.Response.ChannelsFromServer:
				self.setGridData("channels_table", json.Data);
				break;
			case Enum.Response.PacketsFromBot:
				self.setGridData("packets_table", json.Data);
				break;

			case Enum.Response.Files:
				self.setGridData("files_table", json.Data);
				break;
			case Enum.Response.Searches:
				self.setGridData("search_table", json.Data, true);
				break;
		}
	},

	setGridData: function (grid, data, skipReload)
	{
		var gridElement = $("#" + grid);
		gridElement.clearGridData();
		$.each(data, function(i, item)
		{
			gridElement.addRowData(item.Guid, item);
		});

		if (!skipReload)
		{
			gridElement.trigger("reloadGrid");
		}
	},

	addGridItem: function (grid, item, skipReload)
	{
		var gridElement = $("#" + grid);
		gridElement.addRowData(item.Guid, item);

		if (!skipReload)
		{
			gridElement.trigger("reloadGrid");
		}
	},

	updateGridItem: function (grid, item, skipReload)
	{
		var gridElement = $("#" + grid);
		gridElement.setRowData(item.Guid, item);

		if (!skipReload)
		{
			gridElement.trigger("reloadGrid");
		}
	},

	removeGridItem: function (grid, item, skipReload)
	{
		var gridElement = $("#" + grid);
		gridElement.delRowData(item.Guid);

		if (!skipReload)
		{
			gridElement.trigger("reloadGrid");
		}
	}
});
