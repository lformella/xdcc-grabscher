//
//  api.js
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

	ng.controller('ConfigDialogCtrl', ['$scope', '$modalInstance', 'signalr',
		function ($scope, $modalInstance, signalr)
		{
			$scope.signalr = signalr;

			$scope.configReady = false;
			$scope.config = {};
			if ($scope.signalr.isConnected())
			{
				var signalR = $scope.signalr.getProxy().server.load();
				if (signalR != null)
				{
					signalR.done(
						function (data)
						{
							$scope.config = data;
							$scope.configReady = true;
						}
					);
				}
			}

			$scope.save = function()
			{
				if (!$scope.signalr.isConnected())
				{
					return;
				}

				$scope.signalr.getProxy().server.save($scope.config);
				$modalInstance.close();
			};
		}
	]);
});
