//
//  formatter.js
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

var XGFormatter = (function ()
{
	var helper, translate;

	/* ************************************************************************************************************** */
	/* IMAGE FORMATTER                                                                                                */
	/* ************************************************************************************************************** */

	function speed2Overlay (speed)
	{
		var opacity = (speed / (1024 * 1500)) * 0.4;
		return (opacity > 0.4 ? 0.4 : opacity) + 0.6;
	}

	function formatIcon (icon, iconClass, overlay, overlayClass, overlayStyle, onclick)
	{
		iconClass = "icon-" + icon + " " + iconClass;
		if (onclick != undefined && onclick != "")
		{
			iconClass += " button";
		}
		overlayClass = "icon-overlay icon-" + overlay + " " + overlayClass;

		var str = "";
		if (overlay != undefined && overlay != "")
		{
			str += "<i class='" + overlayClass + "'";
			if (overlayStyle != undefined && overlayStyle != "")
			{
				str += " style='" + overlayStyle + "'";
			}
			str += "></i>";
		}
		str += "<i class='" + iconClass + "'";
		if (onclick != undefined && onclick != "")
		{
			str += " onclick='" + onclick + "'";
		}
		str += "></i>";

		return str;
	}

	var self = {
		/**
		 * @param {XGHelper} helper1
		 * @param {XGTranslate} translate1
		 */
		initialize: function (helper1, translate1)
		{
			helper = helper1;
			translate = translate1;
		},

		/* ************************************************************************************************************** */
		/* SERVER FORMATTER                                                                                               */
		/* ************************************************************************************************************** */

		formatServerIcon: function (server, onclick)
		{
			var icon = "hdd";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "opacity: 0.6";

			if (!server.Enabled)
			{
				iconClass = "Aluminium1Dark";
			}
			else if (server.Connected)
			{
				overlay = "ok-circle";
				overlayClass = "ChameleonMiddle";
			}
			else if (server.ErrorCode != "" && server.ErrorCode != "None" && server.ErrorCode != "0")
			{
				overlay = "attention-circle";
				overlayClass = "ScarletRedMiddle";
			}
			else
			{
				overlay = "clock";
				overlayClass = "OrangeMiddle";
			}

			if (server.Active)
			{
				overlay = "spin";
				overlayClass = "ScarletRedMiddle animate-spin icon-small";
				overlayStyle = "";
			}

			return formatIcon(icon, iconClass, overlay, overlayClass, overlayStyle, onclick);
		},

		formatChannelIcon: function (channel, onclick)
		{
			var icon = "comment";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "opacity: 0.6";

			if (!channel.Enabled)
			{
				iconClass = "Aluminium1Dark";
			}
			else if (channel.Connected)
			{
				overlay = "ok-circle";
				overlayClass = "ChameleonMiddle";
			}
			else if (channel.ErrorCode != "" && channel.ErrorCode != "None" && channel.ErrorCode != "0")
			{
				overlay = "attention-circle";
				overlayClass = "ScarletRedMiddle";
			}

			return formatIcon(icon, iconClass, overlay, overlayClass, overlayStyle, onclick);
		},

		formatServerName: function (obj)
		{
			var str = obj.Name; // + ":" + obj.Port;
			if (obj.ErrorCode != "" && obj.ErrorCode != "None" && obj.ErrorCode != "0")
			{
				str += " - <small>" + translate._("Error") + ": " + obj.ErrorCode + "</small>";
			}
			return str;
		},

		formatChannelName: function (obj)
		{
			var str = obj.Name;
			if (obj.ErrorCode != "" && obj.ErrorCode != "None" && obj.ErrorCode != "0")
			{
				str += " - <small>" + translate._("Error") + ": " + obj.ErrorCode + "</small>";
			}
			if (obj.Topic != null)
			{
				str += "<br /><small title='" + obj.Topic + "'>" + obj.Topic + "</small>";
			}
			return str;
		},

		/* ************************************************************************************************************** */
		/* SEARCH FORMATTER                                                                                               */
		/* ************************************************************************************************************** */

		formatSearchCell: function (search)
		{
			var result =
				"<div class='cell-inner' id='" + search.Guid + "'>" +
					"<div class='cell-right'>" + this.formatSearchAction(search) + "</div>" +
					"<div class='cell-left'>" + this.formatSearchIcon(search) + "</div>" +
					"<div class='cell-main' title='" + search.Name + " (" + search.Results + ")'>" + search.Name + "</div>" +
					"</div>";
			return result;
		},

		/* ************************************************************************************************************** */
		/* BOT FORMATTER                                                                                                  */
		/* ************************************************************************************************************** */

		formatBotIcon: function (bot, skipOverlay)
		{
			var icon = "user";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "";

			if (!bot.Connected)
			{
				iconClass = "Aluminium1Dark";

				if (bot.HasNetworkProblems)
				{
					overlay = "attention-circle";
					overlayClass = "ScarletRedMiddle";
				}
			}
			else
			{
				switch (bot.State)
				{
					case 0:
						if (bot.InfoSlotCurrent > 0)
						{
							overlay = "ok-circle";
							overlayClass = "ChameleonMiddle";
							overlayStyle = "opacity: 0.6";
						}
						else if (bot.InfoSlotCurrent == 0 && bot.InfoSlotCurrent)
						{
							overlay = "cancel-circle";
							overlayClass = "OrangeMiddle";
						}
						break;

					case 1:
						iconClass = "SkyBlueDark";
						overlay = "down-circle";
						overlayClass = "SkyBlueMiddle";
						overlayStyle = "opacity: " + speed2Overlay(bot.Speed);
						break;

					case 2:
						overlay = "clock";
						overlayClass = "OrangeMiddle";
						break;
				}
			}

			if (bot.Active)
			{
				overlay = "spin";
				overlayClass = "ScarletRedMiddle animate-spin icon-small";
				overlayStyle = "";
			}

			return formatIcon(icon, iconClass, skipOverlay ? "" : overlay, skipOverlay ? "" : overlayClass, skipOverlay ? "" : overlayStyle);
		},

		formatBotName: function (bot)
		{
			var ret = bot.Name;
			if (bot.LastMessage != "")
			{
				ret += "<br /><small title='" + bot.LastMessage + "'><b>" + helper.date2Human(bot.LastMessageTime) + ":</b> " + bot.LastMessage + "</small>";
			}
			return ret;
		},

		formatBotSpeed: function (bot)
		{
			var ret = "";
			if (bot.InfoSpeedCurrent > 0)
			{
				ret += helper.speed2Human(bot.InfoSpeedCurrent);
			}
			if (bot.InfoSpeedCurrent > 0 && bot.InfoSpeedMax > 0)
			{
				ret += " / ";
			}
			if (bot.InfoSpeedMax > 0)
			{
				ret += helper.speed2Human(bot.InfoSpeedMax);
			}
			return ret;
		},

		formatBotSlots: function (bot)
		{
			var ret = "";
			ret += bot.InfoSlotCurrent;
			ret += " / ";
			ret += bot.InfoSlotTotal;
			return ret;
		},

		formatBotQueue: function (bot)
		{
			var ret = "";
			ret += bot.InfoQueueCurrent;
			ret += " / ";
			ret += bot.InfoQueueTotal;
			return ret;
		},

		/* ************************************************************************************************************** */
		/* PACKET FORMATTER                                                                                               */
		/* ************************************************************************************************************** */

		formatPacketIcon: function (packet, onclick, skipOverlay)
		{
			var icon = "doc";
			var iconClass = "Aluminium2Middle";
			var overlay = "";
			var overlayClass = "";
			var overlayStyle = "";

			var name = packet.Name;
			var ext = name.toLowerCase().substr(-3);
			if (ext == "avi" || ext == "wmv" || ext == "mkv" || ext == "mpg" || ext == "mov" || ext == "mp4")
			{
				icon = "video";
			}
			else if (ext == "mp3" || ext == "ogg" || ext == "wav")
			{
				icon = "headphones";
			}
			else if (ext == "rar" || ext == "tar" || ext == "zip")
			{
				icon = "briefcase";
			}

			if (!packet.Enabled)
			{
				iconClass = "Aluminium1Dark";
			}
			else if (!skipOverlay)
			{
				if (packet.Connected)
				{
					iconClass = "SkyBlueDark";
					overlay = "down-circle";
					overlayClass = "SkyBlueMiddle";
					overlayStyle = "opacity: " + speed2Overlay(packet.Speed);
				}
				else if (packet.Next)
				{
					overlay = "clock";
					overlayClass = "OrangeMiddle";
				}
				else
				{
					overlay = "clock";
					overlayClass = "ButterMiddle";
				}
			}

			if (packet.Active)
			{
				overlay = "spin";
				overlayClass = "ScarletRedMiddle animate-spin icon-small";
				overlayStyle = "";
			}

			return formatIcon(icon, iconClass, overlay, overlayClass, overlayStyle, onclick);
		},

		formatPacketId: function (packet)
		{
			return "#" + packet.Id;
		},

		formatPacketName: function (packet)
		{
			var name = packet.Name;

			if (name == undefined)
			{
				return "";
			}

			var ret = "<span title='" + name + "'>" + name + "</span>";

			if (packet.Connected)
			{
				var a = ((packet.StartSize) / packet.Size).toFixed(2) * 100;
				var b = ((packet.CurrentSize - packet.StartSize) / packet.Size).toFixed(2) * 100;
				var c = ((packet.StopSize - packet.CurrentSize) / packet.Size).toFixed(2) * 100;
				if (a + b + c > 100)
				{
					c = 100 - a - b;
				}

				ret += "<div class='progress progress-striped'>" +
					"<div style='width: " + a + "%' class=''></div>" +
					"<div style='width: " + b + "%' class='bar " + (packet.IsChecked ? "bar-success" : "bar-warning") + "'></div>" +
					"<div style='width: " + c + "%' class='bar " + (packet.IsChecked ? "bar-success" : "bar-warning") + " bar-light'></div>" +
					"</div>";
				//ret += "<progress max='" + packet.Size + "' value='" + packet.CurrentSize + "'></progress>";
			}

			return ret;
		},

		formatPacketSpeed: function (packet)
		{
			return helper.speed2Human(packet.Speed);
		},

		formatPacketSize: function (packet)
		{
			return helper.size2Human(packet.Size);
		},

		formatPacketTimeMissing: function (packet)
		{
			return helper.time2Human(packet.TimeMissing);
		},

		/* ************************************************************************************************************** */
		/* FILE FORMATTER                                                                                                 */
		/* ************************************************************************************************************** */

		formatFileIcon: function (file, onclick)
		{
			var icon = "doc";
			var iconClass = "Aluminium2Middle";

			var ext = file.Name.toLowerCase().substr(-3);
			if (ext == "avi" || ext == "wmv" || ext == "mkv" || ext == "mpg")
			{
				icon = "video";
			}
			else if (ext == "mp3")
			{
				icon = "headphones";
			}
			else if (ext == "rar" || ext == "tar" || ext == "zip")
			{
				icon = "briefcase";
			}

			return formatIcon(icon, iconClass, undefined, undefined, undefined, onclick);
		},

		formatFileName: function (file)
		{
			var ret = file.Name;

			var a = (file.CurrentSize / file.Size).toFixed(2) * 100;
			var b = 100 - a;

			ret += "<div class='progress progress-striped'>" +
				"<div style='width: " + a + "%' class='bar bar-success'></div>" +
				"<div style='width: " + b + "%' class='bar bar-success bar-light'></div>" +
				"</div>";
			//ret += "<progress max='" + file.Size + "' value='" + file.CurrentSize + "'></progress>";

			return ret;
		},

		formatFileSpeed: function (file)
		{
			return helper.speed2Human(file.Speed);
		},

		formatFileSize: function (file)
		{
			return helper.size2Human(file.Size);
		},

		formatFileTimeMissing: function (file)
		{
			return helper.time2Human(file.TimeMissing);
		},

		formatRemoveIcon: function (grid, obj)
		{
			return "<i class='icon-cancel-circle icon-overlay icon-overlay-middle ScarletRedMiddle button' onclick='Grid.removeObject(\"" + grid + "\", \"" + obj.Guid + "\");'></i>";
		},

		/* ************************************************************************************************************** */
		/* NOTIFICATION FORMATTER                                                                                         */
		/* ************************************************************************************************************** */

		formatNotificationIcon: function (notification)
		{
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
					icon = "doc";
					break;

				case Enum.NotificationType.PacketIncompleted:
				case Enum.NotificationType.PacketBroken:
				case Enum.NotificationType.PacketRequested:
				case Enum.NotificationType.PacketRemoved:
				case Enum.NotificationType.PacketCompleted:
					icon = "doc";
					break;

				case Enum.NotificationType.FileSizeMismatch:
				case Enum.NotificationType.FileBuildFailed:
				case Enum.NotificationType.FileCompleted:
					icon = "doc";
					break;
			}

			return formatIcon(icon, iconClass);
		},

		formatNotificationContent: function (notification)
		{
			var msg = translate._("Notification_" + notification.Type,
			[
				{ Name: "Name", Value: notification.ObjectName },
				{ Name: "ParentName", Value: notification.ParentName }
			]);
			return "<span title='" + msg + "'>" + msg +"</span>";
		},

		formatNotificationTime: function (notification)
		{
			return helper.date2Human(notification.Time);
		}
	};
	return self;
}());
