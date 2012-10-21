//  xg.url.js
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

var XGUrl = Class.create(
{
	initialize: function ()
	{
		this.password = "";
	},

	/**
	 * @param {String} password
	 * @return {String}
	 */
	jsonUrl: function (password)
	{
		return "/?password=" + (password != undefined && password != "" ? encodeURIComponent(password) : encodeURIComponent(this.password)) +
			"&offbots=" + ($("#show_offline_bots").attr('checked') ? "1" : "0" ) +
			"&request=";
	},

	/**
	 * @param {Integer} id
	 * @param {String} guid
	 * @return {String}
	 */
	guidUrl: function (id, guid)
	{
		return this.jsonUrl("") + id + "&guid=" + guid;
	},

	/**
	 * @param {Integer} id
	 * @param {String} name
	 * @return {String}
	 */
	nameUrl: function (id, name)
	{
		return this.jsonUrl("") + id + "&name=" + encodeURIComponent(name);
	}
});
