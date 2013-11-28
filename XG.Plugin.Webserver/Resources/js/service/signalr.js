//
//  signalr.js
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

define(['./module', 'signalr.hubs'], function (service) {
	'use strict';

	service.factory('Signalr', ['$rootScope',
		function ($rootScope)
		{
			var Signalr = function (){};

			Signalr.prototype.initialize = function(name, eventCallbacks)
			{
				this.name = name;
				this.eventCallbacks = eventCallbacks;

				this.proxy = $.connection[this.name];
				this.connected = false;

				var connectedCallback = null;
				angular.forEach(this.eventCallbacks, function (value)
				{
					if (value.name == "OnConnected")
					{
						connectedCallback = value.callback;
					}
					else
					{
						this.proxy.on(value.name, function (message)
						{
							value.callback(message);
						});
					}
				}, this);

				var self = this;
				$rootScope.$on('OnConnectedToSignalR', function (e, password)
				{
					self.connected = true;
					if (connectedCallback != null)
					{
						connectedCallback();
					}
				});
			};

			Signalr.prototype.invoke = function (method, arg1, arg2, arg3, arg4, arg5, arg6)
			{
				if (!this.connected)
				{
					return null;
				}

				if (arg6 != undefined && arg6 != null)
				{
					return this.proxy.invoke(method, arg1, arg2, arg3, arg4, arg5, arg6);
				}
				else if (arg5 != undefined && arg5 != null)
				{
					return this.proxy.invoke(method, arg1, arg2, arg3, arg4, arg5);
				}
				else if (arg4 != undefined && arg4 != null)
				{
					return this.proxy.invoke(method, arg1, arg2, arg3, arg4);
				}
				else if (arg3 != undefined && arg3 != null)
				{
					return this.proxy.invoke(method, arg1, arg2, arg3);
				}
				else if (arg2 != undefined && arg2 != null)
				{
					return this.proxy.invoke(method, arg1, arg2);
				}
				else if (arg1 != undefined && arg1 != null)
				{
					return this.proxy.invoke(method, arg1);
				}
				else
				{
					return this.proxy.invoke(method);
				}
			};

			return(Signalr);
		}
	]);
});
