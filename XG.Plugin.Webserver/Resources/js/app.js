//
//  app.js
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

define([
	'angular',
	'ui.bootstrap',
	'ngSanitize',
	'ngTable',
	'ipCookies',
	'./index'
], function (angular) {
	'use strict';

	return angular.module('xg', [
		'ui.bootstrap',
		'ngSanitize',
		'ngTable',
		'ivpusic.cookie',
		'xg.directive',
		'xg.filter',
		'xg.filter.formatter',
		'xg.service',
		'xg.controller',
		'xg.controller.dialog',
		'xg.config',
		'xg.i18n'
	]);
});
