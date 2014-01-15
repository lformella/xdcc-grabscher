//
//  signalr-table.js
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

define(['./module'], function (ng) {
	'use strict';

	ng.service('SignalrTableFactory', ['$rootScope', '$injector', 'SignalrFactory',
		function ($rootScope, $injector, SignalrFactory)
		{
			var SignalrTableFactory = function() {};
			SignalrTableFactory.prototype = Object.create(SignalrFactory.prototype);

			SignalrTableFactory.prototype.parentInitialize = SignalrTableFactory.prototype.initialize;
			SignalrTableFactory.prototype.initialize = function (name, $scope, objectsName, customEventCallbacks, tableParamsName)
			{
				if (tableParamsName == undefined)
				{
					tableParamsName = "tableParams";
				}
				this.tableParamsName = tableParamsName;
				this.reloadOffline = false;

				var self = this;
				var eventCallbacks = [
					{
						name: 'OnAdded',
						callback:  function ()
						{
							self.reloadOffline = true;
							self.$scope[self.tableParamsName].reload();
						}
					},
					{
						name: 'OnRemoved',
						callback:  function ()
						{
							self.reloadOffline = true;
							self.$scope[self.tableParamsName].reload();
						}
					},
					{
						name: 'OnChanged',
						callback:  function ()
						{
							self.reloadOffline = true;
							self.$scope[self.tableParamsName].reload();
						}
					},
					{
						name: 'OnReloadTable',
						callback:  function ()
						{
							self.$scope[self.tableParamsName].reload();
						}
					}
				];

				if (customEventCallbacks != undefined)
				{
					eventCallbacks = eventCallbacks.concat(customEventCallbacks);
				}

				this.parentInitialize(name, $scope, objectsName, eventCallbacks);
			};

			SignalrTableFactory.prototype.loadData = function ($defer, params, method, methodParam)
			{
				if (this.reloadOffline)
				{
					this.reloadOffline = false;
					$defer.resolve(this.$scope[this.objectsName]);
					this.$scope.$apply();
					return;
				}

				if (!this.isConnected())
				{
					return;
				}

				if (method == undefined)
				{
					method = 'load';
				}

				var sortBy = '';
				var sort = '';
				var keys = Object.keys(params.$params.sorting);
				if (keys.length > 0)
				{
					sortBy = keys[0];
					sort = params.$params.sorting[sortBy];
				}

				var self = this;
				var signalr = null;
				if (methodParam == undefined)
				{
					signalr = this.proxy.server[method](params.$params.count, params.$params.page, sortBy, sort);
				}
				else
				{
					signalr = this.proxy.server[method](methodParam, params.$params.count, params.$params.page, sortBy, sort);
				}
				if (signalr != null)
				{
					signalr.done(
						function (data)
						{
							params.total(data.Total);
							self.$scope[self.objectsName] = data.Results;
							$defer.resolve(self.$scope[self.objectsName]);
							self.$scope.$apply();
						}
					);
				}
			};

			return(SignalrTableFactory);
		}
	]);
});
