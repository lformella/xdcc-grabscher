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
		this.snapshots = this.buildDataView();
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
			case "Server":
				return this.servers;
			case "Channel":
				return this.channels;
			case "Bot":
				return this.bots;
			case "Packet":
				return this.packets;
			case "Search":
			case "Object":
			case "List`1":
				return this.searches;
			case "ExternalSearch":
				return this.externalSearch;
			case "Snapshot":
				return this.snapshots;
			case "File":
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

	filterObjects: function(item, args) {
		var result = true;

		if (args != undefined)
		{
			if (args.ParentGuid != undefined)
			{
				if (item.ParentGuid != args.ParentGuid)
				{
					result = false;
				}
			}

			if (args.Guids != undefined)
			{
				result = false;
				$.each(args.Guids, function (i, guid)
				{
					if (item.Guid == guid)
					{
						result = true;
						return false;
					}
					return true;
				});
			}

			if (args.SearchGuid != undefined)
			{
				switch (args.SearchGuid)
				{
					case "00000000-0000-0000-0000-000000000001":
						result = false; // TODO
						break;

					case "00000000-0000-0000-0000-000000000002":
						result = false; // TODO
						break;

					case "00000000-0000-0000-0000-000000000003":
						result = item.Connected;
						break;

					case "00000000-0000-0000-0000-000000000004":
						result = item.Enabled;
						break;

					default:
						var names = args.Name.split(" ");
						$.each(names, function (i, name)
						{
							if (item.Name.toLowerCase().indexOf(name) == -1)
							{
								result = false;
								return false;
							}
							return true;
						});
						break;
				}
			}
		}

		return result;
	}
});
