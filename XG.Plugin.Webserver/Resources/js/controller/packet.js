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

define(['./module'], function (controller) {
	'use strict';

	controller.controller('PacketCtrl', ['$rootScope', '$scope', 'SignalrCrud', 'ngTableParams',
		function ($rootScope, $scope, SignalrCrud, ngTableParams)
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
			$scope.service = new SignalrCrud();
			$scope.service.initialize('packetHub', $scope, 'objects', eventCallbacks);

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
					var sortBy = '';
					var sort = '';

					var keys = Object.keys(params.$params.sorting);
					if (keys.length > 0)
					{
						sortBy = keys[0];
						sort = params.$params.sorting[sortBy];
					}

					var signalR = $scope.service.signalrInvoke('LoadBySearch', $scope.search, params.$params.count, params.$params.page, sortBy, sort);
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
				$scope.search = message;
				$scope.tableParams.reload();
			});
		}
	]);
});
