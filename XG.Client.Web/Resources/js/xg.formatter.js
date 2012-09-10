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
		var str = "Server";
		var overlay = "";
	
		if(!server.Enabled){ str += "Disabled"; }
		else if(server.Connected) { overlay = "OverActive"; }
		else if(server.ErrorCode != "" && server.ErrorCode != "None" && server.ErrorCode != "0") { overlay = "OverAttention"; }
	
		return this.formatIcon2(str, overlay, onclick) + " " + server.Name;
	},

	formatChannelIcon: function (channel, id)
	{
		var str = "Channel";
		var overlay = "";

		if(!channel.Enabled) { str += "Disabled"; }
		else if(channel.Connected) { overlay = "OverActive"; }
		else if(channel.ErrorCode != "" && channel.ErrorCode != "None" && channel.ErrorCode != "0") { overlay = "OverAttention"; }

		return this.formatIcon2(str, overlay, onclick) + " " + channel.Name;
	},

	/* ************************************************************************************************************** */
	/* SEARCH FORMATER                                                                                                */
	/* ************************************************************************************************************** */

	formatSearchIcon: function (cellvalue)
	{
		var str = "";
		switch(cellvalue)
		{
			case "1": str = "ODay"; break;
			case "2": str = "OWeek"; break;
			case "3": str = "Packet"; break;
			case "4": str = "Ok"; break;
			default: str = "Search"; break;
		}
		return this.formatIcon2(str);
	},

	/* ************************************************************************************************************** */
	/* BOT FORMATER                                                                                                   */
	/* ************************************************************************************************************** */

	formatBotIcon: function (bot)
	{
		var str = "Bot";
		var overlay = "";
	
		if(!bot.Connected) { str += "Off"; }
		else
		{
			switch(bot.State)
			{
				case 0:
					if(bot.InfoSlotCurrent > 0) overlay = "OverActive";
					else if(bot.InfoSlotCurrent == 0 && bot.InfoSlotCurrent) overlay = "OverDisabled";
					break;

				case 1:
					overlay = "Over" + this.speed2Image(bot.Speed);
					break;

				case 2:
					overlay = "OverWaiting";
					break;
			}
		}
	
		return this.formatIcon2(str, overlay);
	},

	formatBotName: function (bot)
	{
		var ret = bot.Name;
		if(bot.LastMessage != "")
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

	formatPacketIcon: function (packet, onclick)
	{	
		var str = "Packet";
		var overlay = "";
	
		if(!packet.Enabled) { str += "Disabled"; }
		else
		{
			if(packet.Connected) { overlay = "Over" + this.speed2Image(packet.Part != null ? packet.Part.Speed : 0); }
			else if (packet.Next) { overlay = "OverWaiting"; }
			else { overlay = "OverActive"; }
		}
	
		return this.formatIcon2(str, overlay, onclick);
	},

	formatPacketId: function (packet)
	{
		return "#" + packet.Id;
	},

	formatPacketName: function (packet)
	{
		var name = packet.RealName != undefined && packet.RealName != "" ? packet.RealName : packet.Name;

		if(name == undefined)
		{
			return "";
		}

		var ext = name.toLowerCase().substr(-3);
		var ret = "";
		if(ext == "avi" || ext == "wmv" || ext == "mkv")
		{
			ret += this.formatIcon("ExtVideo") + "&nbsp;&nbsp;";
		}
		else if(ext == "mp3")
		{
			ret += this.formatIcon("ExtAudio") + "&nbsp;&nbsp;";
		}
		else if(ext == "rar" || ext == "tar" || ext == "zip")
		{
			ret += this.formatIcon("ExtCompressed") + "&nbsp;&nbsp;";
		}
		else
		{
			ret += this.formatIcon("ExtDefault") + "&nbsp;&nbsp;";
		}
	
		if(name.toLowerCase().indexOf("german") > -1)
		{
			ret += this.formatIcon("LanguageDe") + "&nbsp;&nbsp;";
		}
	
		ret += name;

		if(packet.Connected && packet.Part != null)
		{
			ret += "<br />";
	
			var a = ((packet.Part.StartSize) / packet.RealSize).toFixed(2) * 100;
			var b = ((packet.Part.CurrentSize - packet.Part.StartSize) / packet.RealSize).toFixed(2) * 100;
			var c = ((packet.Part.StopSize - packet.Part.CurrentSize) / packet.RealSize).toFixed(2) * 100;
			if(a + b + c > 100)
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
	/* IMAGE FORMATER                                                                                                 */
	/* ************************************************************************************************************** */

	speed2Image: function (speed)
	{
		if (speed < 1024 * 150) { return "DL0"; }
		else if (speed < 1024 * 300) { return "DL1"; }
		else if (speed < 1024 * 450) { return "DL2"; }
		else if (speed < 1024 * 600) { return "DL3"; }
		else if (speed < 1024 * 750) { return "DL4"; }
		else if (speed < 1024 * 900) { return "DL5"; }
		else if (speed < 1024 * 1050) { return "DL6"; }
		else if (speed < 1024 * 1200) { return "DL7"; }
		else if (speed < 1024 * 1350) { return "DL8"; }
		else { return "DL9"; }
	},

	formatIcon: function (img)
	{
		return "<img src='image&" + img + "' />";
	},

	formatIcon2: function (img, overlay, onclick)
	{
		var str = "<div style='background-image:url(image&" + img + ");width:22px;height:22px;float:left;margin:0 2px;'";
		if(onclick != undefined && onclick != "")
		{
			str += " class='button' onclick='" + onclick + "'";
		}
		str += ">";
		if(overlay != undefined && overlay != "")
		{
			str += "<div style='background-image:url(image&" + overlay + ");' class='overlay'></div>";
		}
		str += "</div>";
		
		return str;
	}
});
