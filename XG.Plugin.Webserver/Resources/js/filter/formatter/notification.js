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
				case Enum.NotificationType.BotSubmittedWrongPort:
				case Enum.NotificationType.PacketIncompleted:
				case Enum.NotificationType.PacketBroken:
				case Enum.NotificationType.FileSizeMismatch:
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

				case Enum.NotificationType.BotConnected:
				case Enum.NotificationType.BotConnectFailed:
				case Enum.NotificationType.BotSubmittedWrongPort:
					icon = "user";
					break;

				case Enum.NotificationType.PacketIncompleted:
				case Enum.NotificationType.PacketBroken:
				case Enum.NotificationType.PacketRequested:
				case Enum.NotificationType.PacketRemoved:
				case Enum.NotificationType.PacketCompleted:
					icon = "file";
					break;

				case Enum.NotificationType.FileSizeMismatch:
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

			var msg = $translate("Notification_" + notification.Type,
			{
				Name: notification.ObjectName,
				ParentName: notification.ParentName
			});
			return "<span title='" + msg + "'>" + msg + "</span>";
		}
	}]);
});
