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
	var dataView, formatter;

	var searchForm = $("#searchForm, #searchButtons");
	var searches = $("#searches");
	var search = $("#search");
	var searchAdd = $("#searchAdd");
	var searchRemove = $("#searchRemove");
	var searchLoading = $("#searchLoading");

	var errorDialog = $("#errorDialog");
	var xdccDialog = $("#xdccDialog");
	var xdccLink = $("#xdccLink");

	var unreadNotificationCounter = 0;
	var unreadNotifications = $("#unreadNotifications");
	var favicon = new Favico({
		animation:'none'
	});

	var currentSearchGuid = undefined;
	var currentSlide = 0;
	var showOfflineBots, humanDates, combineBotAndPacketGrid;

	var showEffect = { effect: "drop", duration: 500 };

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
					"<span class='resultsOnline label label-success pull-right'>" + args.Data.ResultsOnline + "</span>" +
					"<span class='resultsOffline label label-default pull-right'>" + args.Data.ResultsOffline + "</span>" +
					"<span class='text' title='" + args.Data.Name + "'>" + args.Data.Name + "</span>" +
					"</a></li>");
				$("a[data-guid='" + args.Data.Guid + "']").click(
					function (e)
					{
						searchClick($(this));
					}
				);
			}
			else if (args.DataType == Enum.Grid.Notification)
			{
				updateUnreadNotifications(1);

				if ("Notification" in window && Notification.permission === "granted")
				{
					var options = {
						body: "",
						tag: args.Data.Guid,
						icon: ""
					};
					new Notification(formatter.formatNotificationContent(args.Data, true), options);
				}
			}
		});

		dataView.onUpdate.subscribe(function (e, args)
		{
			if (args.DataType == Enum.Grid.Search)
			{
				var element = $("a[data-guid='" + args.Data.Guid + "'] .resultsOnline");
				element.html(args.Data.ResultsOnline);

				element = $("a[data-guid='" + args.Data.Guid + "'] .resultsOffline");
				element.html(args.Data.ResultsOffline)
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

		if ("Notification" in window && Notification.permission !== "granted" && Notification.permission !== 'denied')
		{
			Notification.requestPermission(function (permission)
			{
				if (!('permission' in Notification))
				{
					Notification.permission = permission;
				}
			});
		}
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
		search.focus();
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
					searchForm.show(showEffect);
				}
				else
				{
					searchForm.hide(showEffect);
				}
			});
		$("#mainCarousel").carousel({ interval: false });
		$("#mainCarousel").bind('slid.bs.carousel',
			function ()
			{
				self.onSlide.notify({ "slide": currentSlide}, null, this);
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
		if (isXdccLinkValid(xdccLink.val()))
		{
			self.onAddXdccLink.notify({Name: xdccLink.val()}, null, this);
			xdccLink.val("");
			xdccDialog.modal("hide");
			$("#xdccDialog .form-group").removeClass('has-error');
		}
	}

	function updateUnreadNotifications (counter)
	{
		if (counter == 0)
		{
			unreadNotificationCounter = 0;
			unreadNotifications.html("");
			unreadNotifications.hide(showEffect);
		}
		else
		{
			unreadNotificationCounter += counter;
			unreadNotifications.html(unreadNotificationCounter);
			unreadNotifications.show(showEffect);
		}
		favicon.badge(unreadNotificationCounter);
	}

	function connectButtons ()
	{
		var element;

		$("#notificationsLink").click(function ()
		{
			updateUnreadNotifications(0);
			$("#" + Enum.Grid.Notification + "Grid").height("auto");
		});

		$("#serverChannelButton").click(function ()
		{
			$("#serverChannelsDialog").modal('show');
		});
		// hook on dialog events to optimize the height because slickgrid will corrupt the viewport
		$("#serverChannelsDialog").bind('shown.bs.modal',
			function ()
			{
				$("#" + Enum.Grid.Server + "Grid, #" + Enum.Grid.Channel + "Grid").height("auto");
			}
		);
		$("#serverChannelsDialog").bind('hidden.bs.modal',
			function ()
			{
				$("#" + Enum.Grid.Server + "Grid, #" + Enum.Grid.Channel + "Grid").height("320");
			}
		);

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
		$("#channel").keyup(function (e)
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
		xdccLink.keyup(function (e)
		{
			if (isXdccLinkValid(xdccLink.val()))
			{
				$("#xdccDialog .form-group").removeClass('has-error');
			}
			else
			{
				$("#xdccDialog .form-group").addClass('has-error');
			}

			if (e.which == 13)
			{
				e.preventDefault();
				addXdccLink();
			}
		});

		$("#xdccDialogButton").click(function ()
		{
			xdccDialog.modal('show');
		});

		$("input[name='snapshotTime']").click(function ()
		{
			self.onRequestSnapshotPlot.notify({Value: $(this).val()}, null, this);
		});

		$("input[id^='snapshotCheckbox']").click(function ()
		{
			self.onUpdateSnapshotPlot.notify({}, null, this);
		});

		element = $("#showOfflineBots");
		element.click(function ()
		{
			showOfflineBots = !showOfflineBots;
			self.onUpdateOfflineBotsFilter.notify({ Enable: showOfflineBots }, null, this);
			updateFlipableVar("showOfflineBots", showOfflineBots);
		});
		updateFlipableVar("showOfflineBots", showOfflineBots);

		element = $("#humanDates");
		element.click(function ()
		{
			humanDates = !humanDates;
			self.onUpdateHumanDates.notify({ Enable: humanDates }, null, this);
			updateFlipableVar("humanDates", humanDates);
		});
		updateFlipableVar("humanDates", humanDates);

		element = $("#combineBotAndPacketGrid");
		element.click(function ()
		{
			combineBotAndPacketGrid = !combineBotAndPacketGrid;
			self.onCombineBotAndPacketGrid.notify({ Enable: combineBotAndPacketGrid }, null, this);
			updateFlipableVar("combineBotAndPacketGrid", combineBotAndPacketGrid);
		});
		updateFlipableVar("combineBotAndPacketGrid", combineBotAndPacketGrid);
	}

	function updateFlipableVar (variable, value)
	{
		var element = $("#" + variable + " span");
		if (value)
		{
			element.addClass("ScarletRedDark");
		}
		else
		{
			element.removeClass("ScarletRedDark");
		}
	}

	function isXdccLinkValid (link)
	{
		return link.match(/^xdcc:\/\/([^/]+\/){2}#([^/]+\/){2}#[0-9]+\/[^/]+\/$/);
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
		onUpdateSnapshotPlot: new Slick.Event(),
		onCombineBotAndPacketGrid: new Slick.Event(),
		onAddXdccLink: new Slick.Event(),
		onOpenNotifications: new Slick.Event(),
		onRequestSnapshotPlot: new Slick.Event(),

		/**
		 * @param {XGDataView} dataView1
		 * @param {XGFormatter} formatter1
		 * @param {Boolean} showOfflineBots1
		 * @param {Boolean} humanDates1
		 * @param {Boolean} combineBotAndPacketGrid1
		 */
		initialize: function (dataView1, formatter1, showOfflineBots1, humanDates1, combineBotAndPacketGrid1)
		{
			dataView = dataView1;
			formatter = formatter1;
			showOfflineBots = showOfflineBots1;
			humanDates = humanDates1;
			combineBotAndPacketGrid = combineBotAndPacketGrid1;

			$("div.container, nav.navbar").show();

			initializeDataView();
			initializeSearch();
			initializeCarousel();
			connectButtons();

			errorDialog.bind('hide.bs.modal',
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
		},

		showInfo: function (text)
		{
			$("#infoLabel").html(text);
			$("#infoDialog").modal('show');
		}
	};
	return self;
}());
