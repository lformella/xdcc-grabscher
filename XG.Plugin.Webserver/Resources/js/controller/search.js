//
//  search.js
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

	controller.controller('SearchCtrl', ['$rootScope', '$scope', 'SignalrCrud', 'HelperService',
		function ($rootScope, $scope, SignalrCrud, HelperService)
		{
			var eventCallbacks = [
				{
					name: 'OnConnected',
					callback:  function ()
					{
						$scope.service.signalrInvoke("GetAll");
					}
				}
			];
			$scope.service = new SignalrCrud();
			$scope.service.initialize('searchHub', $scope, 'objects', eventCallbacks);

			$scope.objects = [];

			$scope.predefinedSearches =
			[
				{
					Guid: '00000000-0000-0000-0000-000000000001',
					Name: 'Enabled',
					ResultsOnline: 0,
					ResultsOffline: 0
				},
				{
					Guid: '00000000-0000-0000-0000-000000000002',
					Name: 'Downloads',
					ResultsOnline: 0,
					ResultsOffline: 0
				}
			];

			// attach methods to service
			$scope.service.removeByName = function (name)
			{
				var element = HelperService.getByName($scope.objects, name);
				if (element != null)
				{
					$scope.service.remove(element.value);
				}
			};

			$scope.service.isObject = function(name)
			{
				return HelperService.getByName($scope.objects, name) != null;
			};

			// events
			$scope.loading = false;

			$scope.searchByGuid = function(guid)
			{
				$scope.loading = true;
				$rootScope.$emit('SearchByGuid', guid);
			};

			$scope.searchByName = function(search)
			{
				$scope.loading = true;
				$rootScope.$emit('SearchByName', search);
			};

			$scope.searchKeydown = function($event)
			{
				if ($event.keyCode == 13)
				{
					$scope.searchByName($scope.search);
				}
			};

			$rootScope.$on('OnSearchComplete', function ()
			{
				$scope.loading = false;
				$scope.$apply();
			});
		}
	]);
});
