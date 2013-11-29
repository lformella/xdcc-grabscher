//
//  main.js
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

	controller.controller('MainCtrl', ['$rootScope', '$scope', '$modal', 'ipCookie', 'SignalrCrudTable',
		function ($rootScope, $scope, $modal, ipCookie, SignalrCrudTable)
		{
			$scope.passwordOk = false;
			$modal.open({
				keyboard: false,
				backdrop: 'static',
				templateUrl: 'passwordDialog.html',
				controller: 'PasswordDialogCtrl'
			}).result.then(function (password)
			{
				$scope.passwordOk = true;
				$.connection.hub.start().done(
					function ()
					{
						$rootScope.$emit('OnConnected', password);
					}
				).fail(
					function (message)
					{
						alert(message);
					}
				);
				ipCookie('xg.password', password, { expires: 21, path: '/' });
			});

			$scope.openXdccDialog = function ()
			{
				$modal.open({
					keyboard: true,
					backdrop: true,
					templateUrl: 'xdccDialog.html',
					controller: 'XdccDialogCtrl'
				}).result.then(function (xdccLink)
				{
					alert(xdccLink);
				});
			};

			// build this here, because the dialogs will respawn and recreate stuff
			$scope.servers = [];
			$scope.serverService = new SignalrCrudTable();
			$scope.serverService.initialize('serverHub', $scope, 'servers', undefined, 'tableParamsServer');

			$scope.channelService = new SignalrCrudTable();
			$scope.channelService.initialize('channelHub', $scope, 'channels', undefined, 'tableParamsChannel');
			$scope.channels = [];

			$scope.openServerChannelsDialog = function ()
			{
				$modal.open({
					keyboard: true,
					backdrop: true,
					templateUrl: 'serverChannelDialog.html',
					controller: 'ServerChannelDialogCtrl',
					resolve:
					{
						serverService: function ()
						{
							return $scope.serverService;
						},
						channelService: function ()
						{
							return $scope.channelService;
						}
					}
				});
			};

			$rootScope.settings = {
				showOfflineBots: ipCookie('xg.showOfflineBots'),
				humanDates: ipCookie('xg.humanDates')
			};

			$scope.flipSetting = function (setting)
			{
				var newValue = !$rootScope.settings[setting];
				$rootScope.settings[setting] = newValue;
				ipCookie('xg.' + setting, newValue ? '1' : '0', { expires: 21, path: '/' });
			};
		}
	]);
});
