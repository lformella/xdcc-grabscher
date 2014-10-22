//
//  snapshot.js
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

define(['./module', 'jqFlot'], function (ng) {
	'use strict';

	ng.controller('SnapshotCtrl', ['$rootScope', '$scope', '$translate', '$filter', 'SignalrService', '$timeout',
		function ($rootScope, $scope, $translate, $filter, SignalrService, $timeout)
		{
			var eventCallbacks = [
				{
					name: 'OnConnected',
					callback: function ()
					{
						$scope.updateSnapshots();
					}
				}
			];
			SignalrService.attachEventCallbacks('snapshotHub', eventCallbacks);
			$scope.proxy = SignalrService.getProxy('snapshotHub');

			$scope.groups = [
				{
					name: 'Servers',
					types: [
						{ enabled: false, id: Enum.SnapshotValue.Servers },
						{ enabled: false, id: Enum.SnapshotValue.ServersEnabled },
						{ enabled: false, id: Enum.SnapshotValue.ServersDisabled },
						{ enabled: false, id: Enum.SnapshotValue.ServersConnected },
						{ enabled: false, id: Enum.SnapshotValue.ServersDisconnected }
					]
				},
				{
					name: 'Channels',
					types: [
						{ enabled: false, id: Enum.SnapshotValue.Channels },
						{ enabled: false, id: Enum.SnapshotValue.ChannelsEnabled },
						{ enabled: false, id: Enum.SnapshotValue.ChannelsDisabled },
						{ enabled: false, id: Enum.SnapshotValue.ChannelsConnected },
						{ enabled: false, id: Enum.SnapshotValue.ChannelsDisconnected }
					]
				},
				{
					name: 'Bots',
					types: [
						{ enabled: false, id: Enum.SnapshotValue.Bots },
						{ enabled: false, id: Enum.SnapshotValue.BotsConnected },
						{ enabled: false, id: Enum.SnapshotValue.BotsDisconnected },
						{ enabled: false, id: Enum.SnapshotValue.BotsFreeSlots },
						{ enabled: false, id: Enum.SnapshotValue.BotsFreeQueue },
						{ enabled: false, id: Enum.SnapshotValue.BotsAverageCurrentSpeed },
						{ enabled: false, id: Enum.SnapshotValue.BotsAverageMaxSpeed }
					]
				},
				{
					name: 'Packets',
					types: [
						{ enabled: false, id: Enum.SnapshotValue.Packets },
						{ enabled: false, id: Enum.SnapshotValue.PacketsConnected },
						{ enabled: false, id: Enum.SnapshotValue.PacketsDisconnected },
						{ enabled: false, id: Enum.SnapshotValue.PacketsSize },
						{ enabled: false, id: Enum.SnapshotValue.PacketsSizeDownloading },
						{ enabled: false, id: Enum.SnapshotValue.PacketsSizeNotDownloading },
						{ enabled: false, id: Enum.SnapshotValue.PacketsSizeConnected },
						{ enabled: false, id: Enum.SnapshotValue.PacketsSizeDisconnected }
					]
				}
			];
			$scope.snapshots = {};
			$scope.days = 1;

			$scope.updateSnapshots = function ()
			{
				if (!SignalrService.isConnected())
				{
					return;
				}

				var signalR = null;
				try
				{
					signalR = $scope.proxy.server.getSnapshots($scope.days);
				}
				catch (e)
				{
					var message = { source: { status: 404 }};
					$rootScope.$emit('AnErrorOccurred', message);
				}

				if (signalR != null)
				{
					signalR.done(
						function (data)
						{
							$.each(data, function (index, item)
							{
								item.color = index;
								item.label = $translate('Snapshot_' + item.type);
								switch (index + 1)
								{
									case Enum.SnapshotValue.Bots:
									case Enum.SnapshotValue.BotsConnected:
									case Enum.SnapshotValue.BotsDisconnected:
									case Enum.SnapshotValue.BotsFreeQueue:
									case Enum.SnapshotValue.BotsFreeSlots:
										item.yaxis = 2;
										break;

									case Enum.SnapshotValue.Packets:
									case Enum.SnapshotValue.PacketsConnected:
									case Enum.SnapshotValue.PacketsDisconnected:
										item.yaxis = 3;
										break;

									case Enum.SnapshotValue.PacketsSize:
									case Enum.SnapshotValue.PacketsSizeDownloading:
									case Enum.SnapshotValue.PacketsSizeNotDownloading:
									case Enum.SnapshotValue.PacketsSizeConnected:
									case Enum.SnapshotValue.PacketsSizeDisconnected:
										item.yaxis = 4;
										break;

									case Enum.SnapshotValue.Speed:
									case Enum.SnapshotValue.BotsAverageCurrentSpeed:
									case Enum.SnapshotValue.BotsAverageMaxSpeed:
										item.yaxis = 5;
										break;

									default:
										item.yaxis = 1;
										break;
								}
							});

							$scope.snapshots = data;
							$timeout($scope.render, 250);
						}
					);
				}
			};

			$scope.$watch('days', function ()
			{
				$scope.updateSnapshots();
			});

			$rootScope.$on('OnSlideTo', function (e, slide)
			{
				if (slide == 5)
				{
					$timeout($scope.render, 250);
				}
			});
		}
	]);
});
