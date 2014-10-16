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

define(['./module'], function (ng) {
	'use strict';

	ng.factory('SignalrFactory', ['$rootScope', '$injector', 'SignalrService', 'HelperService',
		function ($rootScope, $injector, SignalrService, HelperService)
		{
			var SignalrFactory = function(){};

			SignalrFactory.prototype.initialize = function (name, $scope, objectsName, customEventCallbacks)
			{
				this.objectsName = objectsName;
				this.setScope($scope);

				var self = this;
				var eventCallbacks = [
					{
						name: 'OnAdded',
						callback:  function (message)
						{
							self.$scope[self.objectsName].push(message);
							self.$scope.$apply();
						}
					},
					{
						name: 'OnRemoved',
						callback:  function (message)
						{
							var element = HelperService.getByGuid(self.$scope[self.objectsName], message.Guid);
							if (element != null)
							{
								self.$scope[self.objectsName].splice(element.id, 1);
								self.$scope.$apply();
							}
						}
					},
					{
						name: 'OnChanged',
						callback:  function (message)
						{
							var element = HelperService.getByGuid(self.$scope[self.objectsName], message.Guid);
							if (element != null)
							{
								var groupBy = self.$scope[self.objectsName][element.id].GroupBy;
								self.$scope[self.objectsName][element.id] = message;
								self.$scope[self.objectsName][element.id].GroupBy = groupBy;
								self.$scope.$apply();
							}
						}
					}
				];
				SignalrService.attachEventCallbacks(name, eventCallbacks);

				if (customEventCallbacks != undefined)
				{
					SignalrService.attachEventCallbacks(name, customEventCallbacks);
				}

				this.proxy = SignalrService.getProxy(name);
			};

			SignalrFactory.prototype.getProxy = function()
			{
				return this.proxy;
			};

			SignalrFactory.prototype.isConnected = function()
			{
				return SignalrService.isConnected();
			};

			// be able to overwrite the current scope because it wil change on dialogs
			SignalrFactory.prototype.setScope = function ($scope)
			{
				this.$scope = $scope;
				this.$scope[this.objectsName] = [];
			};

			SignalrFactory.prototype.add = function (name, parentGuid)
			{
				if (!SignalrService.isConnected())
				{
					return;
				}

				try
				{
					if (parentGuid == undefined)
					{
						this.proxy.server.add(name);
					}
					else
					{
						this.proxy.server.add(parentGuid, name);
					}
				}
				catch (e)
				{
					var message = { source: { status: 404 }};
					$rootScope.$emit('AnErrorOccurred', message);
				}
			};

			SignalrFactory.prototype.remove = function (object)
			{
				if (!SignalrService.isConnected())
				{
					return;
				}

				object.Active = true;
				try
				{
					this.proxy.server.remove(object.Guid);
					object.Active = false;
					object.Waiting = true;
				}
				catch (e)
				{
					object.Active = false;
					var message = { source: { status: 404 }};
					$rootScope.$emit('AnErrorOccurred', message);
				}
			};

			SignalrFactory.prototype.enable = function (object)
			{
				if (!SignalrService.isConnected())
				{
					return;
				}

				object.Active = true;
				try
				{
					this.proxy.server.enable(object.Guid);
					object.Active = false;
					object.Waiting = true;
				}
				catch (e)
				{
					object.Active = false;
					var message = { source: { status: 404 }};
					$rootScope.$emit('AnErrorOccurred', message);
				}
			};

			SignalrFactory.prototype.disable = function (object)
			{
				if (!SignalrService.isConnected())
				{
					return;
				}

				object.Active = true;
				try
				{
					this.proxy.server.disable(object.Guid);
					object.Active = false;
					object.Waiting = true;
				}
				catch (e)
				{
					object.Active = false;
					var message = { source: { status: 404 }};
					$rootScope.$emit('AnErrorOccurred', message);
				}
			};

			SignalrFactory.prototype.flip = function(object)
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

			SignalrFactory.prototype.visible = function (bool)
			{
				this.proxy.server[bool ? "visible" : "inVisible"]();
			};

			return(SignalrFactory);
		}
	]);
});
