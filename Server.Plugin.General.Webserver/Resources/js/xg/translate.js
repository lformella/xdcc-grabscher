//
//  translate.js
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

var XGTranslate = (function ()
{
	var translations;

	function _ (string, replaces)
	{
		var translated = translations[string];
		translated = translated != "" && translated != undefined ? translated : string;
		if (replaces != undefined)
		{
			$.each(replaces, function (i, item)
			{
				translated = translated.replace("#" + item.Name + "#", item.Value);
			});
		}
		return translated;
	}

	return {
		initialize: function (translations1)
		{
			translations = translations1;

			$("h1, h2, h3, a, span, button, label, legend, p, input").each(function (i, element)
			{
				var item = $(element);

				var original = item.html();
				var translated = _(item.html());
				if (original != translated)
				{
					item.html(_(item.html()));
				}

				if (item.attr("title") != undefined && item.attr("title") != "")
				{
					item.attr("title", _(item.attr("title")));
				}
				if (item.attr("placeholder") != undefined && item.attr("placeholder") != "")
				{
					item.attr("placeholder", _(item.attr("placeholder")));
				}
				if (item.data("name") != undefined && item.data("name") != "")
				{
					item.data("name", _(item.data("name")));
				}
			});
		},

		_: function (text, replaces)
		{
			return _(text, replaces);
		}
	}
}());


