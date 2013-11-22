//
//  en.js
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

define(['./module'], function (i18n) {
	'use strict';

	i18n.config(['$translateProvider',
		function ($translateProvider) {
			$translateProvider.translations('en', {
				/* ************************************************************************************************************** */
				/* GRIDS                                                                                                          */
				/* ************************************************************************************************************** */

				"Id": "Id",

				/* ************************************************************************************************************** */
				/* NOTIFICATIONS                                                                                                  */
				/* ************************************************************************************************************** */

				"Notification_1": "Packet {{Name}} ({{ParentName}}) is complete",
				"Notification_2": "Packet {{Name}} ({{ParentName}}) is not complete",
				"Notification_3": "Packet {{Name}} ({{ParentName}}) is broken",
				"Notification_4": "Packet {{Name}} ({{ParentName}}) was requested",
				"Notification_5": "Packet {{Name}} ({{ParentName}}) was removed",
				"Notification_6": "File {{Name}} is complete",
				"Notification_7": "File {{Name}} has the wrong size",
				"Notification_8": "File {{Name}} could not be build",
				"Notification_9": "Server {{Name}} is connected",
				"Notification_10": "Server {{Name}} could not be connected",
				"Notification_11": "Channel {{Name}} ({{ParentName}}) joined",
				"Notification_12": "Channel {{Name}} ({{ParentName}}) could not be joined",
				"Notification_13": "Channel {{Name}} ({{ParentName}}) is banned",
				"Notification_14": "Channel {{Name}} ({{ParentName}}) parted",
				"Notification_15": "Channel {{Name}} ({{ParentName}}) kicked",
				"Notification_16": "Packet {{Name}} ({{ParentName}}) is downloading",
				"Notification_17": "Packet {{Name}} ({{ParentName}}) could not be downloaded",
				"Notification_18": "Bot {{ParentName}} submitted wrong download port for packet {{Name#"
			});
		}
	]);
});
