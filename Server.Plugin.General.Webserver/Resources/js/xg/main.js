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

var XGMain = (function ()
{
	var statistics, cookie, helper, formatter, websocket, dataView, grid, resize, gui, notification, translate;
	var currentServerGuid = "";
	var serversActive = [], botsActive = [];

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
		var elementSearch = $("#xgLogo");
		var elementPacket = getElementFromGrid(Enum.Grid.Packet, obj.Guid);
		if (!obj.Enabled)
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
		if (obj)
		{
			if (!obj.Enabled)
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
		var elementSearch = $("#xgLogo");
		var elementPacket = getElementFromGrid(Enum.Grid.ExternalSearch, obj.Guid);
		elementPacket.effect("transfer", { to: elementSearch }, 500);
		websocket.sendName(Enum.Request.ParseXdccLink, obj.IrcLink);
	}

	/**
	 * @param {String} host
	 * @param {String} port
	 * @param {String} password
	 */
	function start (host, port, password)
	{
		dataView = Object.create(XGDataView);
		dataView.initialize();

		cookie = Object.create(XGCookie);
		var humanDates = cookie.getCookie("humanDates", "0") == "1";
		var showOfflineBots = cookie.getCookie("showOfflineBots", "0") == "1";
		var combineBotAndPacketGrid = cookie.getCookie("combineBotAndPacketGrid", "0") == "1";

		helper = Object.create(XGHelper);
		helper.setHumanDates(humanDates);

		formatter = Object.create(XGFormatter);
		formatter.initialize(helper, translate);

		statistics = Object.create(XGStatistics);
		statistics.initialize(helper, translate);

		notification = Object.create(XGNotification);
		notification.initialize(dataView, translate);

		startWebsocket(host, port, password);
		startGrid(showOfflineBots, combineBotAndPacketGrid);
		startGui(showOfflineBots, humanDates, combineBotAndPacketGrid);

		resize = Object.create(XGResize);
		resize.initialize(combineBotAndPacketGrid);
		resize.onResize.subscribe(function ()
		{
			grid.resize();
			statistics.resize();
		});
		resize.start();
	}

	function startWebsocket (host, port, password)
	{
		websocket = Object.create(XGWebsocket);
		websocket.initialize(dataView, host, port, password);

		websocket.onDisconnected.subscribe(function ()
		{
			websocket.connect();
		});

		websocket.onError.subscribe(function ()
		{
			gui.showError();
		});

		websocket.onSnapshots.subscribe(function (e, args)
		{
			statistics.setSnapshots(args.Data);
		});

		websocket.onRequestComplete.subscribe(function (e, args)
		{
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
				case Enum.Request.SearchExternal:
					gui.hideLoading();
					newArgs.Data = null;
					break;
			}

			if (newArgs.Data != null)
			{
				newArgs.Data.Active = false;
				dataView.updateItem(newArgs);
			}
		});

		websocket.connect();
	}

	function startGrid (showOfflineBots, combineBotAndPacketGrid)
	{
		grid = Object.create(XGGrid);
		grid.initialize(formatter, helper, dataView, translate, combineBotAndPacketGrid);

		grid.onClick.subscribe(function (e, args)
		{
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
				}

				if (active)
				{
					args.Data.Active = true;
					dataView.updateItem(args);
				}
			}
		});

		grid.onRemoveObject.subscribe(function (e, args)
		{
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

		grid.onFlipObject.subscribe(function (e, args)
		{
			if (args.DataType == Enum.Grid.Packet)
			{
				flipPacket(args.Data);
			}
			else
			{
				flipObject(args.Data);
			}
		});

		grid.onDownloadLink.subscribe(function (e, args)
		{
			downloadLink(args);
		});

		grid.build();
		grid.setFilterOfflineBots(showOfflineBots);
	}

	function startGui (showOfflineBots, humanDates, combineBotAndPacketGrid)
	{
		gui = Object.create(XGGui);
		gui.initialize(dataView, showOfflineBots, humanDates, combineBotAndPacketGrid);

		gui.onSearch.subscribe(function (e, args)
		{
			if (args.Grid != Enum.Grid.ExternalSearch)
			{
				grid.getGrid(Enum.Grid.Bot).setSelectedRows([]);
				grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
				websocket.sendNameGuid(Enum.Request.Search, args.Name, args.Guid);
			}
			else
			{
				if (args.Guid != "00000000-0000-0000-0000-000000000001" && args.Guid != "00000000-0000-0000-0000-000000000002")
				{
					websocket.sendNameGuid(Enum.Request.SearchExternal, args.Name, args.Guid);
				}
			}
			grid.applySearchFilter(args);
			gui.showLoading();
		});

		gui.onSearchAdd.subscribe(function (e, args)
		{
			websocket.sendName(Enum.Request.AddSearch, args.Name);
		});

		gui.onSearchRemove.subscribe(function (e, args)
		{
			websocket.sendGuid(Enum.Request.RemoveSearch, args.Guid);
		});

		gui.onSlide.subscribe(function ()
		{
			grid.resize();
		});

		gui.onAddServer.subscribe(function (e, args)
		{
			websocket.sendName(Enum.Request.AddServer, args.Name);
		});

		gui.onAddChannel.subscribe(function (e, args)
		{
			websocket.sendNameGuid(Enum.Request.AddChannel, args.Name, currentServerGuid);
		});

		gui.onUpdateOfflineBotsFilter.subscribe(function (e, args)
		{
			grid.setFilterOfflineBots(args.Enable);
			cookie.setCookie("showOfflineBots", args.Enable ? "1" : "0");
		});

		gui.onUpdateHumanDates.subscribe(function (e, args)
		{
			helper.setHumanDates(args.Enable);
			grid.invalidate();
			cookie.setCookie("humanDates", args.Enable ? "1" : "0");
		});

		gui.onUpdateStatistics.subscribe(function ()
		{
			websocket.send(Enum.Request.Statistics);
		});

		gui.onUpdateSnapshotPlot.subscribe(function ()
		{
			statistics.updateSnapshotPlot();
		});

		gui.onCombineBotAndPacketGrid.subscribe(function (e, args)
		{
			grid.setCombineBotAndPacketGrid(args.Enable);
			cookie.setCookie("combineBotAndPacketGrid", args.Enable ? "1" : "0");
			resize.setCombineBotAndPacketGrid(args.Enable);
		});
	}

	return {
		/**
		 * @param {String} salt
		 * @param {String} host
		 * @param {String} port
		 */
		initialize: function (salt, host, port)
		{
			var translations = window.translations;
			if (translations == undefined)
			{
				translations = {};
			}
			translate = Object.create(XGTranslate);
			translate.initialize(translations);

			var password = Object.create(XGPassword);
			password.initialize(salt, host, port);
			password.onPasswordOk.subscribe(function (e, args)
			{
				start(host, port, args.Password);
			});
		}
	};
}());
