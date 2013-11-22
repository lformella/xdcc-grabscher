//
//  signalr-crud.js
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

	service.factory('SignalrCrud', ['$rootScope', '$injector', 'Signalr', 'HelperService',
		function ($rootScope, $injector, Signalr, HelperService)
		{
			var SignalrCrud = function(){};

			SignalrCrud.prototype.eventOnAdded = function (message)
			{
				this.$scope[this.objectsName].push(message);
				this.$scope.$apply();
			};

			SignalrCrud.prototype.eventOnRemoved = function (message)
			{
				var element = HelperService.getByGuid(this.$scope[this.objectsName], message.Guid);
				if (element != null)
				{
					this.$scope[this.objectsName].splice(element.id, 1);
					this.$scope.$apply();
				}
			};

			SignalrCrud.prototype.eventOnChanged = function (message)
			{
				var element = HelperService.getByGuid(this.$scope[this.objectsName], message.Guid);
				if (element != null)
				{
					this.$scope[this.objectsName][element.id] = element.value;
					this.$scope.$apply();
				}
			};

			SignalrCrud.prototype.initialize = function (name, $scope, objectsName, customEventCallbacks)
			{
				this.objectsName = objectsName;
				this.setScope($scope);

				var self = this;
				var eventCallbacks = [
					{
						name: 'OnAdded',
						callback:  function (message)
						{
							$.proxy(self.eventOnAdded(message), self);
						}
					},
					{
						name: 'OnRemoved',
						callback:  function (message)
						{
							$.proxy(self.eventOnRemoved(message), self);
						}
					},
					{
						name: 'OnChanged',
						callback:  function (message)
						{
							$.proxy(self.eventOnChanged(message), self);
						}
					}
				];

				if (customEventCallbacks != undefined)
				{
					eventCallbacks = eventCallbacks.concat(customEventCallbacks);
				}

				this.Signalr = new Signalr();
				this.Signalr.initialize(name, eventCallbacks);

				var SignalrInstance = this.Signalr;
				$rootScope.$on('OnConnectToSignalR', function (e, password)
				{
					SignalrInstance.connect(password);
				});
			};

			// be able to overwrite the current scope because it wil change on dialogs
			SignalrCrud.prototype.setScope = function ($scope)
			{
				this.$scope = $scope;
				this.$scope[this.objectsName] = [];
			};


			SignalrCrud.prototype.add = function (name, parentGuid)
			{
				if (parentGuid == undefined)
				{
					return this.signalrInvoke('Add', name);
				}
				else
				{
					return this.signalrInvoke('Add', parentGuid, name);
				}
			};

			SignalrCrud.prototype.remove = function (object)
			{
				object.Active = true;
				return this.signalrInvoke('Remove', object.Guid);
			};

			SignalrCrud.prototype.enable = function (object)
			{
				object.Active = true;
				return this.signalrInvoke('Enable', object.Guid);
			};

			SignalrCrud.prototype.disable = function (object)
			{
				object.Active = true;
				return this.signalrInvoke('Disable', object.Guid);
			};

			SignalrCrud.prototype.flip = function(object)
			{
				if (object.Enabled)
				{
					this.disable(object);
				}
				else
				{
					this.enable(object);
				}
			};

			SignalrCrud.prototype.signalrInvoke = function (method, arg1, arg2, arg3, arg4, arg5, arg6)
			{
				return this.Signalr.invoke(method, arg1, arg2, arg3, arg4, arg5, arg6);
			};

			return(SignalrCrud);
		}
	]);
});
