// 
//  xg.formatter.js
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
	/* ************************************************************************************************************** */
	/* SERVER FORMATER                                                                                                */
	/* ************************************************************************************************************** */

	formatServerIcon: function (server, onclick)
	{
		var icon = "book";
		var iconClass = "Aluminium2Middle";
		var overlay = "";
		var overlayClass = "";

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
			overlay = "attention";
			overlayClass = "ScarletRedMiddle";
		}

		return this.formatIcon2(icon, iconClass, overlay, overlayClass, onclick);
	},

	formatChannelIcon: function (channel, id)
	{
		var icon = "folder";
		var iconClass = "Aluminium2Middle";
		var overlay = "";
		var overlayClass = "";

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
			overlay = "attention";
			overlayClass = "ScarletRedMiddle";
		}

		return this.formatIcon2(icon, iconClass, overlay, overlayClass);
	},

	/* ************************************************************************************************************** */
	/* SEARCH FORMATER                                                                                                */
	/* ************************************************************************************************************** */

	formatSearchIcon: function (cellvalue)
	{
		var icon = "search";
		var iconClass = "Aluminium2Middle";

		switch (cellvalue)
		{
			case "1":
				icon = "clock";
				iconClass = "OrangeMiddle";
				break;

			case "2":
				icon = "clock";
				iconClass = "ButterMiddle";
				break;

			case "3":
				icon = "down-circle2";
				iconClass = "SkyBlueMiddle";
				break;

			case "4":
				icon = "ok-circle2";
				iconClass = "ChameleonMiddle";
				break;
		}
		return this.formatIcon2(icon, iconClass);
	},

	/* ************************************************************************************************************** */
	/* BOT FORMATER                                                                                                   */
	/* ************************************************************************************************************** */

	formatBotIcon: function (bot)
	{
		var icon = "user";
		var iconClass = "Aluminium2Middle";
		var overlay = "";
		var overlayClass = "";

		if (!bot.Connected)
		{
			iconClass = "Aluminium1Dark";
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
					overlayClass = this.speed2Image(bot.Speed);
					break;

				case 2:
					overlay = "clock";
					break;
			}
		}

		return this.formatIcon2(icon, iconClass, overlay, overlayClass);
	},

	formatBotName: function (bot)
	{
		var ret = bot.Name;
		if (bot.LastMessage != "")
		{
			ret += "<br /><small><b>" + bot.LastContact + ":</b> " + bot.LastMessage + "</small>";
		}
		return ret;
	},

	formatBotSpeed: function (bot)
	{
		var ret = "";
		if (bot.InfoSpeedCurrent > 0)
		{
			ret += Helper.speed2Human(bot.InfoSpeedCurrent);
		}
		if (bot.InfoSpeedCurrent > 0 && bot.InfoSpeedMax > 0)
		{
			ret += " / ";
		}
		if (bot.InfoSpeedMax > 0)
		{
			ret += Helper.speed2Human(bot.InfoSpeedMax);
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
	/* PACKET FORMATER                                                                                                */
	/* ************************************************************************************************************** */

	formatPacketIcon: function (packet, onclick, skipOverlay)
	{
		var icon = "gift";
		var iconClass = "Aluminium2Middle";
		var overlay = "";
		var overlayClass = "";

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
				overlayClass = this.speed2Image(packet.Part != null ? packet.Part.Speed : 0);
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

		return this.formatIcon2(icon, iconClass, overlay, overlayClass, onclick);
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

		var icon = "doc";
		var iconClass = "Aluminium2Middle";

		var ext = name.toLowerCase().substr(-3);
		var ret = "";
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
		ret += this.formatIcon(icon, iconClass) + "&nbsp;&nbsp;" + name;

		if (packet.Connected && packet.Part != null)
		{
			ret += "<br />";

			var a = ((packet.Part.StartSize) / packet.RealSize).toFixed(2) * 100;
			var b = ((packet.Part.CurrentSize - packet.Part.StartSize) / packet.RealSize).toFixed(2) * 100;
			var c = ((packet.Part.StopSize - packet.Part.CurrentSize) / packet.RealSize).toFixed(2) * 100;
			if (a + b + c > 100)
			{
				c = 100 - a - b;
			}
			// Enum.TangoColor.SkyBlue.Middle
			ret += "<div role='progressbar' class='ui-progressbar ui-widget ui-corner-all' style='height:2px'>" +
				"<div style='width: " + a + "%;float:left' class='ui-progressbar-value ui-corner-left'></div>" +
				"<div style='width: " + b + "%;float:left;background:#" + (packet.Part.Checked ? Enum.TangoColor.SkyBlue.Dark : Enum.TangoColor.Plum.Dark) + "' class='ui-progressbar-value ui-corner-left ui-widget-header'></div>" +
				"<div style='width: " + c + "%;float:left;background:#" + (packet.Part.Checked ? Enum.TangoColor.SkyBlue.Light : Enum.TangoColor.Plum.Light) + "' class='ui-progressbar-value ui-corner-left ui-widget-header'></div>" +
				"</div><div class='clear'></div>";
		}

		return ret;
	},

	formatPacketSpeed: function (packet)
	{
		return Helper.speed2Human(packet.Part != null ? packet.Part.Speed : 0);
	},

	formatPacketSize: function (packet)
	{
		return Helper.size2Human(packet.RealSize > 0 ? packet.RealSize : packet.Size);
	},

	formatPacketTimeMissing: function (packet)
	{
		return Helper.time2Human(packet.Part != null ? packet.Part.TimeMissing : 0);
	},

	/* ************************************************************************************************************** */
	/* FILE FORMATER                                                                                                  */
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

		ret += "<br /><div role='progressbar' class='ui-progressbar ui-widget ui-corner-all' style='height:4px'>";

		$.each(file.Parts, function (i, part)
		{
			var b = ((part.CurrentSize - part.StartSize) / file.Size).toFixed(2) * 100;
			var c = ((part.StopSize - part.CurrentSize) / file.Size).toFixed(2) * 100;

			ret += "<div style='width: " + b + "%;float:left;background:#" + (part.Checked ? Enum.TangoColor.SkyBlue.Dark : Enum.TangoColor.Plum.Dark) + "' class='ui-progressbar-value ui-corner-left ui-widget-header'></div>";
			ret += "<div style='width: " + c + "%;float:left;background:#" + (part.Checked ? Enum.TangoColor.SkyBlue.Light : Enum.TangoColor.Plum.Light) + "' class='ui-progressbar-value ui-corner-left ui-widget-header'></div>";
		});

		ret += "</div><div class='clear'></div>";

		return ret;
	},

	formatFileSpeed: function (file)
	{
		var speed = 0;
		$.each(file.Parts, function (i, part)
		{
			speed += part.Speed;
		});
		return Helper.speed2Human(speed);
	},

	formatFileSize: function (file)
	{
		return Helper.size2Human(file.Size);
	},

	formatFileTimeMissing: function (file)
	{
		var time = 0;
		$.each(file.Parts, function (i, part)
		{
			time = time == 0 ? part.TimeMissing : (time < part.TimeMissing ? time : part.TimeMissing);
		});
		return Helper.time2Human(time);
	},

	/* ************************************************************************************************************** */
	/* IMAGE FORMATER                                                                                                 */
	/* ************************************************************************************************************** */

	speed2Image: function (speed)
	{
		var overlayClass = "SkyBlue";

		if (speed < 1024 * 150)
		{
			overlayClass += "1";
		}
		else if (speed < 1024 * 300)
		{
			overlayClass += "2";
		}
		else if (speed < 1024 * 450)
		{
			overlayClass += "3";
		}
		else if (speed < 1024 * 600)
		{
			overlayClass += "4";
		}
		else if (speed < 1024 * 750)
		{
			overlayClass += "5";
		}
		else if (speed < 1024 * 900)
		{
			overlayClass += "6";
		}
		else if (speed < 1024 * 1050)
		{
			overlayClass += "7";
		}
		else if (speed < 1024 * 1200)
		{
			overlayClass += "8";
		}
		else
		{
			overlayClass += "9";
		}

		return overlayClass;
	},

	formatIcon: function (icon, iconClass)
	{
		iconClass = "icon-medium icon-" + icon + " " + iconClass;
		return "<i class='" + iconClass + "'></i>";
	},

	formatIcon2: function (icon, iconClass, overlay, overlayClass, onclick)
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
			str += "<i class='" + overlayClass + "'></i>";
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
