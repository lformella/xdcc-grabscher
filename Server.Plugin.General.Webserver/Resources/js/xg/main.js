//
//  main.js
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

var XGMain = (function()
{
	var statistics, cookie, helper, formatter, websocket, dataview, grid, resize;
	var activeTab = 0, enableSearchTransitions = false;
	var currentServerGuid = "";
	var serversActive = [], botsActive = [], searchesActive = [], externalSearchesActive = [];

	function initializeDialogs ()
	{
		/* ********************************************************************************************************** */
		/* SERVER / CHANNEL DIALOG                                                                                    */
		/* ********************************************************************************************************** */

		$("#serverChannelButton")
			.button({icons: { primary: "icon-globe" }})
			.click( function()
			{
				$("#dialogServerChannels").dialog("open");
			});

		$("#dialogServerChannels").dialog({
			autoOpen: false,
			width: 635,
			modal: true,
			resizable: false
		});

		$("#serverButton")
			.button(/*{icons: { primary: "icon-circle-plus" }}*/)
			.click( function()
			{
				var tbox = $("#server");
				if(tbox.val() != "")
				{
					websocket.sendName(Enum.Request.AddServer, tbox.val());
					tbox.val("");
				}
			});

		$("#channelButton")
			.button(/*{icons: { primary: "icon-circle-plus" }}*/)
			.click( function()
			{
				var tbox = $("#channel");
				if(tbox.val() != "")
				{
					websocket.sendNameGuid(Enum.Request.AddChannel, tbox.val(), currentServerGuid);
					tbox.val("");
				}
			});

		/* ********************************************************************************************************** */
		/* STATISTICS DIALOG                                                                                          */
		/* ********************************************************************************************************** */

		$("#statisticsButton")
			.button({icons: { primary: "icon-chart-bar" }})
			.click( function()
			{
				websocket.send(Enum.Request.Statistics);
				$("#dialogStatistics").dialog("open");
			});

		$("#dialogStatistics").dialog({
			autoOpen: false,
			width: 545,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* SNAPSHOTS DIALOG                                                                                           */
		/* ********************************************************************************************************** */

		//$(".snapshotCheckbox").button();
		$(".snapshotCheckbox, input[name='snapshotTime']").click( function()
		{
			statistics.updateSnapshotPlot();
		});

		$("#snapshotsButton")
			.button({icons: { primary: "icon-chart-bar" }})
			.click( function()
			{
				$("#dialogSnapshots").dialog("open");
			});

		$("#dialogSnapshots").dialog({
			autoOpen: false,
			width: $(window).width() - 20,
			height: $(window).height() - 20,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* ERROR DIALOG                                                                                               */
		/* ********************************************************************************************************** */

		$("#dialogError").dialog({
			autoOpen: false,
			modal: true,
			resizable: false,
			close: function()
			{
				$('#dialogError').dialog('open');
			}
		});
	}

	function initializeOthers ()
	{
		$("#searchText").keyup( function (e)
		{
			if (e.which == 13)
			{
				addSearch();
			}
		});

		$("#tabs").tabs({
			select: function(event, ui)
			{
				activeTab = ui.index;
			}
		});
		$('#tabs').bind('tabsshow', function(event, ui) {
			grid.resize();
		});

		var element1 = $("#showOfflineBots");
		element1
			.button({icons: { primary: "icon-eye" }})
			.click( function()
			{
				var checked = $("#showOfflineBots").attr("checked") == "checked";
				cookie.setCookie("showOfflineBots", checked ? "1" : "0" );
				grid.setFilterOfflineBots(checked);
			});
		element1.attr("checked", cookie.getCookie("showOfflineBots", "0") == "1");

		var element2 = $("#humanDates");
		element2
			.button({icons: { primary: "icon-clock" }})
			.click( function()
			{
				var checked = $("#humanDates").attr("checked") == "checked";
				cookie.setCookie("humanDates", checked ? "1" : "0" );
				helper.setHumanDates(checked);
				grid.invalidate();
			});
		element2.attr("checked", cookie.getCookie("humanDates", "0") == "1");
	}

	/**
	 * @param {String} gridName
	 * @param {String} guid
	 * @return {jQuery}
	 */
	function getElementFromGrid (gridName, guid)
	{
		var row = dataview.getDataView(gridName).getRowById(guid);
		return $(grid.getGrid(gridName).getCellNode(row, 1)).parent();
	}

	function addSearch ()
	{
		enableSearchTransitions = true;
		var tbox = $('#searchText');
		if(tbox.val() != "")
		{
			websocket.sendName(Enum.Request.AddSearch, tbox.val());
			tbox.val('');
		}
	}

	/**
	 * @param {Object} obj
	 */
	function flipPacket (obj)
	{
		var elementSearch = getElementFromGrid(Enum.Grid.Search, "00000000-0000-0000-0000-000000000004");
		var elementPacket = getElementFromGrid(Enum.Grid.Packet, obj.Guid);
		if(!obj.Enabled)
		{
			elementPacket.effect("transfer", { to: elementSearch }, 500);
		}
		else
		{
			elementSearch.effect("transfer", { to: elementPacket }, 500);
		}
		flipObject(obj);
	}

	/**
	 * @param {Object} obj
	 */
	function flipObject (obj)
	{
		if(obj)
		{
			if(!obj.Enabled)
			{
				websocket.sendGuid(Enum.Request.ActivateObject, obj.Guid);
			}
			else
			{
				websocket.sendGuid(Enum.Request.DeactivateObject, obj.Guid);
			}
		}
	}

	/**
	 * @param {Object} obj
	 */
	function downloadLink (obj)
	{
		var elementSearch = getElementFromGrid(Enum.Grid.Search, "00000000-0000-0000-0000-000000000004");
		var elementPacket = getElementFromGrid(Enum.Grid.ExternalSearch, obj.Guid);
		elementPacket.effect("transfer", { to: elementSearch }, 500);
		websocket.sendName(Enum.Request.ParseXdccLink, obj.IrcLink);
	}

	return {
		/**
		 * @param {XGHelper} helper1
		 * @param {XGStatistics} statistics1
		 * @param {XGCookie} cookie1
		 * @param {XGFormatter} formatter1
		 * @param {XGWebsocket} websocket1
		 * @param {XGDataView} dataview1
		 * @param {XGGrid} grid1
		 * @param {XGResize} resize1
		 */
		initialize: function(helper1, statistics1, cookie1, formatter1, websocket1, dataview1, grid1, resize1)
		{
			statistics = statistics1;
			cookie = cookie1;
			helper = helper1;
			formatter = formatter1;
			websocket = websocket1;
			dataview = dataview1;
			grid = grid1;
			resize = resize1;
		},

		start: function()
		{
			// socket
			websocket.onDisconnected.subscribe(function (e, args) {
				$("#dialogError").dialog("open");
			});
			websocket.onError.subscribe(function (e, args) {
				$("#dialogError").dialog("open");
			});
			websocket.onAdd.subscribe(function (e, args) {
				dataview.addItem(args);

				switch (args.DataType)
				{
					case Enum.Grid.Search:
						if (enableSearchTransitions)
						{
							$("#searchText").effect("transfer", { to: getElementFromGrid(Enum.Grid.Search, args.Data.Guid) }, 500);
						}
						break;
				}
			});
			websocket.onRemove.subscribe(function (e, args) {
				switch (args.DataType)
				{
					case Enum.Grid.Search:
						if (enableSearchTransitions)
						{
							getElementFromGrid(Enum.Grid.Search, args.Data.Guid).effect("transfer", { to: $("#searchText") }, 500);
						}
						break;
				}

				dataview.removeItem(args);
			});
			websocket.onUpdate.subscribe(function (e, args) {
				dataview.updateItem(args);
			});
			websocket.onSnapshots.subscribe(function (e, args) {
				statistics.setSnapshots(args.Data);
			});
			websocket.onRequestComplete.subscribe(function (e, args) {
				var newArgs = { Data: null, DataType: Enum.Grid.Search };

				switch (args.Data)
				{
					case Enum.Request.ChannelsFromServer:
						newArgs.DataType = Enum.Grid.Server;
						newArgs.Data = dataview.getDataView(newArgs.DataType).getItemById(serversActive.shift());
						break;

					case Enum.Request.PacketsFromBot:
						newArgs.DataType = Enum.Grid.Bot;
						newArgs.Data = dataview.getDataView(newArgs.DataType).getItemById(botsActive.shift());
						break;

					case Enum.Request.Search:
						newArgs.Data = dataview.getDataView(newArgs.DataType).getItemById(searchesActive.shift());
						break;

					case Enum.Request.SearchExternal:
						newArgs.Data = dataview.getDataView(newArgs.DataType).getItemById(externalSearchesActive.shift());
						break;
				}

				if (newArgs.Data != null)
				{
					newArgs.Data.Active = false;
					dataview.updateItem(newArgs);
				}
			});
			websocket.connect();

			// grid
			resize.onResize.subscribe(function (e, args) {
				grid.resize();
				statistics.resize();
			});
			grid.onClick.subscribe(function (e, args) {
				if (args.Data != undefined)
				{
					var active = true;
					switch (args.DataType)
					{
						case Enum.Grid.Server:
							websocket.sendGuid(Enum.Request.ChannelsFromServer, args.Data.Guid);
							grid.getGrid(Enum.Grid.Channel).setSelectedRows([]);
							currentServerGuid = args.Data.Guid;
							$("#channelPanel").show();
							serversActive.push(args.Data.Guid);
							break;

						case Enum.Grid.Bot:
							websocket.sendGuid(Enum.Request.PacketsFromBot, args.Data.Guid);
							grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
							botsActive.push(args.Data.Guid);
							break;

						case Enum.Grid.Packet:
							var row = dataview.getDataView(Enum.Grid.Bot).getRowById(args.Data.ParentGuid);
							grid.getGrid(Enum.Grid.Bot).scrollRowIntoView(row, false);
							grid.getGrid(Enum.Grid.Bot).setSelectedRows([row]);
							break;

						case Enum.Grid.Search:
							if (activeTab == 0)
							{
								grid.getGrid(Enum.Grid.Bot).setSelectedRows([]);
								grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
								websocket.sendGuid(Enum.Request.Search, args.Data.Guid);
								searchesActive.push(args.Data.Guid);
							}
							else if (activeTab == 1)
							{
								if (args.Data.Guid != "00000000-0000-0000-0000-000000000001" && args.Data.Guid != "00000000-0000-0000-0000-000000000002" && args.Data.Guid != "00000000-0000-0000-0000-000000000003" && args.Data.Guid != "00000000-0000-0000-0000-000000000004")
								{
									websocket.sendGuid(Enum.Request.SearchExternal, args.Data.Guid);
									externalSearchesActive.push(args.Data.Guid);
								}
								else
								{
									active = false;
								}
							}
							break;
					}

					if (active)
					{
						args.Data.Active = true;
						dataview.updateItem(args);
					}
				}
			});
			grid.onRemoveObject.subscribe(function (e, args) {
				switch (args.DataType)
				{
					case Enum.Grid.Server:
						websocket.sendGuid(Enum.Request.RemoveServer, args.Data.Guid);
						break;

					case Enum.Grid.Channel:
						websocket.sendGuid(Enum.Request.RemoveChannel, args.Data.Guid);
						break;

					case Enum.Grid.Search:
						enableSearchTransitions = true;
						websocket.sendGuid(Enum.Request.RemoveSearch, args.Data.Guid);
						break;
				}
			});
			grid.onFlipObject.subscribe(function (e, args) {
				if (args.DataType == Enum.Grid.Packet)
				{
					flipPacket(args.Data);
				}
				else
				{
					flipObject(args.Data);
				}
			});
			grid.onDownloadLink.subscribe(function (e, args) {
				downloadLink(args);
			});
			grid.build();

			// resize
			resize.start();

			// other
			initializeDialogs();
			initializeOthers();
		}
	}
}());
