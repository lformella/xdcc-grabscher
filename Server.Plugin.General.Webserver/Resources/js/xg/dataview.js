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
		var repository = new Slick.Data.DataView({ inlineFilters: false });
		repository.setItems([], "Guid");
		repository.setFilter(self.filterObjects);
		return repository;
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
		var repository = this.getDataView(json.DataType);
		if (repository != undefined)
		{
			repository.addItem(json.Data);
		}
	},

	removeItem: function(json)
	{
		var repository = this.getDataView(json.DataType);
		if (repository != undefined)
		{
			repository.deleteItem(json.Data);
		}
	},

	updateItem: function(json)
	{
		var repository = this.getDataView(json.DataType);
		if (repository != undefined)
		{
			repository.updateItem(json.Data);
		}
	},

	setItems: function(json)
	{
		var repository = this.getDataView(json.DataType);
		if (repository != undefined)
		{
			repository.setItems(json.Data);
		}
	},

	filterObjects: function(item, args) {
		var result = true;

		if (args != undefined)
		{
			if (args.Name != "")
			{
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
			}
			if (args.ParentGuid != "")
			{
				if (item.ParentGuid != args.ParentGuid)
				{
					result = false;
				}
			}
		}

		return result;
	}
});
