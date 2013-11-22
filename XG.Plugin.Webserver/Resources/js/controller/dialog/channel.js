//
//  channel.js
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

	controller.controller('ChannelCtrl', ['$scope', 'SignalrCrudTable', '$filter', 'ngTableParams',
		function ($scope, SignalrCrudTable, $filter, ngTableParams)
		{
			$scope.service = $scope.channelService;
			$scope.service.setScope($scope.$parent);
			$scope.service.server = null;

			$scope.$parent.tableParamsChannel = new ngTableParams({
				page: 1,
				count: 10
			}, {
				counts: [],
				total: 0,
				getData: function($defer, params)
				{
					if ($scope.service.server != null)
					{
						$scope.service.loadData($defer, params, 'LoadByServer', $scope.service.server.Guid);
					}
					else
					{
						$defer.resolve({});
					}
				}
			});

			$scope.$parent.addChannel = function()
			{
				$scope.service.add($scope.channel, $scope.service.server.Guid);
				$scope.channel = '';
			};

			$scope.$parent.channelKeydown = function($event)
			{
				if ($event.keyCode == 13 && $scope.service.server != null)
				{
					$scope.$parent.addChannel();
				}
			};
		}
	]);
});
