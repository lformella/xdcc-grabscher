//
//  websocket.js
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

var XGWebsocket = (function ()
{
	var dataView, url, port, password, state, socket;

	/**
	 * @param {Enum.Request} Type
	 * @return {Object}
	 */
	function buildRequest (Type)
	{
		return {
			"Password": password,
			"Type": Type
		};
	}

	/**
	 * @param {Object} request
	 * @return {Boolean}
	 */
	function sendRequest (request)
	{
		if (state == WebSocket.OPEN)
		{
			request = JSON.stringify(request);
			debug(request);
			try
			{
				socket.send(request);
				return true;
			}
			catch (exception)
			{
				self.onError(exception);
			}
		}

		return false;
	}

	/**
	 * @param {Object} json
	 */
	function onMessageReceived (json)
	{
		if (json.DataType == "String")
		{
			json.DataType = json.Data;
		}

		switch (json.Type)
		{
			case Enum.Response.ObjectAdded:
				debug("+ " + json.DataType + " " + json.Data.Name);
				dataView.addItem(json);
				break;

			case Enum.Response.ObjectRemoved:
				debug("- " + json.DataType + " " + json.Data.Name);
				dataView.removeItem(json);
				break;

			case Enum.Response.ObjectChanged:
				debug("~ " + json.DataType + " " + json.Data.Name);
				dataView.updateItem(json);
				break;

			case Enum.Response.Snapshots:
				debug("snapshots");
				self.onSnapshots.notify(json, null, self);
				break;

			case Enum.Response.LiveSnapshot:
				debug("livesnapshot");
				self.onLiveSnapshot.notify(json, null, self);
				break;

			case Enum.Response.SearchComplete:
				debug("search complete");
				self.onSearchComplete.notify(json, null, self);
				break;
		}
	}

	function debug (msg)
	{
		console.log("WEBSOCKET " + msg);
	}

	var self = {
		onError: new Slick.Event(),
		onConnected: new Slick.Event(),
		onDisconnected: new Slick.Event(),
		onSnapshots: new Slick.Event(),
		onSearchComplete: new Slick.Event(),
		onLiveSnapshot: new Slick.Event(),

		/**
		 * @param {XGDataView} dataView1
		 * @param {String} url1
		 * @param {String} port1
		 * @param {String} password1
		 */
		initialize: function (dataView1, url1, port1, password1)
		{
			dataView = dataView1;
			url = url1;
			port = port1;
			password = password1;
		},

		connect: function ()
		{
			var self = this;

			state = WebSocket.CLOSED;
			try
			{
				socket = new WebSocket("ws://" + url + ":" + port);
				socket.onopen = function ()
				{
					state = socket.readyState;

					self.onConnected.notify({}, null, this);
				};
				socket.onmessage = function (msg)
				{
					var data = JSON.parse(msg.data);
					data.Data.DataType = data.DataType;
					onMessageReceived(data);
				};
				socket.onclose = function ()
				{
					state = socket.readyState;

					self.onDisconnected.notify({}, null, this);
				};
				socket.onerror = function ()
				{
					state = socket.readyState;

					self.onError.notify({}, null, this);
				};
			}
			catch (exception)
			{
				if (socket != undefined)
				{
					state = socket.readyState;
				}

				self.onError.notify({exception: exception}, null, this);
			}
		},

		/**
		 * @param {Enum.Request} type
		 * @return {Boolean}
		 */
		send: function (type)
		{
			var request = buildRequest(type);

			return sendRequest(request);
		},

		/**
		 * @param {Enum.Request} type
		 * @param {String} name
		 * @return {Boolean}
		 */
		sendName: function (type, name)
		{
			var request = buildRequest(type);
			request.Name = name;

			return sendRequest(request);
		},

		/**
		 * @param {Enum.Request} type
		 * @param {String} guid
		 * @return {Boolean}
		 */
		sendGuid: function (type, guid)
		{
			var request = buildRequest(type);
			request.Guid = guid;

			return sendRequest(request);
		},

		/**
		 * @param {Enum.Request} type
		 * @param {String} name
		 * @param {String} guid
		 * @return {Boolean}
		 */
		sendNameGuid: function (type, name, guid)
		{
			var request = buildRequest(type);
			request.Name = name;
			request.Guid = guid;

			return sendRequest(request);
		}
	};
	return self;
}());
