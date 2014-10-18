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
				/* SNAPSHOTS                                                                                                      */
				/* ************************************************************************************************************** */

				"Snapshot_1": "Speed",
				"Snapshot_2": "Servers",
				"Snapshot_21": "Server enabled",
				"Snapshot_22": "Server disabled",
				"Snapshot_3": "Server connected",
				"Snapshot_4": "Server disconnected",
				"Snapshot_5": "Channels",
				"Snapshot_23": "Channel enabled",
				"Snapshot_24": "Channel disabled",
				"Snapshot_6": "Channel connected",
				"Snapshot_7": "Channel disconnected",
				"Snapshot_8": "Bots",
				"Snapshot_9": "Bots connected",
				"Snapshot_10": "Bots disconnected",
				"Snapshot_11": "Bots free slots",
				"Snapshot_12": "Bots free queue",
				"Snapshot_19": "Bots average current speed",
				"Snapshot_20": "Bots average max speed",
				"Snapshot_13": "Packets",
				"Snapshot_14": "Packets connected",
				"Snapshot_15": "Packets disconnected",
				"Snapshot_16": "Packets size",
				"Snapshot_17": "Packets size downloading",
				"Snapshot_18": "Packets size not downloading",
				"Snapshot_25": "Packets size connected",
				"Snapshot_26": "Packets size disconnected",
				"Snapshot_27": "File size downloaded",
				"Snapshot_28": "File size missing",
				"Snapshot_29": "File time missing",

				/* ************************************************************************************************************** */
				/* NOTIFICATIONS                                                                                                  */
				/* ************************************************************************************************************** */

				"Notification_Header_1": "Packet is complete",
				"Notification_Description_1": "The Packet {{ObjectName1}} of {{ParentName1}} is complete",
				"Notification_Header_2": "Packet is not complete",
				"Notification_Description_2": "The Packet {{ObjectName1}} of {{ParentName1}} is not complete",
				"Notification_Header_3": "Packet is broken",
				"Notification_Description_3": "The Packet {{ObjectName1}} of {{ParentName1}} is broken",
				"Notification_Header_4": "Packet was requested",
				"Notification_Description_4": "The Packet {{ObjectName1}} of {{ParentName1}} was requested",
				"Notification_Header_5": "Packet was removed",
				"Notification_Description_5": "The Packet {{ObjectName1}} of {{ParentName1}} was removed",
				"Notification_Header_6": "File is complete",
				"Notification_Description_6": "The File {{ObjectName1}} is complete",
				"Notification_Header_7": "Packet does not match File",
				"Notification_Description_7": "Paket {{ObjectName1}} does not match the file {{ObjectName2}}",
				"Notification_Header_8": "Filecould not be build",
				"Notification_Description_8": "The File {{ObjectName1}} could not be build",
				"Notification_Header_9": "Server is connected",
				"Notification_Description_9": "The Server {{ObjectName1}} is connected",
				"Notification_Header_10": "Server could not be connected",
				"Notification_Description_10": "The Server {{ObjectName1}} could not be connected",
				"Notification_Header_11": "Channel joined",
				"Notification_Description_11": "I joined Channel {{ObjectName1}} of {{ParentName1}}",
				"Notification_Header_12": "Channel could not be joined",
				"Notification_Description_12": "I could not join Channel {{ObjectName1}} of {{ParentName1}}",
				"Notification_Header_13": "Channel is banned",
				"Notification_Description_13": "I was banned from Channel {{ObjectName1}} of {{ParentName1}}",
				"Notification_Header_14": "Channel parted",
				"Notification_Description_14": "I parted Channel {{ObjectName1}} of {{ParentName1}}",
				"Notification_Header_15": "Channel kicked",
				"Notification_Description_15": "I was kicked from Channel {{ObjectName1}} of {{ParentName1}}",
				"Notification_Header_16": "Packet is downloading",
				"Notification_Description_16": "The Packet {{ObjectName1}} of {{ParentName1}} is downloading",
				"Notification_Header_17": "Packet could not be downloaded",
				"Notification_Description_17": "The Packet {{ObjectName1}} of {{ParentName1}} could not be downloaded",
				"Notification_Header_18": "Bot submitted wrong data",
				"Notification_Description_18": "The Bot {{ParentName1}} submitted wrong data for packet {{ObjectName1}}",
				"Notification_Header_19": "Packet name is different",
				"Notification_Description_19": "The submitted name of the Packet {{ObjectName1}} is different from the expected",

				/* ************************************************************************************************************** */
				/* OTHERS                                                                                                         */
				/* ************************************************************************************************************** */

				"NewVersionAvailable": "A new version is available: <a href='{{Url}}' target='_blank'>{{Latest}} ({{Name}})</a>"
			});
		}
	]);
});
