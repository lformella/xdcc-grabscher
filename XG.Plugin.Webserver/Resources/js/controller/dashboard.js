//
//  dashboard.js
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

	ng.controller('DashboardCtrl', ['$rootScope', '$scope', 'SignalrService',
		function ($rootScope, $scope, SignalrService)
		{
			var eventCallbacks = [
				{
					name: 'OnConnected',
					callback:  function ()
					{
						$scope.refreshSnapshot();
						$scope.loop();
					}
				}
			];
			SignalrService.attachEventCallbacks('snapshotHub', eventCallbacks);
			$scope.proxy = SignalrService.getProxy('snapshotHub');

			$scope.snapshot = {};
			$scope.snapshot[Enum.SnapshotValue.Servers] = 0;
			$scope.snapshot[Enum.SnapshotValue.ServersEnabled] = 0;
			$scope.snapshot[Enum.SnapshotValue.ServersConnected] = 0;
			$scope.snapshot[Enum.SnapshotValue.Channels] = 0;
			$scope.snapshot[Enum.SnapshotValue.ChannelsEnabled] = 0;
			$scope.snapshot[Enum.SnapshotValue.ChannelsConnected] = 0;
			$scope.snapshot[Enum.SnapshotValue.Bots] = 0;
			$scope.snapshot[Enum.SnapshotValue.BotsConnected] = 0;
			$scope.snapshot[Enum.SnapshotValue.FileSize] = 0;
			$scope.snapshot[Enum.SnapshotValue.FileSizeMissing] = 0;
			$scope.snapshot[Enum.SnapshotValue.FileSizeDownloaded] = 0;
			$scope.snapshot[Enum.SnapshotValue.FileTimeMissing] = 0;

			$scope.refreshSnapshot = function ()
			{
				$scope.proxy.server.getFlotSnapshot().done(
					function (data)
					{
						var liveSnapshot = {};
						$.each(data, function (index, item)
						{
							liveSnapshot[item.type] = item.data[0][1];
						});
						liveSnapshot[Enum.SnapshotValue.FileSize] = liveSnapshot[Enum.SnapshotValue.FileSizeDownloaded] + liveSnapshot[Enum.SnapshotValue.FileSizeMissing];

						$scope.snapshot = liveSnapshot;
						$scope.$apply();
					}
				);
			};

			$scope.refresh = true;
			$rootScope.$on('OnSlideTo', function (e, slide)
			{
				$scope.refresh = slide == 1;
			});

			$scope.loop = function ()
			{
				if ($scope.refresh)
				{
					$scope.refreshSnapshot();
				}

				setTimeout(function ()
				{
					$scope.loop();
				}, 10000);
			};
		}
	]);
});
