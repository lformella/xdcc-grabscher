//  xg.resize.js
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
	/**
	 * @param innerLayout
	 */
	resizeMain: function (innerLayout)
	{
		var searchLayout = $('#search_layout');
		var objectLayout = $('#layout_objects');

		/* left search tab */
		// set table
		$("#search_table")
			.setGridWidth(searchLayout.width() - 10)
			.setGridHeight(searchLayout.height() - 28);
		// patching table
		$($("#search_table .jqgfirstrow td")[2]).width("");
		// patch search input
		$("#search-text").width(searchLayout.width() - 10);
		// patch divs
		$("#search_layout div").width("");

		/* main container */
		var subSize = 68;
		if ($.browser.mozilla)
		{
			subSize -= 2;
		}
		$("#layout_objects_container").height(searchLayout.height() - subSize);
		// bots + packets table
		$("#bots_table, #packets_table").setGridWidth(objectLayout.width() - 1);
		// patching table
		$($("#bots_table .jqgfirstrow td")[2]).width("");
		$("#bots_table_Name").width("");
		$($("#packets_table .jqgfirstrow td")[3]).width("");
		$("#packets_table_Name").width("");
		// patch divs
		$("#layout_objects_container div").width("");

		/* other container */
		subSize = 110;
		if ($.browser.mozilla)
		{
			subSize += 1;
		}
		$("#searches_xg_bitpir_at, #files_table")
			.setGridWidth(objectLayout.width() - 1)
			.setGridHeight(objectLayout.height() - subSize);
		$($("#searches_xg_bitpir_at .jqgfirstrow td")[3]).width("");
		$("#searches_xg_bitpir_at_Name").width("");
		$($("#files_table .jqgfirstrow td")[2]).width("");
		$("#files_table_Name").width("");

		innerLayout.resizeAll();
	},

	resizeContainer: function ()
	{
		var subSize = 74;
		if ($.browser.msie)
		{
			subSize -= 3;
		}
		$("#bots_table").setGridHeight($('#bots_layout').height() - subSize);
		$("#packets_table").setGridHeight($('#packets_layout').height() - subSize);
	}
});
