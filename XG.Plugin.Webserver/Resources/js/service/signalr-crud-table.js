//
//  signalr-crud-table.js
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

define(['./module'], function (service) {
	'use strict';

	service.factory('SignalrCrudTable', ['$rootScope', '$injector', 'SignalrCrud',
		function ($rootScope, $injector, SignalrCrud)
		{
			var SignalrCrudTable = function() {};

			SignalrCrud.prototype.reloadTable = function ()
			{
				this.$scope[this.tableParamsName].reload();
			};

			SignalrCrudTable.prototype = Object.create(SignalrCrud.prototype);

			SignalrCrudTable.prototype.parentInitialize = SignalrCrudTable.prototype.initialize;
			SignalrCrudTable.prototype.initialize = function (name, $scope, objectsName, customEventCallbacks, tableParamsName)
			{
				if (tableParamsName == undefined)
				{
					tableParamsName = "tableParams";
				}
				this.tableParamsName = tableParamsName;

				var self = this;
				var eventCallbacks = [
					{
						name: 'OnAdded',
						callback:  function ()
						{
							$.proxy(self.reloadTable(), self);
						}
					},
					{
						name: 'OnRemoved',
						callback:  function ()
						{
							$.proxy(self.reloadTable(), self);
						}
					},
					{
						name: 'OnChanged',
						callback:  function ()
						{
							$.proxy(self.reloadTable(), self);
						}
					},
					{
						name: 'OnReloadTable',
						callback:  function ()
						{
							$.proxy(self.reloadTable(), self);
						}
					}
				];

				if (customEventCallbacks != undefined)
				{
					eventCallbacks = eventCallbacks.concat(customEventCallbacks);
				}

				this.parentInitialize(name, $scope, objectsName, eventCallbacks);
			};

			SignalrCrudTable.prototype.loadData = function ($defer, params, method, methodParam)
			{
				if (method == undefined)
				{
					method = 'Load';
				}

				var sortBy = '';
				var sort = '';
				var keys = Object.keys(params.$params.sorting);
				if (keys.length > 0)
				{
					sortBy = keys[0];
					sort = params.$params.sorting[sortBy];
				}

				var $scope = this.$scope;
				var signalR = null;
				if (methodParam == undefined)
				{
					signalR = this.signalrInvoke(method, params.$params.count, params.$params.page, sortBy, sort);
				}
				else
				{
					signalR = this.signalrInvoke(method, methodParam, params.$params.count, params.$params.page, sortBy, sort);
				}
				if (signalR != null)
				{
					signalR.done(
						function (data)
						{
							params.total(data.Total);
							$scope.objects = data.Results;
							$defer.resolve($scope.objects);
							$scope.$apply();
						}
					);
				}
			};

			return(SignalrCrudTable);
		}
	]);
});
