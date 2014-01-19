//
//  notification.js
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

	ng.filter('formatNotificationIcon', ['$filter', function ($filter)
	{
		return function (notification)
		{
			if (notification == undefined)
			{
				return "";
			}

			var icon = "";
			var iconClass = "";

			switch (notification.Type)
			{
				case Enum.NotificationType.PacketCompleted:
				case Enum.NotificationType.FileCompleted:
					iconClass = "ChameleonMiddle";
					break;

				case Enum.NotificationType.ServerConnectFailed:
				case Enum.NotificationType.ChannelJoinFailed:
				case Enum.NotificationType.ChannelBanned:
				case Enum.NotificationType.ChannelKicked:
				case Enum.NotificationType.BotConnectFailed:
				case Enum.NotificationType.BotSubmittedWrongData:
				case Enum.NotificationType.PacketIncomplete:
				case Enum.NotificationType.PacketBroken:
				case Enum.NotificationType.PacketFileMismatch:
				case Enum.NotificationType.PacketNameDifferent:
				case Enum.NotificationType.FileBuildFailed:
					iconClass = "ScarletRedMiddle";
					break;

				case Enum.NotificationType.ServerConnected:
				case Enum.NotificationType.ChannelParted:
				case Enum.NotificationType.ChannelJoined:
				case Enum.NotificationType.BotConnected:
				case Enum.NotificationType.PacketRequested:
				case Enum.NotificationType.PacketRemoved:
					iconClass = "SkyBlueMiddle";
					break;
			}

			switch (notification.Type)
			{
				case Enum.NotificationType.ServerConnectFailed:
				case Enum.NotificationType.ServerConnected:
					icon = "hdd";
					break;

				case Enum.NotificationType.ChannelJoinFailed:
				case Enum.NotificationType.ChannelBanned:
				case Enum.NotificationType.ChannelKicked:
				case Enum.NotificationType.ChannelParted:
				case Enum.NotificationType.ChannelJoined:
					icon = "comment";

					break;
				case Enum.NotificationType.BotConnectFailed:
				case Enum.NotificationType.BotSubmittedWrongData:
					icon = "user";
					break;

				case Enum.NotificationType.BotConnected:
				case Enum.NotificationType.PacketIncomplete:
				case Enum.NotificationType.PacketBroken:
				case Enum.NotificationType.PacketRequested:
				case Enum.NotificationType.PacketRemoved:
				case Enum.NotificationType.PacketCompleted:
				case Enum.NotificationType.PacketFileMismatch:
				case Enum.NotificationType.PacketNameDifferent:
					icon = "file";
					break;

				case Enum.NotificationType.FileBuildFailed:
				case Enum.NotificationType.FileCompleted:
					icon = "file";
					break;
			}

			return $filter('formatIcon')(icon, iconClass);
		}
	}]);

	ng.filter('formatNotificationContent', ['$translate', function ($translate)
	{
		return function (notification)
		{
			if (notification == undefined)
			{
				return "";
			}

			var msg = $translate("Notification_Description_" + notification.Type,
			{
				ObjectName1: '<b>' + notification.ObjectName1 + '</b>',
				ParentName1: '<b>' + notification.ParentName1 + '</b>',
				ObjectName2: '<b>' + notification.ObjectName2 + '</b>',
				ParentName2: '<b>' + notification.ParentName2 + '</b>'
			});
			return "<span title='" + msg + "'>" + msg.replace(/\s+/g, '&nbsp;') + "</span>";
		}
	}]);

	ng.filter('formatNotificationHeader', ['$translate', function ($translate)
	{
		return function (notification)
		{
			if (notification == undefined)
			{
				return "";
			}

			return $translate("Notification_Header_" + notification.Type);
		}
	}]);

	ng.filter('formatNotificationDescription', ['$translate', function ($translate)
	{
		return function (notification)
		{
			if (notification == undefined)
			{
				return "";
			}

			return $translate("Notification_Description_" + notification.Type,
			{
				ObjectName1: notification.ObjectName1,
				ParentName1: notification.ParentName1,
				ObjectName2: notification.ObjectName2,
				ParentName2: notification.ParentName2
			});
		}
	}]);
});
