//
//  cookie.js
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

var XGGui = (function ()
{
	var dataView;
	var searchForm = $("#searchForm, #searchButtons");
	var searches = $("#searches");
	var search = $("#search");
	var searchAdd = $("#searchAdd");
	var searchRemove = $("#searchRemove");
	var currentSearchGuid = undefined;
	var currentSlide = 0;

	function searchClick (element)
	{
		setCurrentSearchGuid(element.data("guid"));
		search.val(element.data("name"));

		self.onSearch.notify({Guid: currentSearchGuid, Name: element.data("name"), Grid: currentSlide == 1 ? Enum.Grid.Bot : Enum.Grid.ExternalSearch}, null, this);
	}

	function setCurrentSearchGuid (guid)
	{
		currentSearchGuid = guid;
		searchAdd.hide();
		searchRemove.show();
	}

	function unsetCurrentSearchGuid ()
	{
		currentSearchGuid = undefined;
		searchAdd.show();
		searchRemove.hide();
	}

	function addSearch (searchObj)
	{
		if (search.val() == searchObj.Name)
		{
			setCurrentSearchGuid(searchObj.Guid);
		}

		searches.append("<li><a href='#' data-guid='" + searchObj.Guid + "' data-name='" + searchObj.Name + "'><span class='badge badge-info pull-right'>" + searchObj.Results + "</span>" + searchObj.Name + "</a></li>");
		$("a[data-guid='" + searchObj.Guid + "']").click(
			function (e)
			{
				searchClick($(this));
			}
		);
	}

	function removeSearch (searchObj)
	{
		if (search.val() == searchObj.Name)
		{
			unsetCurrentSearchGuid();
		}
		// TODO remove click handler, too?
		var element = $("a[data-guid='" + searchObj.Guid + "']");
		element.parent().remove();
	}

	var self = {
		onSearch: new Slick.Event(),
		onSearchAdd: new Slick.Event(),
		onSearchRemove: new Slick.Event(),
		onSlide: new Slick.Event(),

		/**
		 * @param {XGDataView} dataView1
		 */
		initialize: function (dataView1)
		{
			dataView = dataView1;

			dataView.onAdd.subscribe(function (e, args)
			{
				if (args.DataType == Enum.Grid.Search)
				{
					addSearch(args.Data);
				}
			});

			dataView.onRemove.subscribe(function (e, args)
			{
				if (args.DataType == Enum.Grid.Search)
				{
					removeSearch(args.Data);
				}
			});

			searchAdd.click(
				function (e)
				{
					if (currentSearchGuid == undefined)
					{
						self.onSearchAdd.notify({Name: search.val()}, null, this);
					}
				}
			);

			searchRemove.click(
				function (e)
				{
					if (currentSearchGuid != undefined)
					{
						self.onSearchRemove.notify({Guid: currentSearchGuid}, null, this);
					}
				}
			);

			search.keyup(
				function (e)
				{
					e.preventDefault();
					unsetCurrentSearchGuid();

					var name = search.val();
					var element = $("a[data-name='" + name + "']");
					if (element.length)
					{
						setCurrentSearchGuid(element.data("name"));
					}

					if (e.which == 13)
					{
						self.onSearch.notify({Guid: currentSearchGuid, Name: name, Grid: currentSlide == 1 ? Enum.Grid.Bot : Enum.Grid.ExternalSearch}, null, this);
					}
				}
			);

			$("#settingsLink").click(
				function (e)
				{
					$("#settings").toggle();
				});

			$(".carousel-link").click(
				function (e)
				{
					$(".carousel-link").parent().removeClass("active");
					$(this).parent().addClass("active");

					currentSlide = $(this).data("slide");
					$("#mainCarousel").carousel(currentSlide);

					if (currentSlide == 1 || currentSlide == 2)
					{
						searchForm.show();
					}
					else
					{
						searchForm.hide();
					}
				});
			$("#mainCarousel").carousel({ interval: false });
			$("#mainCarousel").bind('slid',
				function (e)
				{
					self.onSlide.notify({}, null, this);
				});
		}
	};
	return self;
}());
