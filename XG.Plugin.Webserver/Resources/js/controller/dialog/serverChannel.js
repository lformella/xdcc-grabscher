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
			serverSignalr.visible(true);
			channelSignalr.visible(true);

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
					if (!$scope.serverSignalr.isConnected())
					{
						return;
					}

					$scope.serverSignalr.loadData($defer, params);
				}
			});
			$scope.clear = false;

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
					if (!$scope.channelSignalr.isConnected())
					{
						return;
					}

					if (!$scope.clear && $scope.serverSignalr.server != null)
					{
						$scope.channelSignalr.loadData($defer, params, 'loadByServer', $scope.serverSignalr.server.Guid);
					}
					else
					{
						$defer.resolve({});
					}
				}
			});

			$scope.channelDefault = { Guid: '', Name: '', MessageAfterConnect: '' };
			$scope.channelSignalr.channel = $scope.channelDefault;
			$scope.saveChannel = function()
			{
				var channel = $scope.channelSignalr.channel;

				if (channel.Guid == '')
				{
					if (channel.Name != '')
					{
						$scope.channelSignalr.getProxy().server.add($scope.serverSignalr.server.Guid, channel.Name, channel.MessageAfterConnect);
						$scope.channelSignalr.channel = $scope.channelDefault;
					}
				}
				else
				{
					$scope.channelSignalr.getProxy().server.setMessageAfterConnect(channel.Guid, channel.MessageAfterConnect);
					$scope.channelSignalr.channel = $scope.channelDefault;
				}
			};

			$scope.channelKeydown = function($event)
			{
				if ($event.keyCode == 13)
				{
					$scope.saveChannel();
				}
			};

			$scope.enableAskForVersion = function(channel)
			{
				$scope.channelSignalr.getProxy().server.enableAskForVersion(channel.Guid);
			};

			$scope.disableAskForVersion = function(channel)
			{
				$scope.channelSignalr.getProxy().server.disableAskForVersion(channel.Guid);
			};

			$scope.$watch('serverSignalr.server', function ()
			{
				$scope.channelSignalr.channel = $scope.channelDefault;
				$scope.tableParamsChannel.page(1);
				$scope.clear = true;
				$scope.tableParamsChannel.reload();
				$scope.clear = false;
				$scope.tableParamsChannel.reload();
			});
		}
	]);
});
