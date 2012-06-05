//
// Copyright (C) 2012 Lars Formella <ich@larsformella.de>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

var XGFormatter = Class.create(
{
	/* ************************************************************************************************************** */
	/* SERVER FORMATER                                                                                                */
	/* ************************************************************************************************************** */

	formatServerIcon: function (server)
	{
		var str = "Server";
	
		if(server.Enabled == "false") { str += "Disabled"; }
		else if(server.Connected == "true") { str += "Connected"; }
	
		return this.formatIcon2(str) + " " + server.Name;
	},

	formatChannelIcon: function (channel)
	{
		var str = "Channel";

		if(channel.Enabled == "false") { str += "Disabled"; }
		else if(channel.Connected == "true") { str += "Connected"; }

		return this.formatIcon2(str) + " " + channel.Name;
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
			case "3": str = "BotDL0"; break;
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
	
		if(bot.Connected == "false") { str += "Off"; }
		else
		{
			switch(bot.BotState)
			{
				case "Idle":
					if(bot.InfoSpeedCurrent > 0)
					{
						if(bot.InfoSlotCurrent > 0) str += "Free";
						else if(bot.InfoSlotCurrent == 0) str += "Full";
					}
					break;

				case "Active":
					str += this.speed2Image(bot.InfoSpeed);
					break;

				case "Waiting":
					str += "Queued";
					break;
			}
		}
	
		return this.formatIcon2(str);
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

	formatPacketIcon: function (packet)
	{	
		var str = "Packet";
	
		if(packet.Enabled == "false") { str += "Disabled"; }
		else
		{
			if(packet.Connected == "true") { str += this.speed2Image(packet.Speed); }
			else if (packet.Order == "true") { str += "Queued"; }
			else { str += "New"; }
		}
	
		return this.formatIcon2(str);
	},

	formatPacketId: function (packet)
	{
		return "#" + packet.Id;
	},

	formatPacketName: function (packet)
	{
		var ext = packet.Name.toLowerCase().substr(-3);
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
	
		if(packet.Name.toLowerCase().indexOf("german") > -1)
		{
			ret += this.formatIcon("LanguageDe") + "&nbsp;&nbsp;";
		}
	
		ret += packet.Name;
	
		if(packet.Connected == "true")
		{
			ret += "<br />";
	
			var a = ((packet.StartSize) / packet.Size).toFixed(2) * 100;
			var b = ((packet.CurrentSize - packet.StartSize) / packet.Size).toFixed(2) * 100;
			var c = ((packet.StopSize - packet.CurrentSize) / packet.Size).toFixed(2) * 100;
			if(a + b + c > 100)
			{
				c = 100 - a - b;
			}
			// Enum.TangoColor.SkyBlue.Middle
			ret += "<div role='progressbar' class='ui-progressbar ui-widget ui-widget-content ui-corner-all' style='height:3px'>" +
				"<div style='width: " + a + "%;float:left' class='ui-progressbar-value ui-corner-left'></div>" +
				"<div style='width: " + b + "%;float:left;background:#" + (packet.IsChecked == "true" ? Enum.TangoColor.SkyBlue.Dark : Enum.TangoColor.Plum.Dark) + "' class='ui-progressbar-value ui-corner-left ui-widget-header'></div>" +
				"<div style='width: " + c + "%;float:left;background:#" + (packet.IsChecked == "true" ? Enum.TangoColor.SkyBlue.Light : Enum.TangoColor.Plum.Light) + "' class='ui-progressbar-value ui-corner-left ui-widget-header'></div>" +
				"</div><div class='clear'></div>";
		}
	
		return ret;
	},

	/* ************************************************************************************************************** */
	/* IMAGE FORMATER                                                                                                 */
	/* ************************************************************************************************************** */

	speed2Image: function (speed)
	{
		if (speed < 1024 * 125) { return "DL0"; }
		else if (speed < 1024 * 250) { return "DL1"; }
		else if (speed < 1024 * 500) { return "DL2"; }
		else if (speed < 1024 * 750) { return "DL3"; }
		else if (speed < 1024 * 1000) { return "DL4"; }
		else { return "DL5"; }
	},

	formatIcon: function (img)
	{
		return "<img src='image&" + img + "' />";
	},

	formatIcon2: function (img)
	{
		return "<div style='background-image:url(image&" + img + ");width:22px;height:22px;float:left;'></div>";
	}
});
