// 
//  xg.helper.js
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

var XGHelper = Class.create(
{
	/**
	 * @param {int} size
	 * @return {String}
	 */
	size2Human: function (size)
	{
		if (size == 0)
		{
			return "&nbsp;";
		}
		if (size < 1024)
		{
			return size + " B";
		}
		else if (size < 1024 * 1024)
		{
			return (size / 1024).toFixed(0) + " K";
		}
		else if (size < 1024 * 1024 * 1024)
		{
			return (size / (1024 * 1024)).toFixed(0) + " M";
		}
		else if (size < 1024 * 1024 * 1024 * 1024)
		{
			return (size / (1024 * 1024 * 1024)).toFixed(0) + " G";
		}
		return (size / (1024 * 1024 * 1024 * 1024)).toFixed((size < 1024 * 1024 * 1024 * 1024 * 10) ? 1 : 0) + " T";
	},

	/**
	 * @param {int} speed
	 * @return {String}
	 */
	speed2Human: function (speed)
	{
		if (speed == 0)
		{
			return "&nbsp;";
		}
		if (speed < 1024)
		{
			return speed + " B";
		}
		else if (speed < 1024 * 1024)
		{
			return (speed > 100 * 1024 ? (speed / 1024).toFixed(1) : (speed / 1024).toFixed(2)) + " K";
		}
		return (speed > 100 * 1024 * 1024 ? (speed / (1024 * 1024)).toFixed(1) : (speed / (1024 * 1024)).toFixed(2)) + " M";
	},

	/**
	 * @param {int} time
	 * @return {String}
	 */
	time2Human: function (time)
	{
		var str = "";
		if(time < 0 || time >= 106751991167300 || time == "106751991167300") { return str; }
	
		var buff = 0;
	
		if (time > 86400)
		{
			buff = Math.floor(time / 86400);
			str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
	
			time -= buff * 86400;
		}
		else if (str != "") { str += "00:"; }
	
		if (time > 3600)
		{
			buff = Math.floor(time / 3600);
			str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
			time -= buff * 3600;
		}
		else if (str != "") { str += "00:"; }
	
		if (time > 60)
		{
			buff = Math.floor(time / 60);
			str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
			time -= buff * 60;
		}
		else if (str != "") { str += "00:"; }
	
		if (time > 0)
		{
			buff = time;
			str += (buff >= 10 ? "" + buff : "0" + buff);
		}
		else if (str != "") { str += "00"; }
		else str = "&nbsp;";
	
		return str;
	},

	/**
	 * @param {int} timestamp
	 * @return {String}
	 */
	timeStampToDate: function (timestamp)
	{
		var date = new Date(timestamp * 1000);
		date.setHours(date.getHours() - 2);
		return date;
	},

	/**
	 * @param {int} timestamp
	 * @return {String}
	 */
	timeStampToHuman: function (timestamp)
	{
		if (timestamp <= 0)
		{
			return "&nbsp;";
		}

		var date = this.timeStampToDate(timestamp);
		var diff = (((new Date()).getTime() - date.getTime()) / 1000);

		if (diff < 0)
		{
			return "now";
		}
		if (diff < 60)
		{
			diff = Math.floor(diff);
			return diff + " second" + (diff != 1 ? "s" : "") + " ago";
		}
		diff = diff / 60;
		if (diff < 60)
		{
			diff = Math.floor(diff);
			return diff + " minute" + (diff != 1 ? "s" : "") + " ago";
		}
		diff = diff / 60;
		if (diff < 24)
		{
			diff = Math.floor(diff);
			return diff + " hour" + (diff != 1 ? "s" : "") + " ago";
		}

		var hours = date.getHours();
		if (hours < 10)
		{
			hours = "0" + hours;
		}
		var minutes = date.getMinutes();
		if (minutes < 10)
		{
			minutes = "0" + minutes;
		}

		diff = diff / 24;
		if (diff < 2)
		{
			return "yesterday at " + hours + ":" + minutes + "";
		}
		if (diff < 7)
		{
			return LANG_WEEKDAY[date.getDay()] + " at " + hours + ":" + minutes + "";
		}

		return date.getDate() + ". " + LANG_MONTH[date.getMonth()] + " at " + hours + ":" + minutes + "";
	}
});
