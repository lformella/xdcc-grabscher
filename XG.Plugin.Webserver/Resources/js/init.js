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
		'angular': '../../../Scripts/angular',
		'ngAnimate': '../../../Scripts/angular-animate',
		'domReady': './libs/domReady',
		'jquery': '../../../Scripts/jquery-2.0.3.min',
		'ngTranslate': './libs/angular-translate.min',
		'favicon': './libs/favicon',
		'ipCookies': './libs/angular-cookie',
		'jqKnob': './libs/jquery.knob',
		'jqFlot': '../../../Scripts/flot/jquery.flot',
		'jqFlot.time': '../../../Scripts/flot/jquery.flot.time.min',
		'jqFlot.pie': '../../../Scripts/flot/jquery.flot.pie.min',
		'jqFlot.axislabels': './libs/jquery.flot.axislabels',
		'moment': '../../../Scripts/moment-with-langs',
		'ngSanitize': '../../../Scripts/angular-sanitize',
		'ngTable': './libs/ng-table',
		'signalr': '../../../Scripts/jquery.signalR-2.0.0',
		'signalr.hubs': '../../../signalr/hubs?noext=',
		'ui.bootstrap': '../../../Scripts/ui-bootstrap-tpls-0.7.0'
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
