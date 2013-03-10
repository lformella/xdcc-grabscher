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

var XGNotification = (function()
{
	return {
		handleMessage: function (message)
		{
			var type = "";

			switch (message.Type)
			{
				case Enum.NotificationType.PacketCompleted:
				case Enum.NotificationType.FileCompleted:
					type = "success";
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
					type = "error";
					break;

				case Enum.NotificationType.ServerConnected:
				case Enum.NotificationType.ChannelParted:
				case Enum.NotificationType.ChannelJoined:
				case Enum.NotificationType.BotConnected:
				case Enum.NotificationType.PacketRequested:
				case Enum.NotificationType.PacketRemoved:
					type = "info";
					break;
			}

			$.pnotify({
				//title: 'Oh No!',
				text: _("Notification_" + message.Type, [{ Name: "Name", Value: message.ObjectName }]),
				type: type,
				hide: type == "info" || type == "",
				styling: 'jqueryui',
				animation: {
					'effect_in': 'scale',
					'options_in': { 'easing': 'easeOutElastic', percent: 100 },
					'effect_out': 'scale',
					'options_out': { 'easing': 'easeOutElastic', percent: 0 }
				}
			});
		}
	}
}());
