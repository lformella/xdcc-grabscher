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

define(['./module'], function (controller) {
	'use strict';

	controller.controller('DashboardCtrl', ['$rootScope', '$scope', 'Signalr',
		function ($rootScope, $scope, Signalr)
		{
			var eventCallbacks = [
				{
					name: 'OnConnected',
					callback:  function ()
					{
						$scope.service.invoke('GetFlotSnapshot').done(
							function (data)
							{
								var liveSnapshot = {};
								$.each(data, function (index, item)
								{
									liveSnapshot[item.label] = item.data[0][1];
								});

								$scope.snapshot = liveSnapshot;
								$scope.$apply();
							}
						);
					}
				}
			];
			$scope.service = new Signalr();
			$scope.service.initialize('snapshotHub', eventCallbacks);

			$scope.snapshot = {
				Servers: 0,
				ServersEnabled: 0,
				ServersConnected: 0,
				Channels: 0,
				ChannelsEnabled: 0,
				ChannelsConnected: 0,
				Bots: 0,
				BotsConnected: 0,
				FileSize: 0,
				FileTimeMissing: 0,
				FileSizeMissing: 0,
				FileSizeDownloaded: 0
			}
		}
	]);
});
