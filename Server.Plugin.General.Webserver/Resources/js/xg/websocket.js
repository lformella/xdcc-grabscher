//
//  xg.websocket.js
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
	initialize: function(cookie, url, port, password)
	{
		var self = this;

		this.cookie = cookie;

		this.onConnected = 0;
		this.onDisconnected = 0;
		this.onMessageReceived = 0;
		this.onError = 0;

		this.Password = password;

		this.state = WebSocket.CLOSED;
		try
		{
			this.socket = new WebSocket("ws://" + url + ":" + port);
			this.socket.onopen = function ()
			{
				self.state = self.socket.readyState;

				if (self.onConnected != 0)
				{
					self.onConnected();
				}
			}
			this.socket.onmessage = function (msg)
			{
				if (self.onMessageReceived != 0)
				{
					self.onMessageReceived(JSON.parse(msg.data));
				}
			}
			this.socket.onclose = function ()
			{
				self.state = self.socket.readyState;

				if (self.onDisconnected != 0)
				{
					self.onDisconnected();
				}
			}
			this.socket.onerror = function ()
			{
				self.state = self.socket.readyState;

				if (self.onError != 0)
				{
					self.onError();
				}
			}
		}
		catch (exception)
		{
			this.state = self.socket.readyState;

			if (self.onError != 0)
			{
				self.onError(exception);
			}
		}
	},

	buildRequest: function(Type)
	{
		return {
			"Password": this.Password,
			"Type": Type,
			"IgnoreOfflineBots": this.cookie.getCookie("show_offline_bots", false)
		};
	},

	send: function(type)
	{
		var request = this.buildRequest(type);

		return this.sendRequest(request);
	},

	sendName: function(type, name)
	{
		var request = this.buildRequest(type);
		request.Name = name;

		return this.sendRequest(request);
	},

	sendGuid: function(type, guid)
	{
		var request = this.buildRequest(type);
		request.Guid = guid;

		return this.sendRequest(request);
	},

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
			}
		}

		return false;
	}
});
