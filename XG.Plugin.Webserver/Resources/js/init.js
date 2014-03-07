//
//  init.js
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

// let Angular know that we're bootstrapping manually
window.name = "NG_DEFER_BOOTSTRAP!";

require.config({
	paths: {
		'angular': '../../../Scripts/angular.min',
		'ngAnimate': '../../../Scripts/angular-animate.min',
		'ngTranslate': './libs/angular-translate.min',
		'ngSanitize': '../../../Scripts/angular-sanitize.min',
		'ngTable': './libs/ng-table',
		'domReady': './libs/domReady',
		'favicon': './libs/favicon',
		'ipCookies': './libs/angular-cookie',
		'jquery': '../../../Scripts/jquery-2.1.0.min',
		'jqKnob': './libs/jquery.knob',
		'jqFlot': '../../../Scripts/flot/jquery.flot.min',
		'jqFlot.axislabels': './libs/jquery.flot.axislabels',
		'jqFlot.pie': '../../../Scripts/flot/jquery.flot.pie.min',
		'jqFlot.time': '../../../Scripts/flot/jquery.flot.time.min',
		'moment': '../../../Scripts/moment-with-langs.min',
		'sha256': './libs/sha256',
		'signalr': '../../../Scripts/jquery.signalR-2.0.2.min',
		'signalr.hubs': '../../../signalr/hubs?noext=',
		'ui.bootstrap': '../../../Scripts/ui-bootstrap-tpls-0.10.0.min'
	},
	shim: {
		'angular': {
			exports: 'angular',
			deps: ['jquery', 'signalr.hubs']
		},
		'ngAnimate': {
			deps: ['angular']
		},
		'ipCookies': {
			deps: ['angular']
		},
		'jqFlot': {
			deps: ['jquery']
		},
		'jqFlot.time': {
			deps: ['jqFlot']
		},
		'jqFlot.pie': {
			deps: ['jqFlot']
		},
		'jqFlot.axislabels': {
			deps: ['jqFlot']
		},
		'jqKnob': {
			deps: ['jquery']
		},
		'ngTranslate': {
			deps: ['angular']
		},
		'ngSanitize': {
			deps: ['angular']
		},
		'ngTable': {
			deps: ['angular']
		},
		'signalr': {
			deps: ['jquery']
		},
		'signalr.hubs': {
			deps: ['signalr']
		},
		'ui.bootstrap': {
			deps: ['angular']
		}
	},
	deps: ['./bootstrap']
});
