//
//  formatter.js
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

var XGFormatter = Class.create(
{
	initialize: function (helper)
	{
		this.helper = helper;
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
			overlay = "ok-circle2";
			overlayClass = "ChameleonMiddle";
		}
		else if (server.ErrorCode != "" && server.ErrorCode != "None" && server.ErrorCode != "0")
		{
			overlay = "attention-circle";
			overlayClass = "ScarletRedMiddle";
		}

		return this.formatIcon2(icon, iconClass, overlay, overlayClass, overlayStyle, onclick);
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
			overlay = "ok-circle2";
			overlayClass = "ChameleonMiddle";
		}
		else if (channel.ErrorCode != "" && channel.ErrorCode != "None" && channel.ErrorCode != "0")
		{
			overlay = "attention-circle";
			overlayClass = "ScarletRedMiddle";
		}

		return this.formatIcon2(icon, iconClass, overlay, overlayClass, overlayStyle, onclick);
	},
	
	formatServerChannelName: function (obj)
	{
		var str = obj.Name;
		if (obj.ErrorCode != "" && obj.ErrorCode != "None" && obj.ErrorCode != "0")
		{
			str += "<br /><small>" + _("Error") + ": " + obj.ErrorCode + "</small>";
		}
		return str;
	},

	/* ************************************************************************************************************** */
	/* SEARCH FORMATTER                                                                                               */
	/* ************************************************************************************************************** */

	formatSearchIcon: function (search)
	{
		var icon = "search";
		var iconClass = "Aluminium2Middle";

		switch (search.Guid)
		{
			case "00000000-0000-0000-0000-000000000001":
				icon = "clock";
				iconClass = "OrangeMiddle";
				break;

			case "00000000-0000-0000-0000-000000000002":
				icon = "clock";
				iconClass = "ButterMiddle";
				break;

			case "00000000-0000-0000-0000-000000000003":
				icon = "down-circle2";
				iconClass = "SkyBlueMiddle";
				break;

			case "00000000-0000-0000-0000-000000000004":
				icon = "ok-circle2";
				iconClass = "ChameleonMiddle";
				break;
		}

		return this.formatIcon2(icon, iconClass);
	},

	formatSearchAction: function (search)
	{
		var result = "";

		switch (search.Guid)
		{
			case "00000000-0000-0000-0000-000000000001":
			case "00000000-0000-0000-0000-000000000002":
			case "00000000-0000-0000-0000-000000000003":
			case "00000000-0000-0000-0000-000000000004":
				break;

			default:
				result = "<i class='icon-cancel-circle2 icon-overlay ScarletRedMiddle button' onclick='XG.removeSearch(\"" + search.Guid + "\");'></i>";
				break;
		}

		return result;
	},

	/* ************************************************************************************************************** */
	/* BOT FORMATTER                                                                                                  */
	/* ************************************************************************************************************** */

	formatBotIcon: function (bot)
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
						overlay = "ok-circle2";
						overlayClass = "ChameleonMiddle";
						overlayStyle = "opacity: 0.6";
					}
					else if (bot.InfoSlotCurrent == 0 && bot.InfoSlotCurrent)
					{
						overlay = "cancel-circle2";
						overlayClass = "OrangeMiddle";
					}
					break;

				case 1:
					iconClass = "SkyBlueDark";
					overlay = "down-circle2";
					overlayClass = "SkyBlueMiddle";
					overlayStyle = "opacity: " + this.speed2Overlay(bot.Speed);
					break;

				case 2:
					overlay = "clock";
					overlayClass = "OrangeMiddle";
					break;
			}
		}

		return this.formatIcon2(icon, iconClass, overlay, overlayClass, overlayStyle);
	},

	formatBotName: function (bot)
	{
		var ret = bot.Name;
		if (bot.LastMessage != "")
		{
			ret += "<br /><small><b>" + this.helper.date2Human(bot.LastMessageTime) + ":</b> " + bot.LastMessage + "</small>";
		}
		return ret;
	},

	formatBotSpeed: function (bot)
	{
		var ret = "";
		if (bot.InfoSpeedCurrent > 0)
		{
			ret += this.helper.speed2Human(bot.InfoSpeedCurrent);
		}
		if (bot.InfoSpeedCurrent > 0 && bot.InfoSpeedMax > 0)
		{
			ret += " / ";
		}
		if (bot.InfoSpeedMax > 0)
		{
			ret += this.helper.speed2Human(bot.InfoSpeedMax);
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

		var name = packet.RealName != undefined && packet.RealName != "" ? packet.RealName : packet.Name;
		var ext = name.toLowerCase().substr(-3);
		if (ext == "avi" || ext == "wmv" || ext == "mkv" || ext == "mpg")
		{
			icon = "video-1";
		}
		else if (ext == "mp3")
		{
			icon = "headphones";
		}
		else if (ext == "rar" || ext == "tar" || ext == "zip")
		{
			icon = "th";
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
				overlay = "down-circle2";
				overlayClass = "SkyBlueMiddle";
				overlayStyle = "opacity: " + this.speed2Overlay(packet.Part != null ? packet.Part.Speed : 0);
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

		return this.formatIcon2(icon, iconClass, overlay, overlayClass, overlayStyle, onclick);
	},

	formatPacketId: function (packet)
	{
		return "#" + packet.Id;
	},

	formatPacketName: function (packet)
	{
		var name = packet.RealName != undefined && packet.RealName != "" ? packet.RealName : packet.Name;

		if (name == undefined)
		{
			return "";
		}

		var ret = name;

		if (packet.Connected && packet.Part != null)
		{
			ret += "<progress max='" + packet.RealSize + "' value='" + packet.Part.CurrentSize + "'></progress>";
		}

		return ret;
	},

	formatPacketSpeed: function (packet)
	{
		return this.helper.speed2Human(packet.Part != null ? packet.Part.Speed : 0);
	},

	formatPacketSize: function (packet)
	{
		return this.helper.size2Human(packet.RealSize > 0 ? packet.RealSize : packet.Size);
	},

	formatPacketTimeMissing: function (packet)
	{
		return this.helper.time2Human(packet.Part != null ? packet.Part.TimeMissing : 0);
	},

	/* ************************************************************************************************************** */
	/* FILE FORMATTER                                                                                                 */
	/* ************************************************************************************************************** */

	formatFileIcon: function (file)
	{
		var icon = "doc";
		var iconClass = "Aluminium2Middle";

		var ext = file.Name.toLowerCase().substr(-3);
		if (ext == "avi" || ext == "wmv" || ext == "mkv" || ext == "mpg")
		{
			icon = "video-1";
		}
		else if (ext == "mp3")
		{
			icon = "headphones";
		}
		else if (ext == "rar" || ext == "tar" || ext == "zip")
		{
			icon = "th";
		}

		return this.formatIcon2(icon, iconClass);
	},

	formatFileName: function (file)
	{
		var ret = file.Name;

		ret += "<progress max='" + file.Size + "' value='" + file.CurrentSize + "'></progress>";

		return ret;
	},

	formatFileSpeed: function (file)
	{
		return this.helper.speed2Human(file.Speed);
	},

	formatFileSize: function (file)
	{
		return this.helper.size2Human(file.Size);
	},

	formatFileTimeMissing: function (file)
	{
		return this.helper.time2Human(file.TimeMissing);
	},

	/* ************************************************************************************************************** */
	/* IMAGE FORMATTER                                                                                                */
	/* ************************************************************************************************************** */

	speed2Overlay: function (speed)
	{
		var opacity = (speed / (1024 * 1500)) * 0.4;
		return (opacity > 0.4 ? 0.4 : opacity) + 0.6;
	},

	formatIcon: function (icon, iconClass)
	{
		iconClass = "icon-medium icon-" + icon + " " + iconClass;
		return "<i class='" + iconClass + "'></i>";
	},

	formatIcon2: function (icon, iconClass, overlay, overlayClass, overlayStyle, onclick)
	{
		iconClass = "icon-big icon-" + icon + " " + iconClass;
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
});
