// 
//  helper.js
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

var XGDataView = Class.create(
{
	initialize: function()
	{
		this.servers = this.buildDataView();
		this.channels = this.buildDataView();
		this.bots = this.buildDataView();
		this.packets = this.buildDataView();
		this.externalSearch = this.buildDataView();
		this.searches = this.buildDataView();
		this.files = this.buildDataView();
	},

	/**
	 * @return {Slick.Data.DataView}
	 */
	buildDataView: function()
	{
		var self = this;
		var dataView = new Slick.Data.DataView({ inlineFilters: false });
		dataView.setItems([], "Guid");
		dataView.setFilter(self.filterObjects);
		return dataView;
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
				return this.servers;
			case Enum.Grid.Channel:
				return this.channels;
			case Enum.Grid.Bot:
				return this.bots;
			case Enum.Grid.Packet:
				return this.packets;
			case Enum.Grid.Search:
			case "Object":
			case "List`1":
				return this.searches;
			case Enum.Grid.ExternalSearch:
				return this.externalSearch;
			case Enum.Grid.File:
				return this.files;
		}

		return undefined;
	},

	addItem: function(json)
	{
		var dataView = this.getDataView(json.DataType);
		if (dataView != undefined)
		{
			dataView.addItem(json.Data);
		}
	},

	removeItem: function(json)
	{
		var dataView = this.getDataView(json.DataType);
		if (dataView != undefined)
		{
			dataView.deleteItem(json.Data.Guid);
		}
	},

	updateItem: function(json)
	{
		var dataView = this.getDataView(json.DataType);
		if (dataView != undefined)
		{
			dataView.updateItem(json.Data.Guid, json.Data);
		}
	},

	setItems: function(json)
	{
		var dataView = this.getDataView(json.DataType);
		if (dataView != undefined)
		{
			dataView.setItems(json.Data);
		}
	},

	filterObjects: function(item, args)
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
				if (args.Guids != undefined)
				{
					var currentResult = false;
					$.each(args.Guids, function (i, guid)
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
			}

			if (item.DataType == Enum.Grid.Packet)
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
				}
			}
		}

		return result;
	}
});
