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

			$scope.slide = 1;
			$scope.slideTo = function (slide)
			{
				$scope.slide = slide;
			};

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
							$.each(data.FileHandlers, function (a, fileHandler)
							{
								fileHandler.Processes = [];

								var currentProcess = fileHandler.Process;
								while (currentProcess != undefined)
								{
									fileHandler.Processes.push(currentProcess);
									currentProcess = currentProcess.Next;
								}

								resetProcessIndex(fileHandler);
							});

							$scope.config = data;
							resetFileHandlerIndex();

							$scope.configReady = true;
							$scope.$apply();
						}
					);
				}
			}

			var resetFileHandlerIndex = function()
			{
				$.each($scope.config.FileHandlers, function (a, fileHandler)
				{
					fileHandler.Id = a;
				});
			};

			var resetProcessIndex = function(fileHandler)
			{
				$.each(fileHandler.Processes, function (a, process)
				{
					process.Id = a;
				});
			};

			$scope.addFileHandler = function()
			{
				var fileHandler = {
					Regex: "",
					Processes: [{
						Id: 0,
						Command: "",
						Arguments: ""
					}]
				};
				$scope.config.FileHandlers.push(fileHandler);
				resetFileHandlerIndex();
			};

			$scope.removeFileHandler = function(fileHandler)
			{
				$scope.config.FileHandlers.splice(fileHandler.Id, 1);
				resetFileHandlerIndex();
			};

			$scope.addProcess = function(fileHandler, afterProcess)
			{
				var process = {
					Command: "",
					Arguments: ""
				};
				fileHandler.Processes.splice(afterProcess.Id + 1, 0, process);
				resetProcessIndex(fileHandler);
			};

			$scope.removeProcess = function(fileHandler, process)
			{
				fileHandler.Processes.splice(process.Id, 1);
				resetProcessIndex(fileHandler);
			};

			$scope.save = function()
			{
				if (!$scope.signalr.isConnected())
				{
					return;
				}

				var data = $scope.config;
				$.each(data.FileHandlers, function (a, fileHandler)
				{
					var previousProcess = null;
					$.each(fileHandler.Processes, function (b, process)
					{
						if (b == 0)
						{
							fileHandler.Process = process;
						}
						else if (previousProcess != null)
						{
							previousProcess.Next = process;
						}
						previousProcess = process;

						delete process.Id;
					});

					delete fileHandler.Id;
					delete fileHandler.Processes;
				});

				$scope.signalr.getProxy().server.save(data);
				$modalInstance.close();
			};
		}
	]);
});
