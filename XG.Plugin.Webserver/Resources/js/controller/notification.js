//
//  notification.js
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

define(['./module', 'favico'], function (ng, Favico) {
	'use strict';

	ng.controller('NotificationCtrl', ['$rootScope', '$scope', 'SignalrTableFactory', '$filter', 'ngTableParams',
		function ($rootScope, $scope, SignalrTableFactory, $filter, ngTableParams)
		{
			var eventCallbacks = [
				{
					name: 'OnConnected',
					callback: function ()
					{
						$scope.tableParams.reload();
					}
				},
				{
					name: 'OnAdded',
					callback: function (message)
					{
						$scope.counter++;

						if ("Notification" in window && Notification.permission === "granted")
						{
							var options = {
								body: $filter('formatNotificationDescription')(message),
								tag: message.Guid,
								icon: "Resources/images/chat.ico"
							};
							new Notification($filter('formatNotificationHeader')(message), options);
						}
					}
				}
			];
			$scope.signalr = new SignalrTableFactory();
			$scope.signalr.initialize('notificationHub', $scope, 'objects', eventCallbacks);

			$scope.tableParams = new ngTableParams({
				page: 1,
				count: 20,
				sorting: {
					Time: 'desc'
				}
			}, {
				counts: [],
				total: 0,
				getData: function($defer, params)
				{
					if (!$scope.signalr.isConnected())
					{
						return;
					}

					$scope.signalr.loadData($defer, params);
				}
			});

			$scope.refresh = function ()
			{
				$scope.tableParams.reload();
			};

			$scope.counter = 0;

			var favicon = new Favico({ animation:'none' });
			$scope.$watch('counter', function (counter)
			{
				favicon.badge(counter);
			});

			// request desktop notification permission
			if ("Notification" in window && Notification.permission !== "granted" && Notification.permission !== 'denied')
			{
				Notification.requestPermission(function (permission)
				{
					if (!('permission' in Notification))
					{
						Notification.permission = permission;
					}
				});
			}
		}
	]);
});
