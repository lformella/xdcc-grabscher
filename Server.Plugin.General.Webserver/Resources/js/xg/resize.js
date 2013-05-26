//
//  resize.js
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

var XGResize = (function ()
{
	var width, height;
	var combineBotAndPacketGrid;

	function resize (force)
	{
		/* dialog */
		var width1 = $(window).width() - 20;
		var height1 = $(window).height() - 60;

		// just resize if necessary
		if (force || width1 != width || height1 != height)
		{
			width = width1;
			height = height1;

			$("#" + Enum.Grid.Bot + "Grid, #" + Enum.Grid.Packet + "Grid, #" + Enum.Grid.ExternalSearch + "Grid, #" + Enum.Grid.File + "Grid").width(width);
			var botHeight = height * 0.4 - 10;
			$("#" + Enum.Grid.Bot + "Grid").height(botHeight);
			if (combineBotAndPacketGrid)
			{
				botHeight = -10;
			}
			$("#" + Enum.Grid.Packet + "Grid").height(height - botHeight - 20);
			$("#" + Enum.Grid.ExternalSearch + "Grid, #" + Enum.Grid.File + "Grid").height(height - 10);

			$("#searchForm .dropdown-menu").css("max-height", (height - 20) + "px");

			$("#snapshot")
				.width(width - 240)
				.height(height - 40);
		}

		self.onResize.notify({}, null, self);
	}

	function resizeContainer ()
	{
		self.onResize.notify({}, null, self);
	}

	var self = {
		onResize: new Slick.Event(),

		/**
		 * @param {Boolean} combineBotAndPacketGrid1
		 */
		initialize: function (combineBotAndPacketGrid1)
		{
			combineBotAndPacketGrid = combineBotAndPacketGrid1;
		},

		start: function ()
		{
			$(window).resize(function ()
			{
				resize();
			});

			// resize after all is visible - twice, because the first run wont change all values :|
			resize();
			setTimeout(function ()
			{
				resize();
			}, 1000);
		},
		setCombineBotAndPacketGrid: function (enable)
		{
			combineBotAndPacketGrid = enable;
			resize(true);
		}
	};
	return self;
}());
