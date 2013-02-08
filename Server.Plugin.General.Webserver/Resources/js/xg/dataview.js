// 
//  dataview.js
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

var XGDataView = (function()
{
	var servers, channels, bots, packets, externalSearch, searches, files;
	var botFilterGuids = [];

	/**
	 * @return {Slick.Data.DataView}
	 */
	function buildDataView()
	{
		var dataView = new Slick.Data.DataView({ inlineFilters: false });
		dataView.setItems([], "Guid");
		dataView.setFilter(filterObjects);
		return dataView;
	}

	function filterObjects (item, args)
	{
		var result = true;

		if (args != undefined)
		{
			if (args.OfflineBots != undefined && !args.OfflineBots)
			{
				switch (item.DataType)
				{
					case Enum.Grid.Bot:
						//result = result && item.Connected;
						break;
					case Enum.Grid.Packet:
						//var parentItem = this.getDataView(Enum.Grid.Bot).getDataItem(item.ParentGuid);
						//if (parentItem != undefined)
						{
							//result = result && parentItem.Connected;
						}
						break;
				}
			}

			if (args.ParentGuid != undefined)
			{
				if (item.ParentGuid != args.ParentGuid)
				{
					result = result && false;
				}
			}

			if (item.DataType == Enum.Grid.Bot)
			{
				var currentResult = false;
				$.each(botFilterGuids, function (i, guid)
				{
					if (item.Guid == guid)
					{
						currentResult = true;
						return false;
					}
					return true;
				});
				result = result && currentResult;
			}

			if (item.DataType == Enum.Grid.Packet || item.DataType == Enum.Grid.ExternalSearch)
			{
				if (args.SearchGuid != undefined)
				{
					switch (args.SearchGuid)
					{
						case "00000000-0000-0000-0000-000000000001":
							result = result && false; // TODO
							break;

						case "00000000-0000-0000-0000-000000000002":
							result = result && false; // TODO
							break;

						case "00000000-0000-0000-0000-000000000003":
							result = result && item.Connected;
							break;

						case "00000000-0000-0000-0000-000000000004":
							result = result && item.Enabled;
							break;

						default:
							var names = args.Name.split(" ");
							$.each(names, function (i, name)
							{
								if (item.Name.toLowerCase().indexOf(name) == -1)
								{
									result = result && false;
									return false;
								}
								return true;
							});
							break;
					}

					if (item.DataType == Enum.Grid.Packet && result && botFilterGuids.indexOf(item.ParentGuid) == -1)
					{
						botFilterGuids.push(item.ParentGuid);
					}
				}
			}
		}

		return result;
	}

	return {
		initialize: function()
		{
			servers = buildDataView();
			channels = buildDataView();
			bots = buildDataView();
			packets = buildDataView();
			externalSearch = buildDataView();
			searches = buildDataView();
			files = buildDataView();
		},

		/**
		 * @param {string} name
		 * @return {Slick.Data.DataView}
		 */
		getDataView: function(name)
		{
			switch (name)
			{
				case Enum.Grid.Server:
					return servers;
				case Enum.Grid.Channel:
					return channels;
				case Enum.Grid.Bot:
					return bots;
				case Enum.Grid.Packet:
					return packets;
				case Enum.Grid.Search:
				case "Object":
				case "List`1":
					return searches;
				case Enum.Grid.ExternalSearch:
					return externalSearch;
				case Enum.Grid.File:
					return files;
			}

			return null;
		},

		addItem: function(json)
		{
			var dataView = this.getDataView(json.DataType);
			if (dataView != null)
			{
				dataView.addItem(json.Data);
			}
		},

		removeItem: function(json)
		{
			var dataView = this.getDataView(json.DataType);
			if (dataView != null)
			{
				dataView.deleteItem(json.Data.Guid);
			}
		},

		updateItem: function(json)
		{
			var dataView = this.getDataView(json.DataType);
			if (dataView != null)
			{
				dataView.updateItem(json.Data.Guid, json.Data);
			}
		},

		setItems: function(json)
		{
			var dataView = this.getDataView(json.DataType);
			if (dataView != null)
			{
				dataView.setItems(json.Data);
			}
		},

		resetBotFilter: function ()
		{
			botFilterGuids = [];
		}
	};
}());
