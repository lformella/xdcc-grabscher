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
		'jquery': '../../../Scripts/jquery-2.0.3.min',
		'angular': '../../../Scripts/angular',
		'ngTranslate': './libs/angular-translate.min',
		'domReady': './libs/domReady',
		'signalr': '../../../Scripts/jquery.signalR-2.0.0',
		'ngSanitize': '../../../Scripts/angular-sanitize',
		'ui.bootstrap': '../../../Scripts/ui-bootstrap-tpls-0.7.0',
		'ngTable': './libs/ng-table',
		'ipCookies': './libs/angular-cookie',
		'moment': '../../../Scripts/moment-with-langs',
		'jqKnob': './libs/jquery.knob'
	},
	shim: {
		'angular': {
			exports: 'angular',
			deps: ['jquery']
		},
		'ngTranslate': {
			deps: ['angular']
		},
		'ngSanitize': {
			deps: ['angular']
		},
		'ui.bootstrap': {
			deps: ['angular']
		},
		'ngTable': {
			deps: ['angular']
		},
		'ipCookies': {
			deps: ['angular']
		},
		'jqKnob': {
			deps: ['jquery']
		}
	},
	deps: ['./bootstrap']
});
