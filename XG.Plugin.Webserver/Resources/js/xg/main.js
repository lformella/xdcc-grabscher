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

define(['xg/config', 'xg/cookie', 'xg/dataview', 'xg/formatter', 'xg/graph', 'xg/grid', 'xg/gui', 'xg/helper', 'xg/password', 'xg/resize', 'xg/translate', 'xg/websocket'],
	function(config, cookie, dataView, formatter, graph, grid, gui, helper, password, resize, translate, websocket)
{
	var currentServerGuid = "", lastSearch, currentTab = 0;

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
	function flipFile (obj)
	{
		obj.Active = true;
		dataView.updateItem({ Data: obj, DataType: Enum.Grid.File });
		websocket.sendGuid(Enum.Request.DeactivateObject, obj.Guid);
	}

	/**
	 * @param {Object} obj
	 */
	function flipPacket (obj)
	{
		obj.Active = true;
		dataView.updateItem({ Data: obj, DataType: Enum.Grid.Packet });
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
		obj.Active = true;
		dataView.updateItem({ Data: obj, DataType: Enum.Grid.ExternalSearch });
		websocket.sendName(Enum.Request.ParseXdccLink, obj.IrcLink);
		setTimeout(function ()
		{
			obj.Active = false;
			dataView.updateItem({ Data: obj, DataType: Enum.Grid.ExternalSearch });
		}, 1000);
	}

	function start ()
	{
		config.onUpdateCombineBotAndPacketGrid.subscribe(function (e, args)
		{
			cookie.setCookie("combineBotAndPacketGrid", args.Enable ? "1" : "0");
		});
		config.onUpdateHumanDates.subscribe(function (e, args)
		{
			cookie.setCookie("humanDates", args.Enable ? "1" : "0");
		});
		config.onUpdateShowOfflineBots.subscribe(function (e, args)
		{
			cookie.setCookie("showOfflineBots", args.Enable ? "1" : "0");
		});

		config.setHumanDates(cookie.getCookie("humanDates", "0") == "1");
		config.setShowOfflineBots(cookie.getCookie("showOfflineBots", "0") == "1");
		config.setCombineBotAndPacketGrid(cookie.getCookie("combineBotAndPacketGrid", "0") == "1");

		dataView.initialize();

		graph.initialize();

		startWebsocket();
		startGrid();
		startGui();

		resize.onResize.subscribe(function ()
		{
			grid.resize();
			graph.resize();
		});
		resize.initialize();

		loop();
	}

	function startWebsocket ()
	{
		websocket.onConnected.subscribe(function ()
		{
			websocket.send(Enum.Request.LiveSnapshot);
			websocket.send(Enum.Request.Searches);
			websocket.send(Enum.Request.Servers);
			websocket.send(Enum.Request.Files);
			websocket.sendName(Enum.Request.Snapshots, -1);
		});

		websocket.onDisconnected.subscribe(function ()
		{
			gui.showError();
		});

		websocket.onError.subscribe(function ()
		{
			gui.showError();
		});

		websocket.onSnapshots.subscribe(function (e, args)
		{
			graph.setSnapshots(args.Data);
		});

		websocket.onLiveSnapshot.subscribe(function (e, args)
		{
			graph.setLiveSnapshot(args.Data);
		});

		websocket.onSearchComplete.subscribe(function (e, args)
		{
			if (args.Data == Enum.Request.Search)
			{
				dataView.endUpdate(Enum.Grid.Packet);
				dataView.endUpdate(Enum.Grid.Bot);
			}
			else
			{
				dataView.endUpdate(Enum.Grid.ExternalSearch);
			}

			grid.applySearchFilter(lastSearch, args.Data == Enum.Request.Search ? Enum.Grid.Bot : Enum.Grid.ExternalSearch);
			gui.hideLoading();
		});

		websocket.initialize();
	}

	function startGrid ()
	{
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
						active = true;
						break;

					case Enum.Grid.Bot:
						websocket.sendGuid(Enum.Request.PacketsFromBot, args.Data.Guid);
						grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
						active = true;
						break;

					case Enum.Grid.Packet:
						var row = dataView.getDataView(Enum.Grid.Bot).getRowById(args.Data.ParentGuid);
						grid.getGrid(Enum.Grid.Bot).scrollRowIntoView(row, false);
						grid.getGrid(Enum.Grid.Bot).setSelectedRows([row]);
						break;
				}

				if (active)
				{
					args.Data.Active = true;
					dataView.updateItem(args);
				}
			}
		});

		grid.onDblClick.subscribe(function (e, args)
		{
			if (args.Data != undefined)
			{
				switch (args.DataType)
				{
					case Enum.Grid.Packet:
						gui.showInfo(args.Data.Name);
						break;
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
			args.Data.Active = true;
			dataView.updateItem(args);
		});

		grid.onFlipObject.subscribe(function (e, args)
		{
			switch (args.DataType)
			{
				case Enum.Grid.Packet:
					flipPacket(args.Data);
					break;

				case Enum.Grid.File:
					flipFile(args.Data);
					break;

				default:
					flipObject(args.Data);
					break;
			}
		});

		grid.onDownloadLink.subscribe(function (e, args)
		{
			downloadLink(args);
		});

		grid.initialize();
	}

	function startGui ()
	{
		gui.initialize();

		gui.onSearch.subscribe(function (e, args)
		{
			if (args.Grid != Enum.Grid.ExternalSearch)
			{
				dataView.beginUpdate(Enum.Grid.Bot);
				dataView.beginUpdate(Enum.Grid.Packet);

				grid.getGrid(Enum.Grid.Bot).setSelectedRows([]);
				grid.getGrid(Enum.Grid.Packet).setSelectedRows([]);
				websocket.sendNameGuid(Enum.Request.Search, args.Name, args.Guid);
			}
			else
			{
				if (args.Guid != "00000000-0000-0000-0000-000000000001" && args.Guid != "00000000-0000-0000-0000-000000000002")
				{
					dataView.beginUpdate(Enum.Grid.ExternalSearch);

					websocket.sendNameGuid(Enum.Request.SearchExternal, args.Name, args.Guid);
				}
			}

			lastSearch = args;
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

		gui.onSlide.subscribe(function (e, args)
		{
			currentTab = args.slide;
			switch (currentTab)
			{
				case 0:
					websocket.send(Enum.Request.LiveSnapshot);
					break;

				case 1:
				case 2:
				case 3:
					grid.resize();
					break;
			}
		});

		gui.onAddServer.subscribe(function (e, args)
		{
			websocket.sendName(Enum.Request.AddServer, args.Name);
		});

		gui.onAddChannel.subscribe(function (e, args)
		{
			websocket.sendNameGuid(Enum.Request.AddChannel, args.Name, currentServerGuid);
		});

		gui.onRequestSnapshotPlot.subscribe(function (e, args)
		{
			websocket.sendName(Enum.Request.Snapshots, args.Value);
		});

		gui.onUpdateSnapshotPlot.subscribe(function ()
		{
			graph.updateSnapshotPlot();
		});

		gui.onAddXdccLink.subscribe(function (e, args)
		{
			websocket.sendName(Enum.Request.ParseXdccLink, args.Name);
		});

		gui.onOpenNotifications.subscribe(function (e, args)
		{
			grid.invalidate(Enum.Grid.Notification);
		});
	}

	function loop ()
	{
		if (currentTab == 0)
		{
			websocket.send(Enum.Request.LiveSnapshot);
		}

		setTimeout(function ()
		{
			loop();
		}, 10000);
	}

	var self = {
		initialize: function ()
		{
			var translations = window.translations;
			if (translations == undefined)
			{
				translations = {};
			}
			translate.initialize(translations);

			password.initialize();
			password.onPasswordOk.subscribe(function ()
			{
				start();
			});
		}
	};

	return self;
});
