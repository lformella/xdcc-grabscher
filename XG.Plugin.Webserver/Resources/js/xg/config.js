//
//  config.js
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

define(function()
{
	var salt = '#SALT#';
	var host = '#HOST#';
	var port = '#PORT#';
	var language = '#LANGUAGE_SHORT#';
	var password = '';
	var combineBotAndPacketGrid = false;
	var humanDates = true;
	var showOfflineBots = true;

	var self = {
		onUpdateCombineBotAndPacketGrid: new Slick.Event(),
		onUpdateHumanDates: new Slick.Event(),
		onUpdateShowOfflineBots: new Slick.Event(),

		getSalt: function ()
		{
			return salt;
		},

		getHost: function ()
		{
			return host;
		},

		getPort: function ()
		{
			return port;
		},

		getLanguage: function ()
		{
			return language;
		},

		getPassword: function ()
		{
			return password;
		},

		setPassword: function (value)
		{
			password = value;
		},

		getCombineBotAndPacketGrid: function ()
		{
			return combineBotAndPacketGrid;
		},

		setCombineBotAndPacketGrid: function (enable)
		{
			combineBotAndPacketGrid = enable;
			self.onUpdateCombineBotAndPacketGrid.notify({ Enable: enable }, null, this);
		},

		getHumanDates: function ()
		{
			return humanDates;
		},

		setHumanDates: function (enable)
		{
			humanDates = enable;
			self.onUpdateHumanDates.notify({ Enable: enable }, null, this);
		},

		getShowOfflineBots: function ()
		{
			return showOfflineBots;
		},

		setShowOfflineBots: function (enable)
		{
			showOfflineBots = enable;
			self.onUpdateShowOfflineBots.notify({ Enable: enable }, null, this);
		}
	};

	return self;
});
