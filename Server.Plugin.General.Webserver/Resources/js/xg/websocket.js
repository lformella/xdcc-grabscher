//
//  websocket.js
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

var XGWebsocket = Class.create(
{
	/**
	 * @param {String} url
	 * @param {String} port
	 * @param {String} password
	 * @param {XGCookie} cookie
	 */
	initialize: function(url, port, password, cookie)
	{
		this.url = url;
		this.port = port;
		this.password = password;
		this.cookie = cookie;

		this.onAdd = new Slick.Event();
		this.onRemove = new Slick.Event();
		this.onUpdate = new Slick.Event();
		this.onSearchExternal = new Slick.Event();
		this.onSearches = new Slick.Event();
		this.onSnapshots = new Slick.Event();
		this.onStatistics = new Slick.Event();
	},

	connect: function ()
	{
		var self = this;

		this.state = WebSocket.CLOSED;
		try
		{
			this.socket = new WebSocket("ws://" + this.url + ":" + this.port);
			this.socket.onopen = function ()
			{
				self.state = self.socket.readyState;

				self.onConnected();
			};
			this.socket.onmessage = function (msg)
			{
				self.onMessageReceived(JSON.parse(msg.data));
			};
			this.socket.onclose = function ()
			{
				self.state = self.socket.readyState;

				self.onDisconnected();
			};
			this.socket.onerror = function ()
			{
				self.state = self.socket.readyState;

				self.onError("");
			};
		}
		catch (exception)
		{
			if (self.socket != undefined)
			{
				self.state = self.socket.readyState;
			}

			self.onError(exception);
		}
	},

	/**
	 * @param {Enum.Request} Type
	 * @return {Object}
	 */
	buildRequest: function(Type)
	{
		return {
			"Password": this.password,
			"Type": Type,
			"IgnoreOfflineBots": this.cookie.getCookie("show_offline_bots", false) == "1"
		};
	},

	/**
	 * @param {Enum.Request} type
	 * @return {Boolean}
	 */
	send: function(type)
	{
		var request = this.buildRequest(type);

		return this.sendRequest(request);
	},

	/**
	 * @param {Enum.Request} type
	 * @param {String} name
	 * @return {Boolean}
	 */
	sendName: function(type, name)
	{
		var request = this.buildRequest(type);
		request.Name = name;

		return this.sendRequest(request);
	},

	/**
	 * @param {Enum.Request} type
	 * @param {String} guid
	 * @return {Boolean}
	 */
	sendGuid: function(type, guid)
	{
		var request = this.buildRequest(type);
		request.Guid = guid;

		return this.sendRequest(request);
	},

	/**
	 * @param {Enum.Request} type
	 * @param {String} name
	 * @param {String} guid
	 * @return {Boolean}
	 */
	sendNameGuid: function(type, name, guid)
	{
		var request = this.buildRequest(type);
		request.Name = name;
		request.Guid = guid;

		return this.sendRequest(request);
	},

	/**
	 * @param {Object} request
	 * @return {Boolean}
	 */
	sendRequest: function(request)
	{
		if (this.state == WebSocket.OPEN)
		{
			try
			{
				this.socket.send(JSON.stringify(request));
				return true;
			}
			catch (exception)
			{
				this.onError(exception);
			}
		}

		return false;
	},

	/**
	 * @param {String} grid
	 * @param {String} guid
	 * @return {object}
	 */
	getRowData: function (grid, guid)
	{
		var str = $("#" + grid).getRowData(guid).Object;
		return JSON.parse(JSON.parse(str).Object);
	},

	onConnected: function ()
	{
		this.send(Enum.Request.Searches);
		this.send(Enum.Request.Servers);
		//this.send(Enum.Request.Files);
		//this.send(Enum.Request.Snapshots);
	},

	onDisconnected: function ()
	{
		$("#dialog_error").dialog("open");
	},

	onMessageReceived: function (json)
	{
		if (json.DataType == "String")
		{
			json.DataType = json.Data;
		}

		switch (json.Type)
		{
			case Enum.Response.ObjectAdded:
				this.onAdd.notify(json, null, self);
				break;

			case Enum.Response.ObjectRemoved:
				this.onRemove.notify(json, null, self);
				break;

			case Enum.Response.ObjectChanged:
				this.onUpdate.notify(json, null, self);
				break;

			case Enum.Response.SearchExternal:
				this.onSearchExternal.notify(json.data, null, self);
				break;

			case Enum.Response.Searches:
				this.onSearches.notify(json, null, self);
				break;

			case Enum.Response.Snapshots:
				this.onSnapshots.notify(json, null, self);
				break;

			case Enum.Response.Statistics:
				this.onStatistics.notify(json, null, self);
				break;
		}
	}
});

/*
if (grid == "search")
{
	$("#search-text").effect("transfer", { to: $("#" + json.Data.Guid) }, 500);
}
if (grid == "search")
{
	$("#" + json.Data.Guid).effect("transfer", { to: $("#search-text") }, 500);
}
*/
