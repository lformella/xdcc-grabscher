//
//  gui.js
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
	var searchLoading = $("#searchLoading");
	var errorDialog = $("#errorDialog");
	var xdccDialog = $("#xdccDialog");
	var currentSearchGuid = undefined;
	var currentSlide = 0;
	var showOfflineBots, humanDates, combineBotAndPacketGrid;

	function searchClick (element)
	{
		setCurrentSearchGuid(element.data("guid"));
		search.val(element.data("name"));

		notifyOnSearch(currentSearchGuid, element.data("name"));
	}

	function notifyOnSearch (guid, name)
	{
		self.onSearch.notify({Guid: guid, Name: name, Grid: currentSlide == 1 ? Enum.Grid.Bot : Enum.Grid.ExternalSearch}, null, this);
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

	function initializeDataView ()
	{
		dataView.onAdd.subscribe(function (e, args)
		{
			if (args.DataType == Enum.Grid.Search)
			{
				if (search.val() == args.Data.Name)
				{
					setCurrentSearchGuid(args.Data.Guid);
				}

				searches.append("<li><a href='#' data-guid='" + args.Data.Guid + "' data-name='" + args.Data.Name + "'>" +
					"<span class='badge badge-info pull-right'>" + args.Data.Results + "</span>" +
					"<span class='text' title='" + args.Data.Name + "'>" + args.Data.Name + "</span>" +
					"</a></li>");
				$("a[data-guid='" + args.Data.Guid + "']").click(
					function (e)
					{
						searchClick($(this));
					}
				);
			}
		});

		dataView.onRemove.subscribe(function (e, args)
		{
			if (args.DataType == Enum.Grid.Search)
			{
				if (search.val() == args.Data.Name)
				{
					unsetCurrentSearchGuid();
				}
				// TODO remove click handler, too?
				var element = $("a[data-guid='" + args.Data.Guid + "']");
				element.parent().remove();
			}
		});
	}

	function initializeSearch ()
	{
		$("a[data-guid='00000000-0000-0000-0000-000000000001'], a[data-guid='00000000-0000-0000-0000-000000000002']").click(
			function ()
			{
				searchClick($(this));
			}
		);

		searchAdd.click(
			function ()
			{
				if (currentSearchGuid == undefined)
				{
					self.onSearchAdd.notify({Name: search.val()}, null, this);
				}
			}
		);

		searchRemove.click(
			function ()
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
					setCurrentSearchGuid(element.data("guid"));
				}

				if (e.which == 13)
				{
					notifyOnSearch(currentSearchGuid != undefined ? currentSearchGuid : "00000000-0000-0000-0000-000000000000", name);
				}
			}
		);
	}

	function initializeSettings ()
	{
		$("#settingsLink").click(
			function ()
			{
				$("#settings").toggle("blind", 500);
			});
	}

	function initializeCarousel ()
	{
		$(".carousel-link").click(
			function ()
			{
				$(".carousel-link").parent().removeClass("active");
				$(this).parent().addClass("active");

				currentSlide = $(this).data("slide");
				$("#mainCarousel").carousel(currentSlide);

				if (currentSlide == 1 || currentSlide == 2)
				{
					searchForm.show("drop", 500);
				}
				else
				{
					searchForm.hide("drop", 500);
				}
			});
		$("#mainCarousel").carousel({ interval: false });
		$("#mainCarousel").bind('slid',
			function ()
			{
				self.onSlide.notify({}, null, this);
			});
	}

	function addServer ()
	{
		var tbox = $("#server");
		if (tbox.val() != "")
		{
			self.onAddServer.notify({Name: tbox.val()}, null, this);
			tbox.val("");
		}
	}

	function addChannel ()
	{
		var tbox = $("#channel");
		if (tbox.val() != "")
		{
			self.onAddChannel.notify({Name: tbox.val()}, null, this);
			tbox.val("");
		}
	}

	function addXdccLink ()
	{
		var tbox = $("#xdccLink");
		if (tbox.val() != "")
		{
			self.onAddXdccLink.notify({Name: tbox.val()}, null, this);
			tbox.val("");
			xdccDialog.modal("hide");
		}
	}

	function connectButtons ()
	{
		var element;

		$("#serverChannelButton").click(function ()
		{
			$("#settings").toggle("blind", 500);
			$("#serverChannelsDialog").modal('show');
		});

		$("#serverButton").click(function ()
		{
			addServer();
		});
		$("#server").keyup(function (e)
		{
			if (e.which == 13)
			{
				e.preventDefault();
				addServer();
			}
		});

		$("#channelButton").click(function ()
		{
			addChannel();
		});
		$("#channel").click(function (e)
		{
			if (e.which == 13)
			{
				e.preventDefault();
				addChannel();
			}
		});

		$("#xdccButton").click(function ()
		{
			addXdccLink();
		});
		$("#xdccLink").click(function (e)
		{
			if (e.which == 13)
			{
				e.preventDefault();
				addXdccLink();
			}
		});

		$("#xdccDialogButton").click(function ()
		{
			$("#settings").toggle("blind", 500);
			xdccDialog.modal('show');
		});

		$("#statisticsButton").click(function ()
		{
			self.onUpdateStatistics.notify({}, null, this);
			$("#settings").toggle("blind", 500);
			$("#statisticsDialog").modal('show');
		});

		$(".snapshotCheckbox, input[type='checkbox']").click(function ()
		{
			self.onUpdateSnapshotPlot.notify({}, null, this);
		});

		element = $("#showOfflineBots");
		element.click(function ()
		{
			showOfflineBots = !showOfflineBots;
			self.onUpdateOfflineBotsFilter.notify({Enable: showOfflineBots}, null, this);
		});
		if (showOfflineBots)
		{
			element.button("toggle");
		}

		element = $("#humanDates");
		element.click(function ()
		{
			humanDates = !humanDates;
			self.onUpdateHumanDates.notify({Enable: humanDates}, null, this);
		});
		if (humanDates)
		{
			element.button("toggle");
		}

		element = $("#combineBotAndPacketGrid");
		element.click(function ()
		{
			combineBotAndPacketGrid = !combineBotAndPacketGrid;
			self.onCombineBotAndPacketGrid.notify({Enable: combineBotAndPacketGrid}, null, this);
		});
		if (combineBotAndPacketGrid)
		{
			element.button("toggle");
		}
	}

	var self = {
		onSearch: new Slick.Event(),
		onSearchAdd: new Slick.Event(),
		onSearchRemove: new Slick.Event(),
		onSlide: new Slick.Event(),
		onAddServer: new Slick.Event(),
		onAddChannel: new Slick.Event(),
		onUpdateOfflineBotsFilter: new Slick.Event(),
		onUpdateHumanDates: new Slick.Event(),
		onUpdateStatistics: new Slick.Event(),
		onUpdateSnapshotPlot: new Slick.Event(),
		onCombineBotAndPacketGrid: new Slick.Event(),
		onAddXdccLink: new Slick.Event(),

		/**
		 * @param {XGDataView} dataView1
		 * @param {Boolean} showOfflineBots1
		 * @param {Boolean} humanDates1
		 * @param {Boolean} combineBotAndPacketGrid1
		 */
		initialize: function (dataView1, showOfflineBots1, humanDates1, combineBotAndPacketGrid1)
		{
			dataView = dataView1;
			showOfflineBots = showOfflineBots1;
			humanDates = humanDates1;
			combineBotAndPacketGrid = combineBotAndPacketGrid1;

			$(".container, .navbar").show();

			initializeDataView();
			initializeSearch();
			initializeSettings();
			initializeCarousel();
			connectButtons();

			errorDialog.bind('hide',
				function (e)
				{
					e.preventDefault();
				}
			);
		},

		showLoading: function ()
		{
			searchLoading.show();
		},

		hideLoading: function ()
		{
			searchLoading.hide();
		},

		showError: function ()
		{
			errorDialog.modal('show');
		}
	};
	return self;
}());
