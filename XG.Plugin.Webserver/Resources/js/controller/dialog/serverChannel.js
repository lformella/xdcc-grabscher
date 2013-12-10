//
//  serverChannel.js
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

	ng.controller('ServerChannelDialogCtrl', ['$scope', '$modalInstance', 'serverSignalr', 'channelSignalr', 'ngTableParams',
		function ($scope, $modalInstance, serverSignalr, channelSignalr, ngTableParams)
		{
			$scope.serverSignalr = serverSignalr;
			$scope.serverSignalr.setScope($scope);
			$scope.serverSignalr.server = null;
			$scope.tableParamsServer = new ngTableParams({
				page: 1,
				count: 10
			}, {
				counts: [],
				total: 0,
				getData: function($defer, params)
				{
					$scope.serverSignalr.loadData($defer, params);
				}
			});

			$scope.server = '';
			$scope.addServer = function()
			{
				if ($scope.server != '')
				{
					$scope.serverSignalr.add($scope.server);
					$scope.server = '';
				}
			};

			$scope.serverKeydown = function($event)
			{
				if ($event.keyCode == 13)
				{
					$scope.addServer();
				}
			};

			$scope.channelSignalr = channelSignalr;
			$scope.channelSignalr.setScope($scope);
			$scope.tableParamsChannel = new ngTableParams({
				page: 1,
				count: 10
			}, {
				counts: [],
				total: 0,
				getData: function($defer, params)
				{
					if ($scope.serverSignalr.server != null)
					{
						$scope.channelSignalr.loadData($defer, params, 'loadByServer', $scope.serverSignalr.server.Guid);
					}
					else
					{
						$defer.resolve({});
					}
				}
			});

			$scope.channel = '';
			$scope.addChannel = function()
			{
				if ($scope.channel != '')
				{
					$scope.channelSignalr.add($scope.channel, $scope.serverSignalr.server.Guid);
					$scope.channel = '';
				}
			};

			$scope.channelKeydown = function($event)
			{
				if ($event.keyCode == 13)
				{
					$scope.addChannel();
				}
			};

			$scope.$watch('serverSignalr.server', function () {
				$scope.tableParamsChannel.reload();
			});
		}
	]);
});
