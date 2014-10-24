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

requirejs.onError = function (err)
{
	alert("error: " + err);
};

require.config({
	paths: {
		'angular':				'../../Scripts/angular.min',
		'ngAnimate':			'../../Scripts/angular-animate.min',
		'ipCookies':			'../../Scripts/angular-cookie.min',
		'ngSanitize':			'../../Scripts/angular-sanitize.min',
		'ngTranslate':			'./libs/angular-translate.min',
		'ui.bootstrap':			'../../Scripts/angular-ui/ui-bootstrap-tpls.min',
		'ngTable':				'../../Scripts/ng-table.min',

		'domReady':				'./libs/domReady',
		'favico':				'../../Scripts/favico.min',
		'jqKnob':				'./libs/jquery.knob',

		'jquery':				'../../Scripts/jquery-2.1.1.min',
		'jqFlot':				'../../Scripts/flot/jquery.flot.min',
		'jqFlot.pie':			'../../Scripts/flot/jquery.flot.pie.min',
		'jqFlot.time':			'../../Scripts/flot/jquery.flot.time.min',
		'jqFlot.axislabels':	'./libs/jquery.flot.axislabels',

		'moment':				'../../Scripts/moment-with-locales.min',
		'signalr':				'../../Scripts/jquery.signalR-2.1.2.min',
		'signalr.hubs':			'../../../signalr/hubs?noext=',
		'sha256':				'./libs/sha256'
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
