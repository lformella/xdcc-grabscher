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

define(['./module'], function (ng) {
	'use strict';

	ng.controller('SearchCtrl', ['$rootScope', '$scope', 'SignalrFactory', 'HelperService', '$translate',
		function ($rootScope, $scope, SignalrFactory, HelperService, $translate)
		{
			var eventCallbacks = [
				{
					name: 'OnConnected',
					callback: function ()
					{
						$scope.signalr.visible(true);
						$scope.signalr.getProxy().server.getAll();
					}
				}
			];
			$scope.signalr = new SignalrFactory();
			$scope.signalr.initialize('searchHub', $scope, 'objects', eventCallbacks);

			$scope.objects = [];

			$scope.predefinedSearches =
			[
				{
					Guid: '00000000-0000-0000-0000-000000000001',
					Name: $translate('Enabled'),
					Size: 0,
					ResultsOnline: 0,
					ResultsOffline: 0
				},
				{
					Guid: '00000000-0000-0000-0000-000000000002',
					Name: $translate('Downloads'),
					Size: 0,
					ResultsOnline: 0,
					ResultsOffline: 0
				}
			];

			$scope.formats = ['G', 'M', 'K', 'B'];
			$scope.format = $scope.formats[3];

			// size formatter
			$scope.getRealSize = function ()
			{
				if ($scope.format == 'G')
				{
					return $scope.size * 1024 * 1024 * 1024;
				}
				else if ($scope.format == 'M')
				{
					return $scope.size * 1024 * 1024;
				}
				else if ($scope.format == 'K')
				{
					return $scope.size * 1024;
				}
				else
				{
					return $scope.size;
				}
			};

			// array traversal
			var getBySearchAndSize = function (array, search, size)
			{
				var element = null;
				angular.forEach(array, function (value, key)
				{
					if (value["Name"] == search && value["Size"] == size)
					{
						element = { value: value, id: key };
						return false;
					}
					return true;
				});
				return element;
			};

			// attach methods to service
			$scope.signalr.removeByParameter = function (search, size)
			{
				var element = getBySearchAndSize($scope.objects, search, size);
				if (element != null)
				{
					$scope.signalr.remove(element.value);
				}
			};

			$scope.signalr.isObject = function(search, size)
			{
				return getBySearchAndSize($scope.objects, search, size) != null;
			};

			// events
			$scope.loading = false;

			$scope.searchByGuid = function(guid)
			{
				$scope.loading = true;
				$rootScope.$emit('SearchByGuid', guid);
			};

			$scope.searchByParameter = function(search, size)
			{
				$scope.loading = true;
				$rootScope.$emit('SearchByParameter', search, size);
			};

			$scope.searchKeydown = function($event)
			{
				if ($event.keyCode == 13)
				{
					$scope.triggerSearch();
				}
			};

			$scope.triggerSearch = function($event)
			{
				$scope.searchByParameter($scope.search, $scope.getRealSize());
			};

			$scope.searchClicked = function(search)
			{
				$scope.searchByGuid(search.Guid);
				$scope.search = search.Name;

				if (search.Size > 1024 * 1024 * 1024)
				{
					$scope.format = 'G';
					$scope.size = (search.Size / (1024 * 1024 * 1024)).toFixed(2);
				}
				else if (search.Size > 1024 * 1024)
				{
					$scope.format = 'M';
					$scope.size = (search.Size / (1024 * 1024)).toFixed(2);
				}
				else if (search.Size > 1024)
				{
					$scope.format = 'K';
					$scope.size = (search.Size / 1024).toFixed(2);
				}
				else
				{
					$scope.format = 'B';
					$scope.size = search.Size;
				}
			};

			$scope.removeSearch = function($event, search)
			{
				$event.preventDefault();
				$event.stopPropagation();
				$scope.signalr.remove(search);
			};

			$rootScope.$on('OnSearchComplete', function ()
			{
				$scope.loading = false;
				$scope.$apply();
			});

			var resize = function ()
			{
				$("#searchForm .dropdown-menu").css("max-height", ($(window).height() - 70) + "px");
			};

			$(window).resize(function ()
			{
				resize();
			});

			resize();
		}
	]);
});
