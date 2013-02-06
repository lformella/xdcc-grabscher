//
//  main.js
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

	function initializeDialogs ()
	{
		/* ********************************************************************************************************** */
		/* SERVER / CHANNEL DIALOG                                                                                    */
		/* ********************************************************************************************************** */

		$("#serverChannelButton")
			.button({icons: { primary: "icon-globe-1" }})
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
	function removeSearch (obj)
	{
		enableSearchTransitions = true;
		websocket.sendGuid(Enum.Request.RemoveSearch, obj.Guid);
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
					case "Object":
					case "List`1":
						if (enableSearchTransitions)
						{
							$("#searchText").effect("transfer", { to: getElementFromGrid("Search", args.Data.Guid) }, 500);
						}
						break;
				}
			});
			websocket.onRemove.subscribe(function (e, args) {
				switch (args.DataType)
				{
					case Enum.Grid.Search:
					case "Object":
					case "List`1":
						if (enableSearchTransitions)
						{
							getElementFromGrid("Search", args.Data.Guid).effect("transfer", { to: $("#searchText") }, 500);
						}
						break;
				}

				dataview.removeItem(args);
			});
			websocket.onUpdate.subscribe(function (e, args) {
				dataview.updateItem(args);
			});
			websocket.onSearches.subscribe(function (e, args) {
				dataview.setItems(args);
			});
			websocket.onSnapshots.subscribe(function (e, args) {
				statistics.setSnapshots(args.Data);
			});
			websocket.connect();

			// grid
			resize.onResize.subscribe(function (e, args) {
				grid.resize();
				statistics.resize();
			});
			grid.onClick.subscribe(function (e, args) {
				if (args.object != undefined)
				{
					switch (args.grid)
					{
						case "serverGrid":
							websocket.sendGuid(Enum.Request.ChannelsFromServer, args.object.Guid);
							break;

						case "botGrid":
							websocket.sendGuid(Enum.Request.PacketsFromBot, args.object.Guid);
							break;

						case "searchGrid":
							websocket.sendGuid(activeTab == 0 ? Enum.Request.Search : Enum.Request.SearchExternal, args.object.Guid);
							break;
					}
				}
			});
			grid.onRemoveSearch.subscribe(function (e, args) {
				removeSearch(args);
			});
			grid.onFlipObject.subscribe(function (e, args) {
				flipObject(args);
			});
			grid.onFlipPacket.subscribe(function (e, args) {
				flipPacket(args);
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
