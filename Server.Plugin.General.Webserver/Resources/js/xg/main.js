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
	var statistics, cookie, helper, formatter, websocket, dataView, grid, resize, gui;
	var activeTab = 0;
	var currentServerGuid = "";
	var serversActive = [], botsActive = [], searchesActive = [], externalSearchesActive = [];

	function initializeDialogs ()
	{
		/* ********************************************************************************************************** */
		/* SERVER / CHANNEL DIALOG                                                                                    */
		/* ********************************************************************************************************** */

		$("#serverChannelButton").click( function()
			{
				$("#serverChannelsDialog").modal('show');
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

		$("#statisticsButton").click( function()
			{
				websocket.send(Enum.Request.Statistics);
				$("#dialogStatistics").modal('show');
			});

		/* ********************************************************************************************************** */
		/* SNAPSHOTS DIALOG                                                                                           */
		/* ********************************************************************************************************** */

		//$(".snapshotCheckbox").button();
		$(".snapshotCheckbox, input[name='snapshotTime']").click( function()
		{
			statistics.updateSnapshotPlot();
		});

		$("#snapshotsButton").click( function()
			{
				$("#dialogSnapshots").modal('show');
			});
	}

	function initializeOthers ()
	{
		var element;

		$("#tabs").tabs({
			select: function(event, ui)
			{
				activeTab = ui.index;
			}
		});
		$('#tabs').bind('tabsshow', function(event, ui) {
			grid.resize();
		});

		element = $("#showOfflineBots");
		var showOfflineBots = cookie.getCookie("showOfflineBots", "0") == "1";
		element.click( function()
			{
				showOfflineBots = !showOfflineBots;
				cookie.setCookie("showOfflineBots", showOfflineBots ? "1" : "0");
				grid.setFilterOfflineBots(showOfflineBots);
			});
		if (showOfflineBots)
		{
			element.button("toggle");
		}
		grid.setFilterOfflineBots(showOfflineBots);

		element = $("#humanDates");
		var humanDates = cookie.getCookie("humanDates", "0") == "1";
		element.click( function()
			{
				humanDates = !humanDates;
				cookie.setCookie("humanDates", humanDates ? "1" : "0");
				helper.setHumanDates(humanDates);
				grid.invalidate();
			});
		if (humanDates)
		{
			element.button("toggle");
		}
		helper.setHumanDates(humanDates);
	}

	/**
	 * @param {String} gridName
	 * @param {String} guid
	 * @return {jQuery}
	 */
	function getElementFromGrid (gridName, guid)
	{
		var row = dataView.getDataView(gridName).getRowById(guid);
		return $(grid.getGrid(gridName).getCellNode(row, 1)).parent();
	}

	/**
	 * @param {Object} obj
	 */
	function flipPacket (obj)
	{
		var elementSearch = $("#00000000-0000-0000-0000-000000000002");
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
		var elementSearch = $("#00000000-0000-0000-0000-000000000002");
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
		 * @param {XGDataView} dataView1
		 * @param {XGGrid} grid1
		 * @param {XGResize} resize1
		 * @param {XGGui} gui1
		 */
		initialize: function(helper1, statistics1, cookie1, formatter1, websocket1, dataView1, grid1, resize1, gui1)
		{
			statistics = statistics1;
			cookie = cookie1;
			helper = helper1;
			formatter = formatter1;
			websocket = websocket1;
			dataView = dataView1;
			grid = grid1;
			resize = resize1;
			gui = gui1;
		},

		start: function()
		{
			// socket
			websocket.onDisconnected.subscribe(function (e, args) {
				//$("#dialogError").dialog("open");
				websocket.connect();
			});
			websocket.onError.subscribe(function (e, args) {
				$("#dialogError").modal('show');
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
						newArgs.Data = dataView.getDataView(newArgs.DataType).getItemById(serversActive.shift());
						break;

					case Enum.Request.PacketsFromBot:
						newArgs.DataType = Enum.Grid.Bot;
						newArgs.Data = dataView.getDataView(newArgs.DataType).getItemById(botsActive.shift());
						break;

					case Enum.Request.Search:
						newArgs.Data = dataView.getDataView(newArgs.DataType).getItemById(searchesActive.shift());
						break;

					case Enum.Request.SearchExternal:
						newArgs.Data = dataView.getDataView(newArgs.DataType).getItemById(externalSearchesActive.shift());
						break;
				}

				if (newArgs.Data != null)
				{
					newArgs.Data.Active = false;
					dataView.updateItem(newArgs);
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
					var active = false;
					switch (args.DataType)
					{
						case Enum.Grid.Server:
							websocket.sendGuid(Enum.Request.ChannelsFromServer, args.Data.Guid);
							grid.getGrid(Enum.Grid.Channel).setSelectedRows([]);
							currentServerGuid = args.Data.Guid;
							$("#channelPanel").show();
							serversActive.push(args.Data.Guid);
							active = true;
							break;

						case Enum.Grid.Bot:
							websocket.sendGuid(Enum.Request.PacketsFromBot, args.Data.Guid);
							grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
							botsActive.push(args.Data.Guid);
							active = true;
							break;

						case Enum.Grid.Packet:
							var row = dataView.getDataView(Enum.Grid.Bot).getRowById(args.Data.ParentGuid);
							grid.getGrid(Enum.Grid.Bot).scrollRowIntoView(row, false);
							grid.getGrid(Enum.Grid.Bot).setSelectedRows([row]);
							active = true;
							break;

						case Enum.Grid.Search:
							if (activeTab == 0)
							{
								grid.getGrid(Enum.Grid.Bot).setSelectedRows([]);
								grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
								websocket.sendGuid(Enum.Request.Search, args.Data.Guid);
								searchesActive.push(args.Data.Guid);
								active = true;
							}
							else if (activeTab == 1)
							{
								if (args.Data.Guid != "00000000-0000-0000-0000-000000000001" && args.Data.Guid != "00000000-0000-0000-0000-000000000002")
								{
									websocket.sendGuid(Enum.Request.SearchExternal, args.Data.Guid);
									externalSearchesActive.push(args.Data.Guid);
									active = true;
								}
							}
							break;
					}

					if (active)
					{
						args.Data.Active = true;
						dataView.updateItem(args);
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

            // gui
            gui.onSearch.subscribe(function (e, args) {
                if (args.Grid != Enum.Grid.ExternalSearch)
                {
                    grid.getGrid(Enum.Grid.Bot).setSelectedRows([]);
                    grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
                    websocket.sendGuid(Enum.Request.Search, args.Guid);
                    searchesActive.push(args.Guid);
                    active = true;
                }
                else if (activeTab == 1)
                {
                    if (args.Guid != "00000000-0000-0000-0000-000000000001" && args.Guid != "00000000-0000-0000-0000-000000000002")
                    {
                        websocket.sendGuid(Enum.Request.SearchExternal, args.Guid);
                        externalSearchesActive.push(args.Guid);
                        active = true;
                    }
                }
				grid.applySearchFilter(args);
            });
            gui.onSearchAdd.subscribe(function (e, args) {
                websocket.sendName(Enum.Request.AddSearch, args.Name);
            });
            gui.onSearchRemove.subscribe(function (e, args) {
                websocket.sendGuid(Enum.Request.RemoveSearch, args.Guid);
            });
            gui.onSlide.subscribe(function (e, args) {
				grid.resize();
            });

			// other
			initializeDialogs();
			initializeOthers();

			// resize
			resize.start();
		}
	}
}());
