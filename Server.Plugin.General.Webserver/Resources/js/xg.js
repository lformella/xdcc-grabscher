//
//  xg.js
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

var XG;
var XGBase = Class.create(
{
	/**
	 * @param {XGUrl} url
	 * @param {XGHelper} helper
	 * @param {XGRefresh} refresh
	 * @param {XGCookie} cookie
	 * @param {XGFormatter} formatter
	 */
	initialize: function(url, helper, refresh, cookie, formatter)
	{
		XG = this;

		this.url = url;
		this.refresh = refresh;
		this.cookie = cookie;
		this.helper = helper;
		this.formatter = formatter;

		this.idServer = 0;
		this.activeTab = 0;

		this.initializeGrids();
		this.initializeDialogs();
		this.initializeOthers();
		this.loadInitialSearches();
	},

	initializeGrids: function()
	{
		var self = this;

		/* ********************************************************************************************************** */
		/* SERVER GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#servers_table").jqGrid(
		{
			url: self.url.guidUrl(Enum.TCPClientRequest.GetServers, ''),
			datatype: "json",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name')],
			colModel: [
				{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return self.formatter.formatServerIcon(r, "XG.flipObject(\"" + o.rowId + "\", \"servers_table\");"); }, width:36, sortable: false, classes: "icon-cell"},
				{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:216, editable:true, fixed:false}
			],
			onSelectRow: function(id)
			{
				if(id)
				{
					self.idServer = id;
					var serv = self.refresh.getRowData('servers_table', id);
					if(serv)
					{
						self.refresh.reloadGrid("channels_table", self.url.guidUrl(Enum.TCPClientRequest.GetChannelsFromServer, id));
					}
				}
			},
			ondblClickRow: function(id)
			{
				if(id)
				{
					self.flipObject(id, "servers_table");
				}
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('servers.sort.index', index);
				self.cookie.setCookie('servers.sort.sortorder', sortorder);
			},
			pager: $('#servers_pager'),
			rowNum: 1000,
			pgbuttons: false,
			pginput: false,
			recordtext: '',
			pgtext: '',
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			height: 300,
			sortname: self.cookie.getCookie('servers.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('servers.sort.sortorder', 'asc'),
			caption: _("Servers"),
			hidegrid: false
		}).navGrid('#servers_pager', {edit:false, search:false}, {},
		{
			mtype: "GET",
			url: "/",
			serializeEditData: function (postdata)
			{
				return { request: Enum.TCPClientRequest.AddServer, name: postdata.Name };
			}
		},
		{
			mtype: "GET",
			url: "/",
			serializeDelData: function (postdata)
			{
				return { request: Enum.TCPClientRequest.RemoveServer, guid: postdata.id };
			}
		});

		/* ********************************************************************************************************** */
		/* CHANNEL GRID                                                                                               */
		/* ********************************************************************************************************** */

		$("#channels_table").jqGrid(
		{
			url: self.url.guidUrl(Enum.TCPClientRequest.GetChannelsFromServer, ''),
			datatype: "json",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name')],
			colModel: [
				{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return self.formatter.formatChannelIcon(r, "XG.flipObject(\"" + o.rowId + "\", \"channels_table\");"); }, width:36, sortable: false, classes: "icon-cell"},
				{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:216, editable:true, fixed:false}
			],
			ondblClickRow: function(id)
			{
				if(id)
				{
					self.flipObject(id, "channels_table");
				}
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('channels.sort.index', index);
				self.cookie.setCookie('channels.sort.sortorder', sortorder);
			},
			pager: $('#channels_pager'),
			rowNum: 1000,
			pgbuttons: false,
			pginput: false,
			recordtext: '',
			pgtext: '',
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			height: 300,
			sortname: self.cookie.getCookie('channels.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('channels.sort.sortorder', 'asc'),
			caption: _("Channels"),
			hidegrid: false
		}).navGrid('#channels_pager', {edit:false, search:false}, {},
		{
			mtype: "GET",
			url: "/",
			serializeEditData: function (postdata)
			{
				return { request: Enum.TCPClientRequest.AddChannel, name: postdata.Name, guid: self.idServer };
			}
		},
		{
			mtype: "GET",
			url: "/",
			serializeDelData: function (postdata)
			{
				return { request: Enum.TCPClientRequest.RemoveChannel, guid: postdata.id };
			}
		});

		/* ********************************************************************************************************** */
		/* BOT GRID                                                                                                   */
		/* ********************************************************************************************************** */

		$("#bots_table").jqGrid(
		{
			url: self.url.guidUrl(Enum.TCPClientRequest.GetBotsFromChannel, ''),
			datatype: "json",
			cmTemplate:{fixed:true},
			colNames: ['', '', _('Name'), _('Speed'), _('Q-Pos'), _('Q-Time'), _('max Speed'), _('Slots'), _('Queue')],
			colModel: [
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return self.formatter.formatBotIcon(r); }, width:32, sortable: false, classes: "icon-cell"},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatBotName(r); }, fixed:false},
				{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return self.helper.speed2Human(r.Speed); }, width:70, align:"right"},
				{name:'QueuePosition',	index:'QueuePosition',	formatter: function(c, o, r) { return r.QueuePosition > 0 ? r.QueuePosition : "&nbsp;"; }, width:70, align:"right"},
				{name:'QueueTime',		index:'QueueTime',		formatter: function(c, o, r) { return self.helper.time2Human(r.QueueTime); }, width:70, align:"right"},
				{name:'InfoSpeedMax',	index:'InfoSpeedMax',	formatter: function(c, o, r) { return self.formatter.formatBotSpeed(r); }, width:100, align:"right"},
				{name:'InfoSlotTotal',	index:'InfoSlotTotal',	formatter: function(c, o, r) { return self.formatter.formatBotSlots(r); }, width:60, align:"right"},
				{name:'InfoQueueTotal',	index:'InfoQueueTotal',	formatter: function(c, o, r) { return self.formatter.formatBotQueue(r); }, width:60, align:"right"}
			],
			onSelectRow: function(id)
			{
				if(id)
				{
					self.refresh.reloadGrid("packets_table", self.url.guidUrl(Enum.TCPClientRequest.GetPacketsFromBot, id));
				}
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('bots.sort.index', index);
				self.cookie.setCookie('bots.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager: $('#bots_pager'),
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			height: 300,
			sortname: self.cookie.getCookie('bots.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('bots.sort.sortorder', 'asc'),
			caption: _("Bots"),
			hidegrid: false
		}).navGrid('#bots_pager', {edit:false, add:false, del:false, search:false});

		/* ********************************************************************************************************** */
		/* PACKET GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#packets_table").jqGrid(
		{
			url: self.url.guidUrl(Enum.TCPClientRequest.GetPacketsFromBot, ''),
			datatype: "json",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Id'), _('Name'), _('Size'), _('Speed'), _('Time'), _('Updated')],
			colModel: [
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return self.formatter.formatPacketIcon(r, "XG.flipPacket(\"" + o.rowId + "\");"); }, width:36, sortable: false, classes: "icon-cell"},
				{name:'Id',				index:'Id',				formatter: function(c, o, r) { return self.formatter.formatPacketId(r); }, width:40, align:"right"},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatPacketName(r); }, fixed:false},
				{name:'Size',			index:'Size',			formatter: function(c, o, r) { return self.formatter.formatPacketSize(r); }, width:70, align:"right"},
				{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return self.formatter.formatPacketSpeed(r); }, width:70, align:"right"},
				{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return self.formatter.formatPacketTimeMissing(r) }, width:90, align:"right"},
				{name:'LastUpdated',	index:'LastUpdated',	formatter: function(c, o, r) { return r.LastUpdated; }, width:135, align:"right"}
			],
			onSelectRow: function(id)
			{
				if(id)
				{
					var pack = self.refresh.getRowData('packets_table', id);
					if(pack)
					{
						$('#bots_table').setSelection(pack.ParentGuid, false);
					}
				}
			},
			ondblClickRow: function(id)
			{
				if(id)
				{
					self.flipPacket(id);
				}
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('packets.sort.index', index);
				self.cookie.setCookie('packets.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager: $('#packets_pager'),
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			height: 300,
			sortname: self.cookie.getCookie('packets.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('packets.sort.sortorder', 'asc'),
			caption: _("Packets"),
			hidegrid: false
		}).navGrid('#packets_pager', {edit:false, add:false, del:false, search:false});

		/* ********************************************************************************************************** */
		/* SEARCH GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#search_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', '', ''],
			colModel: [
				{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Id',		index:'Id',		formatter: function(c) { return self.formatter.formatSearchIcon(c); }, width:26, sortable: false, classes: "icon-cell"},
				{name:'Name',	index:'Name',	fixed:false, sortable: false},
				{name:'Action',	index:'Action',	width:18, sortable: false}
			],
			onSelectRow: function(id)
			{
				if(id)
				{
					var data = $("#search_table").getRowData(id);
					var url1 = "";
					var url2 = "";
					switch(id)
					{
						case "1":
							url1 = self.url.nameUrl(Enum.TCPClientRequest.SearchBot, "0-86400") + "&searchBy=time";
							url2 = self.url.nameUrl(Enum.TCPClientRequest.SearchPacket, "0-86400") + "&searchBy=time";
							break;
						case "2":
							url1 = self.url.nameUrl(Enum.TCPClientRequest.SearchBot, "0-604800") + "&searchBy=time";
							url2 = self.url.nameUrl(Enum.TCPClientRequest.SearchPacket, "0-604800") + "&searchBy=time";
							break;
						case "3":
							url1 = self.url.nameUrl(Enum.TCPClientRequest.SearchBot, data.Name) + "&searchBy=connected";
							url2 = self.url.nameUrl(Enum.TCPClientRequest.SearchPacket, data.Name) + "&searchBy=connected";
							break;
						case "4":
							url1 = self.url.nameUrl(Enum.TCPClientRequest.SearchBot, data.Name) + "&searchBy=enabled";
							url2 = self.url.nameUrl(Enum.TCPClientRequest.SearchPacket, data.Name) + "&searchBy=enabled";
							break;
						default:
							switch(self.activeTab)
							{
								case 0:
									url1 = self.url.nameUrl(Enum.TCPClientRequest.SearchBot, data.Name) + "&searchBy=name";
									url2 = self.url.nameUrl(Enum.TCPClientRequest.SearchPacket, data.Name) + "&searchBy=name";
									break;
								case 1:
									self.refresh.reloadGrid("searches_xg_bitpir_at", "http://xg.bitpir.at/index.php?show=search&action=json&do=search_packets&searchString=" + data.Name);
									break;
							}
							break;
					}

					if(url1 != "")
					{
						self.refresh.reloadGrid("bots_table", url1);
					}
					if(url2 != "")
					{
						self.refresh.reloadGrid("packets_table", url2);
					}
					self.idSearch = id;
				}
			},
			pager: $('#search_pager'),
			pgbuttons: false,
			pginput: false,
			recordtext: '',
			pgtext: '',
			sortname: 'Name',
			ExpandColumn : 'Name',
			viewrecords: true,
			autowidth: true,
			sortorder: "desc",
			hidegrid: false
		});

		$("#search-text").keyup( function (e)
		{
			if (e.which == 13)
			{
				self.addNewSearch();
			}
		});

		/**************************************************************************************************************/
		/* SEARCH GRID                                                                                                */
		/**************************************************************************************************************/

		$("#searches_xg_bitpir_at").jqGrid(
		{
			url: self.url.guidUrl(Enum.TCPClientRequest.Version, ''),
			datatype:'jsonp',
			cmTemplate:{fixed:true},
			colNames:['', '', _('Id'), _('Name'), _('Last Mentioned'), _('Size'), _('Bot'), _('Speed'), ''],
			colModel:[
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return self.formatter.formatPacketIcon(r, "XG.downloadLink(\"" + o.rowId + "\");", true); }, width:24, sortable: false},
				{name:'Id',				index:'Id',				formatter: function(c, o, r) { return self.formatter.formatPacketId(r); }, width:38, align:"right"},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatPacketName(r); }, fixed:false},
				{name:'LastMentioned',	index:'LastMentioned',	formatter: function(c, o, r) { return self.helper.timeStampToHuman(r.LastMentioned); }, width:140, align:"right"},
				{name:'Size',			index:'Size',			formatter: function(c, o, r) { return self.helper.size2Human(r.Size); }, width:60, align:"right"},
				{name:'BotName',		index:'BotName',		formatter: function(c, o, r) { return r.BotName; }, width:160},
				{name:'BotSpeed',		index:'BotSpeed',		formatter: function(c, o, r) { return self.helper.speed2Human(r.BotSpeed); }, width:80, align:"right"},

				{name:'IrcLink',		index:'IrcLink',		formatter: function(c, o, r) { return r.IrcLink; }, hidden:true}
			],
			ondblClickRow: function(id)
			{
				if(id)
				{
					self.downloadLink(id);
				}
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('searches_xg_bitpir_at.sort.index', index);
				self.cookie.setCookie('searches_xg_bitpir_at.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager:$('#searches_pager_xg_bitpir_at'),
			viewrecords:true,
			ExpandColumn:'Name',
			height:'100%',
			autowidth:true,
			sortname: self.cookie.getCookie('searches_xg_bitpir_at.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('searches_xg_bitpir_at.sort.sortorder', 'asc'),
			caption: _("Search via xg.bitpir.at"),
			hidegrid: false
		}).navGrid('#searches_pager_xg_bitpir_at', {edit:false, add:false, del:false, search:false});

		/* ********************************************************************************************************** */
		/* FILE GRID                                                                                                  */
		/* ********************************************************************************************************** */

		$("#files_table").jqGrid(
		{
			url: self.url.guidUrl(Enum.TCPClientRequest.GetFiles, ''),
			datatype: "json",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name'), _('Size'), _('Speed'), _('Time')],
			colModel: [
				{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
				{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return self.formatter.formatFileIcon(r); }, width:24, sortable: false},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return self.formatter.formatFileName(r); }, fixed:false},
				{name:'Size',			index:'Size',			formatter: function(c, o, r) { return self.formatter.formatFileSize(r); }, width:70, align:"right"},
				{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return self.formatter.formatFileSpeed(r); }, width:70, align:"right"},
				{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return self.formatter.formatFileTimeMissing(r) }, width:90, align:"right"}
			],
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('files.sort.index', index);
				self.cookie.setCookie('files.sort.sortorder', sortorder);
			},
			rowNum: 100,
			rowList: [100, 200, 400, 800],
			pager: $('#files_pager'),
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			height: 300,
			sortname: self.cookie.getCookie('files.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('files.sort.sortorder', 'asc'),
			caption: _("Files"),
			hidegrid: false
		}).navGrid('#files_pager', {edit:false, add:false, del:false, search:false});
	},

	initializeDialogs: function()
	{
		var self = this;

		/* ********************************************************************************************************** */
		/* SERVER / CHANNEL DIALOG                                                                                    */
		/* ********************************************************************************************************** */

		$("#server_channel_button")
			.button({icons: { primary: "ui-icon-gear" }})
			.click( function()
			{
				self.refresh.reloadGrid("servers_table", self.url.guidUrl(Enum.TCPClientRequest.GetServers, ''));
				self.refresh.reloadGrid("channels_table", "");
				$("#dialog_server_channels").dialog("open");
			});

		$("#dialog_server_channels").dialog({
			autoOpen: false,
			width: 560,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* STATISTICS DIALOG                                                                                          */
		/* ********************************************************************************************************** */

		$("#statistics_button")
			.button({icons: { primary: "ui-icon-comment" }})
			.click( function()
			{
				self.refresh.refreshStatistic();
				$("#dialog_statistics").dialog("open");
			});

		$("#dialog_statistics").dialog({
			autoOpen: false,
			width: 545,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* SNAPSHOTS DIALOG                                                                                           */
		/* ********************************************************************************************************** */

		//$(".snapshot_checkbox").button();
		$(".snapshot_checkbox, input[name='snapshot_time']").click( function()
		{
			self.refresh.updateSnapshotPlot();
		});

		$("#snapshots_button")
			.button({icons: { primary: "ui-icon-comment" }})
			.click( function()
			{
				$("#dialog_snapshots").dialog("open");
			});

		$("#dialog_snapshots").dialog({
			autoOpen: false,
			width: $(window).width() - 20,
			height: $(window).height() - 20,
			modal: true,
			resizable: false
		});
	},

	initializeOthers: function()
	{
		var self = this;

		$("#tabs").tabs({
			select: function(event, ui)
			{
				self.activeTab = ui.index;
			}
		});
		$("#show_offline_bots").button()
			.click( function()
			{
				self.cookie.setCookie("show_offline_bots", $("#show_offline_bots").attr('checked') ? "1" : "0" );
			});
	},

	loadInitialSearches: function()
	{
		var self = this;

		this.idSearchCount = 1;
		var mydata = [
			{Id:"1", Name: _("ODay Packets"), Action: ""},
			{Id:"2", Name: _("OWeek Packets"), Action: ""},
			{Id:"3", Name: _("Downloads"), Action: ""},
			{Id:"4", Name: _("Enabled Packets"), Action: ""}
		];
		for(var i=0; i<=mydata.length; i++)
		{
			$("#search_table").addRowData(i + 1, mydata[i]);
			this.idSearchCount++;
		}

		// get searches
		$.getJSON(this.url.jsonUrl(Enum.TCPClientRequest.GetSearches),
			function(result) {
				$.each(result.Searches, function(i, item) {
					self.addSearch(item.Search);
				});
			}
		);
	},

	addNewSearch: function ()
	{
		var tbox = $('#search-text');
		if(tbox.val() != "")
		{
			$.get(this.url.nameUrl(Enum.TCPClientRequest.AddSearch, tbox.val()));
			var id = this.addSearch(tbox.val());
			tbox.val('');

			$("#search-text").effect("transfer", { to: $("#" + id) }, 500);
		}
	},

	/**
	 * @param {String} search
	 * @return {Integer}
	 */
	addSearch: function (search)
	{
		var datarow =
		{
			Id: this.idSearchCount,
			Name: search,
			Action: "<i class='icon-cancel-circle2 icon-overlay ScarletRedMiddle button' onclick='XG.removeSearch(" + this.idSearchCount + ");'></i>"
		};
		$("#search_table").addRowData(this.idSearchCount, datarow);
		return this.idSearchCount++;
	},

	/**
	 * @param {String} guid
	 */
	removeSearch: function (guid)
	{
		if(guid <= 4)
		{
			return;
		}
		var data = $("#search_table").getRowData(guid);
		$.get(this.url.nameUrl(Enum.TCPClientRequest.RemoveSearch, data.Name));

		$("#" + guid).effect("transfer", { to: $("#search-text") }, 500);
		$('#search_table').delRowData(guid);
	},

	/**
	 * @param {String} guid
	 */
	flipPacket: function (guid)
	{
		var self = this;

		var pack = this.refresh.getRowData('packets_table', guid);
		if(pack)
		{
			if(!pack.Enabled)
			{
				$("#" + guid).effect("transfer", { to: $("#4") }, 500);

				$.get(this.url.guidUrl(Enum.TCPClientRequest.ActivateObject, guid));
				setTimeout(function() { self.refresh.refreshObject('packets_table', guid); }, 1000);
			}
			else
			{
				$("#4").effect("transfer", { to: $("#" + guid) }, 500);

				$.get(this.url.guidUrl(Enum.TCPClientRequest.DeactivateObject, guid));

				if (this.idSearch == 3 || this.idSearch == 4)
				{
					setTimeout(function() { self.refresh.reloadGrid("bots_table", ""); }, 1000);
					setTimeout(function() { self.refresh.reloadGrid("packets_table", ""); }, 1000);
				}
				else
				{
					setTimeout(function() { self.refresh.refreshObject("packets_table", guid); }, 1000);
				}
			}
		}
	},

	/**
	 * @param {String} guid
	 * @param {String} grid
	 */
	flipObject: function (guid, grid)
	{
		var self = this;

		var obj = this.refresh.getRowData(grid, guid);
		if(obj)
		{
			if(!obj.Enabled)
			{
				$.get(this.url.guidUrl(Enum.TCPClientRequest.ActivateObject, guid));
			}
			else
			{
				$.get(this.url.guidUrl(Enum.TCPClientRequest.DeactivateObject, guid));
			}
			setTimeout(function() { self.refresh.refreshObject(grid, guid); }, 1000);
		}
	},

	/**
	 * @param {String} guid
	 */
	downloadLink: function (guid)
	{
		$("#" + guid).effect("transfer", { to: $("#4") }, 500);

		var data = this.refresh.getRowData("searches_xg_bitpir_at", guid);
		$.get(this.url.nameUrl(Enum.TCPClientRequest.ParseXdccLink, data.IrcLink));
	}
});
