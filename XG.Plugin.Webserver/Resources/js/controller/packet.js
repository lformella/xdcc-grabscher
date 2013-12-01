//
//  packet.js
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

	ng.controller('PacketCtrl', ['$rootScope', '$scope', 'SignalrTableFactory', 'ngTableParams',
		function ($rootScope, $scope, SignalrTableFactory, ngTableParams)
		{
			var eventCallbacks = [
				{
					name: 'OnChanged',
					callback:  function ()
					{
						$scope.tableParams.reload();
					}
				}
			];
			$scope.signalr = new SignalrTableFactory();
			$scope.signalr.initialize('packetHub', $scope, 'objects', eventCallbacks);

			$scope.searchBy = "Name";
			$scope.search = "";
			$scope.parents = [];

			$scope.tableParams = new ngTableParams({
				page: 1,
				count: 20
			}, {
				counts: [20, 40, 80, 160, 320, 640],
				groupBy: 'ParentGuid',
				total: 0,
				getData: function($defer, params)
				{
					if (!$scope.signalr.isConnected())
					{
						return;
					}

					var sortBy = '';
					var sort = '';

					var keys = Object.keys(params.$params.sorting);
					if (keys.length > 0)
					{
						sortBy = keys[0];
						sort = params.$params.sorting[sortBy];
					}

					var signalR = $scope.signalr.getProxy().server['loadBy' + $scope.searchBy]($scope.search, $rootScope.settings.showOfflineBots, params.$params.count, params.$params.page, sortBy, sort);
					if (signalR != null)
					{
						signalR.done(
							function (data)
							{
								params.total(data.Total);
								$scope.objects = data.Results;

								$scope.parents = [];
								angular.forEach($scope.objects, function (value)
								{
									$scope.parents[value.ParentGuid] = value.Bot;
								});

								$rootScope.$emit('OnSearchComplete', $scope.search);
								$defer.resolve($scope.objects);
								$scope.$apply();
							}
						);
					}
				}
			});

			// events
			$rootScope.$on('SearchByName', function (e, message)
			{
				$scope.searchBy = "Name";
				$scope.search = message;
				$scope.tableParams.reload();
			});

			$rootScope.$on('SearchByGuid', function (e, message)
			{
				$scope.searchBy = "Guid";
				$scope.search = message;
				$scope.tableParams.reload();
			});

			$rootScope.$watch('settings.showOfflineBots', function () {
				$scope.tableParams.reload();
			});
		}
	]);
});
