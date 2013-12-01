//
//  xdcc.js
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

	ng.controller('XdccDialogCtrl', ['$scope', '$modalInstance', 'SignalrService',
		function ($scope, $modalInstance, SignalrService)
		{
			$scope.proxy = SignalrService.getProxy('externalHub');
			$scope.xdccLink = "";

			$scope.isXdccLinkValid = function ()
			{
				return $scope.xdccLink.match(/^xdcc:\/\/([^/]+\/){2}#([^/]+\/){2}#[0-9]+\/[^/]+\/$/);
			};

			$scope.xdccKeydown = function($event)
			{
				if ($event.keyCode == 13)
				{
					$scope.save();
				}
			};

			$scope.save = function ()
			{
				if ($scope.isXdccLinkValid())
				{
					if (SignalrService.isConnected())
					{
						$scope.proxy.server.parseXdccLink($scope.xdccLink);
					}
					$modalInstance.close();
				}
			};
		}
	]);
});
