//
//  resize.js
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

var XGResize = Class.create(
{
	initialize: function ()
	{
		this.onResize = new Slick.Event();
	},

	start: function ()
	{
		var self = this;

		$("body").layout({
			onresize: function () {
				self.resizeMain(innerLayout);
			}
		});
		var innerLayout = $("#layoutObjectsContainer").layout({
			resizeWithWindow: false,
			onresize: function () {
				self.resizeContainer();
			}
		});

		// make bot and packet grid height almost equal
		innerLayout.sizePane("north", $('#layoutObjects').height() / 2 - 80);

		// resize after all is visible - twice, because the first run wont change all values :|
		this.resizeMain(innerLayout);
		setTimeout(function() { self.resizeMain(innerLayout); }, 1000);
	},

	/**
	 * @param innerLayout
	 */
	resizeMain: function (innerLayout)
	{
		var self = this;

		var searchLayout = $('#searchLayout');
		var objectLayout = $('#layoutObjects');

		/* left search tab */
		// set table
		$("#searchGrid")
			.height(searchLayout.height() - 28);
		// patch search input
		$("#searchText").width(searchLayout.width() - 8);

		/* main container */
		var subSize = 68;
		if ($.browser.mozilla)
		{
			subSize -= 2;
		}
		$("#layoutObjectsContainer").height(searchLayout.height() - subSize);
		// bots + packets table
		$("#botGrid, #packetGrid").width(objectLayout.width() - 1);

		/* other container */
		subSize = 110;
		if ($.browser.mozilla)
		{
			subSize += 1;
		}
		$("#externalGrid, #fileGrid")
			.width(objectLayout.width() - 1)
			.height(objectLayout.height() - subSize);

		/* dialog */
		var width = $(window).width() - 20;
		var height = $(window).height() - 20;
		// just resize if necessary
		if (width != this.width || height != this.height)
		{
			this.width = width;
			this.height = height;

			$("#dialogSnapshots").dialog("option", {
				width: width,
				height: height
			});
			$("#snapshot")
				.width(width - 240)
				.height(height - 40);
		}

		innerLayout.resizeAll();

		this.onResize.notify({}, null, self);
	},

	resizeContainer: function ()
	{
		$("#botGrid").height($('#botLayout').height());
		$("#packetGrid").height($('#packetLayout').height());

		this.onResize.notify({}, null, self);
	}
});
