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

/* ****************************************************************************************************************** */
/* ENUM STUFF                                                                                                         */
/* ****************************************************************************************************************** */

function Enum() {}

Enum.TCPClientRequest =
{
	None: 0,
	Version: 1,

	AddServer: 2,
	RemoveServer: 3,
	AddChannel: 4,
	RemoveChannel: 5,

	ActivateObject: 6,
	DeactivateObject: 7,

	SearchPacket: 8,
	SearchBot: 9,

	GetServers: 10,
	GetChannelsFromServer: 11,
	GetBotsFromChannel: 12,
	GetPacketsFromBot: 13,
	GetFiles: 14,
	GetObject: 15,

	AddSearch: 16,
	RemoveSearch: 17,
	GetSearches: 18,

	GetStatistics: 19,
	GetSnapshots: 20,
	ParseXdccLink: 21,

	CloseServer: 22
};

Enum.TangoColor =
{
	Butter		: { Light: "fce94f", Middle: "edd400", Dark: "c4a000"},
	Orange		: { Light: "fcaf3e", Middle: "f57900", Dark: "ce5c00"},
	Chocolate	: { Light: "e9b96e", Middle: "c17d11", Dark: "8f5902"},
	Chameleon	: { Light: "8ae234", Middle: "73d216", Dark: "4e9a06"},
	SkyBlue		: { Light: "729fcf", Middle: "3465a4", Dark: "204a87"},
	Plum		: { Light: "ad7fa8", Middle: "75507b", Dark: "5c3566"},
	ScarletRed	: { Light: "ef2929", Middle: "cc0000", Dark: "a40000"},
	Aluminium1	: { Light: "eeeeec", Middle: "d3d7cf", Dark: "babdb6"},
	Aluminium2	: { Light: "888a85", Middle: "555753", Dark: "2e3436"}
};

/* ****************************************************************************************************************** */
/* GLOBAL VARS / FUNCTIONS                                                                                            */
/* ****************************************************************************************************************** */

var Password = "";

var idServer;
var idSearch;

var searchActive = false;
var activeTab = 0;

/**
 * @param {String} password
 * @return {String}
 * @constructor
 */
var JsonUrl = function (password) { return "/?password=" + (password != undefined ? encodeURIComponent(password) : encodeURIComponent(Password)) + "&offbots=" + ($("#show_offline_bots").attr('checked') ? "1" : "0" ) + "&request="; };

/**
 *
 * @param {Integer} id
 * @param {String} guid
 * @return {String}
 * @constructor
 */
var GuidUrl = function (id, guid) { return JsonUrl() + id + "&guid=" + guid; };

/**
 *
 * @param {Integer} id
 * @param {String} name
 * @return {String}
 * @constructor
 */
var NameUrl = function (id, name) { return JsonUrl() + id + "&name=" + encodeURIComponent(name); };

var idSearchCount = 1;

var LANG_MONTH_SHORT = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
var LANG_MONTH = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
var LANG_WEEKDAY = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

var Formatter;
var Helper = new XGHelper();

var outerLayout, innerLayout;

var snapshots;



/* ****************************************************************************************************************** */
/* GRID / FORM LOADER                                                                                                 */
/* ****************************************************************************************************************** */

$(function()
{
	Formatter = new XGFormatter();

	/* ************************************************************************************************************** */
	/* SERVER GRID                                                                                                    */
	/* ************************************************************************************************************** */

	$("#servers_table").jqGrid(
	{
		datatype: "json",
		cmTemplate: {fixed:true},
		colNames: ['', '', 'Name'],
		colModel: [
			{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return Formatter.formatServerIcon(r, "FlipObject(\"" + o.rowId + "\", \"servers_table\");"); }, width:34, sortable: false, classes: "icon-cell"},
			{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:218, editable:true, fixed:false}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				idServer = id;
				var serv = GetRowData('servers_table', id);
				if(serv)
				{
					ReloadGrid("channels_table", GuidUrl(Enum.TCPClientRequest.GetChannelsFromServer, id));
				}
			}
		},
		ondblClickRow: function(id)
		{
			if(id)
			{
				FlipObject(id, "servers_table");
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('servers.sort.index', index);
			SetCookie('servers.sort.sortorder', sortorder);
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
		sortname: GetCookie('servers.sort.index', 'Name'),
		sortorder: GetCookie('servers.sort.sortorder', 'asc'),
		caption: "Servers",
		hidegrid: false
	}).navGrid('#servers_pager', {edit:false, search:false}, {},
	{
		mtype: "GET",
		url: "/",
		serializeEditData: function (postdata)
		{
			return { password: escape(Password), request: Enum.TCPClientRequest.AddServer, name: postdata.Name };
		}
	},
	{
		mtype: "GET",
		url: "/",
		serializeDelData: function (postdata)
		{
			return { password: escape(Password), request: Enum.TCPClientRequest.RemoveServer, guid: postdata.id };
		}
	});

	/* ************************************************************************************************************** */
	/* CHANNEL GRID                                                                                                   */
	/* ************************************************************************************************************** */

	$("#channels_table").jqGrid(
	{
		datatype: "json",
		cmTemplate: {fixed:true},
		colNames: ['', '', 'Name'],
		colModel: [
			{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return Formatter.formatChannelIcon(r, "FlipObject(\"" + o.rowId + "\", \"channels_table\");"); }, width:34, sortable: false, classes: "icon-cell"},
			{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:218, editable:true, fixed:false}
		],
		ondblClickRow: function(id)
		{
			if(id)
			{
				FlipObject(id, "channels_table");
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('channels.sort.index', index);
			SetCookie('channels.sort.sortorder', sortorder);
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
		sortname: GetCookie('channels.sort.index', 'Name'),
		sortorder: GetCookie('channels.sort.sortorder', 'asc'),
		caption: "Channels",
		hidegrid: false
	}).navGrid('#channels_pager', {edit:false, search:false}, {},
	{
		mtype: "GET",
		url: "/",
		serializeEditData: function (postdata)
		{
			return { password: escape(Password), request: Enum.TCPClientRequest.AddChannel, name: postdata.Name, guid: idServer };
		}
	},
	{
		mtype: "GET",
		url: "/",
		serializeDelData: function (postdata)
		{
			return { password: escape(Password), request: Enum.TCPClientRequest.RemoveChannel, guid: postdata.id };
		}
	});

	/* ************************************************************************************************************** */
	/* BOT GRID                                                                                                       */
	/* ************************************************************************************************************** */

	$("#bots_table").jqGrid(
	{
		datatype: "json",
		cmTemplate:{fixed:true},
		colNames: ['', '', 'Name', 'Speed', 'Q-Pos', 'Q-Time', 'Speed', 'Slots', 'Queue'],
		colModel: [
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatBotIcon(r); }, width:28, sortable: false, classes: "icon-cell"},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatBotName(r); }, fixed:false},
			{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return Helper.speed2Human(r.Speed); }, width:70, align:"right"},
			{name:'QueuePosition',	index:'QueuePosition',	formatter: function(c, o, r) { return r.QueuePosition > 0 ? r.QueuePosition : "&nbsp;"; }, width:70, align:"right"},
			{name:'QueueTime',		index:'QueueTime',		formatter: function(c, o, r) { return Helper.time2Human(r.QueueTime); }, width:70, align:"right"},
			{name:'InfoSpeedMax',	index:'InfoSpeedMax',	formatter: function(c, o, r) { return Formatter.formatBotSpeed(r); }, width:100, align:"right"},
			{name:'InfoSlotTotal',	index:'InfoSlotTotal',	formatter: function(c, o, r) { return Formatter.formatBotSlots(r); }, width:60, align:"right"},
			{name:'InfoQueueTotal',	index:'InfoQueueTotal',	formatter: function(c, o, r) { return Formatter.formatBotQueue(r); }, width:60, align:"right"}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				searchActive = false;
				ReloadGrid("packets_table", GuidUrl(Enum.TCPClientRequest.GetPacketsFromBot, id));
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('bots.sort.index', index);
			SetCookie('bots.sort.sortorder', sortorder);
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager: $('#bots_pager'),
		ExpandColumn: 'Name',
		viewrecords: true,
		autowidth: true,
		scrollrows: true,
		height: 300,
		sortname: GetCookie('bots.sort.index', 'Name'),
		sortorder: GetCookie('bots.sort.sortorder', 'asc'),
		caption: "Bots",
		hidegrid: false
	}).navGrid('#bots_pager', {edit:false, add:false, del:false, search:false});

	/* ************************************************************************************************************** */
	/* PACKET GRID                                                                                                    */
	/* ************************************************************************************************************** */

	$("#packets_table").jqGrid(
	{
		datatype: "json",
		cmTemplate: {fixed:true},
		colNames: ['', '', 'Id', 'Name', 'Size', 'Speed', 'Time', 'Updated'],
		colModel: [
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatPacketIcon(r, "FlipPacket(\"" + o.rowId + "\");"); }, width:33, sortable: false, classes: "icon-cell"},
			{name:'Id',				index:'Id',				formatter: function(c, o, r) { return Formatter.formatPacketId(r); }, width:40, align:"right"},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatPacketName(r); }, fixed:false},
			{name:'Size',			index:'Size',			formatter: function(c, o, r) { return Formatter.formatPacketSize(r); }, width:70, align:"right"},
			{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return Formatter.formatPacketSpeed(r); }, width:70, align:"right"},
			{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return Formatter.formatPacketTimeMissing(r) }, width:90, align:"right"},
			{name:'LastUpdated',	index:'LastUpdated',	formatter: function(c, o, r) { return r.LastUpdated; }, width:135, align:"right"}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				var pack = GetRowData('packets_table', id);
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
				FlipPacket(id);				
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('packets.sort.index', index);
			SetCookie('packets.sort.sortorder', sortorder);
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager: $('#packets_pager'),
		ExpandColumn: 'Name',
		viewrecords: true,
		autowidth: true,
		height: 300,
		sortname: GetCookie('packets.sort.index', 'Id'),
		sortorder: GetCookie('packets.sort.sortorder', 'asc'),
		caption: "Packets",
		hidegrid: false
	}).navGrid('#packets_pager', {edit:false, add:false, del:false, search:false});

	/* ************************************************************************************************************** */
	/* SEARCH GRID                                                                                                    */
	/* ************************************************************************************************************** */

	$("#search_table").jqGrid(
	{
		datatype: "local",
		cmTemplate: {fixed:true},
		colNames: ['', '', '', ''],
		colModel: [
			{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Id',		index:'Id',		formatter: function(c) { return Formatter.formatSearchIcon(c); }, width:26, sortable: false},
			{name:'Name',	index:'Name',	fixed:false, sortable: false},
			{name:'Action',	index:'Action',	width:17, sortable: false}
		],
		onSelectRow: function(id)
		{
			searchActive = true;
			if(id)
			{
				var data = $("#search_table").getRowData(id);
				var url1 = "";
				var url2 = "";
				switch(id)
				{
					case "1":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBot, "0-86400000") + "&searchBy=time";
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacket, "0-86400000") + "&searchBy=time";
						break;
					case "2":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBot, "0-604800000") + "&searchBy=time";
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacket, "0-604800000") + "&searchBy=time";
						break;
					case "3":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBot, data.Name) + "&searchBy=connected";
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacket, data.Name) + "&searchBy=connected";
						break;
					case "4":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBot, data.Name) + "&searchBy=enabled";
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacket, data.Name) + "&searchBy=enabled";
						break;
					default:
						switch(activeTab)
						{
							case 0:
								url1 = NameUrl(Enum.TCPClientRequest.SearchBot, data.Name) + "&searchBy=name";
								url2 = NameUrl(Enum.TCPClientRequest.SearchPacket, data.Name) + "&searchBy=name";
								break;
							case 1:
								ReloadGrid("searches_xg_bitpir_at", "http://xg.bitpir.at/index.php?show=search&action=json&do=search_packets&searchString=" + data.Name);
								break;
						}
						break;
				}

				if(url1 != "")
				{
					ReloadGrid("bots_table", url1);
				}
				if(url2 != "")
				{
					ReloadGrid("packets_table", url2);
				}
				idSearch = id;
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
	}).navGrid('#search_pager', {edit:false, add:false, del:false, search:false, refresh:false});

	$("#search_pager_left").html("<input type=\"text\" id=\"search-text\" />");

	var mydata = [
		{Id:"1", Name:"ODay Packets", Action: ""},
		{Id:"2", Name:"OWeek Packets", Action: ""},
		{Id:"3", Name:"Downloads", Action: ""},
		{Id:"4", Name:"Enabled Packets", Action: ""}
	];
	for(var i=0; i<=mydata.length; i++)
	{
		$("#search_table").addRowData(i + 1, mydata[i]);
		idSearchCount++;
	}

	/* ************************************************************************************************************** */
	/* SEARCH STUFF                                                                                                   */
	/* ************************************************************************************************************** */

	$("#search-text").keyup( function (e)
	{
		if (e.which == 13)
		{
			AddNewSearch();
		}
	});

	/******************************************************************************************************************/
	/* SEARCH GRID                                                                                                    */
	/******************************************************************************************************************/

	$("#searches_xg_bitpir_at").jqGrid(
	{
		datatype:'jsonp',
		cmTemplate:{fixed:true},
		colNames:['', '', 'Id', 'Name', 'Last Mentioned', 'Size', 'Bot', 'Speed', ''],
		colModel:[
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return Formatter.formatPacketIcon(r, "DownloadLink(\"" + o.rowId + "\");", true); }, width:24, sortable: false},
			{name:'Id',				index:'Id',				formatter: function(c, o, r) { return Formatter.formatPacketId(r); }, width:38, align:"right"},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatPacketName(r); }, fixed:false},
			{name:'LastMentioned',	index:'LastMentioned',	formatter: function(c, o, r) { return Helper.timeStampToHuman(r.LastMentioned); }, width:140, align:"right"},
			{name:'Size',			index:'Size',			formatter: function(c, o, r) { return Helper.size2Human(r.Size); }, width:60, align:"right"},
			{name:'BotName',		index:'BotName',		formatter: function(c, o, r) { return r.BotName; }, width:160},
			{name:'BotSpeed',		index:'BotSpeed',		formatter: function(c, o, r) { return Helper.speed2Human(r.BotSpeed); }, width:80, align:"right"},

			{name:'IrcLink',		index:'IrcLink',		formatter: function(c, o, r) { return r.IrcLink; }, hidden:true}
		],
		ondblClickRow: function(id)
		{
			if(id)
			{
				DownloadLink(id);
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('searches_xg_bitpir_at.sort.index', index);
			SetCookie('searches_xg_bitpir_at.sort.sortorder', sortorder);
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager:$('#searches_pager_xg_bitpir_at'),
		viewrecords:true,
		ExpandColumn:'Name',
		height:'100%',
		autowidth:true,
		sortname: GetCookie('searches_xg_bitpir_at.sort.index', 'Id'),
		sortorder: GetCookie('searches_xg_bitpir_at.sort.sortorder', 'asc'),
		caption: "Search via xg.bitpir.at",
		hidegrid: false
	}).navGrid('#searches_pager_xg_bitpir_at', {edit:false, add:false, del:false, search:false});

	/* ************************************************************************************************************** */
	/* FILE GRID                                                                                                      */
	/* ************************************************************************************************************** */

	$("#files").jqGrid(
	{
		datatype: "json",
		cmTemplate: {fixed:true},
		colNames: ['', '', 'Name', 'Size', 'Speed', 'Time'],
		colModel: [
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatFileIcon(r); }, width:24, sortable: false},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatFileName(r); }, fixed:false},
			{name:'Size',			index:'Size',			formatter: function(c, o, r) { return Formatter.formatFileSize(r); }, width:70, align:"right"},
			{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return Formatter.formatFileSpeed(r); }, width:70, align:"right"},
			{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return Formatter.formatFileTimeMissing(r) }, width:90, align:"right"}
		],
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('files.sort.index', index);
			SetCookie('files.sort.sortorder', sortorder);
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager: $('#files_pager'),
		ExpandColumn: 'Name',
		viewrecords: true,
		autowidth: true,
		height: 300,
		sortname: GetCookie('files.sort.index', 'Id'),
		sortorder: GetCookie('files.sort.sortorder', 'asc'),
		caption: "Files",
		hidegrid: false
	}).navGrid('#files_pager', {edit:false, add:false, del:false, search:false});

	/* ************************************************************************************************************** */
	/* PASSWORD DIALOG                                                                                                */
	/* ************************************************************************************************************** */

	$("#dialog_password").dialog({
		bgiframe: true,
		height: 140,
		modal: true,
		resizable: false,
		hide: 'explode',
		buttons: {
			'Connect': function()
			{
				ButtonConnectClicked($(this));
			}
		},
		close: function()
		{
			if(Password == "")
			{
				$('#dialog_password').dialog('open');
			}
			$("#password").val('').removeClass('ui-state-error');
		}
	});

	$("#password").keyup(function (e) {
		if (e.which == 13)
		{
			ButtonConnectClicked($("#dialog_password"));
		}
	});

	/* ************************************************************************************************************** */
	/* SERVER / CHANNEL DIALOG                                                                                        */
	/* ************************************************************************************************************** */

	$("#server_channel_button")
		.button({icons: { primary: "ui-icon-gear" }})
		.click( function()
		{
			ReloadGrid("servers_table", GuidUrl(Enum.TCPClientRequest.GetServers, ''));
			ReloadGrid("channels_table");
			$("#dialog_server_channels").dialog("open");
		});

	$("#dialog_server_channels").dialog({
		bgiframe: true,
		autoOpen: false,
		width: 560,
		modal: true,
		resizable: false
	});

	/* ************************************************************************************************************** */
	/* STATISTICS DIALOG                                                                                              */
	/* ************************************************************************************************************** */

	$("#statistics_button")
		.button({icons: { primary: "ui-icon-comment" }})
		.click( function()
		{
			RefreshStatistic();
			$("#dialog_statistics").dialog("open");
		});

	$("#dialog_statistics").dialog({
		bgiframe: true,
		autoOpen: false,
		width: 545,
		modal: true,
		resizable: false
	});

	/* ************************************************************************************************************** */
	/* SNAPSHOTS DIALOG                                                                                               */
	/* ************************************************************************************************************** */

	//$(".snapshot_checkbox").button();
	$(".snapshot_checkbox, input[name='snapshot_time']").click( function()
	{
		UpdateSnapshotPlot();
	});

	$("#snapshots_button")
		.button({icons: { primary: "ui-icon-comment" }})
		.click( function()
		{
			$("#dialog_snapshots").dialog("open");
		});

	$("#dialog_snapshots").dialog({
		bgiframe: true,
		autoOpen: false,
		width: 1230,
		height: 750,
		modal: true,
		resizable: false
	});

	/* ************************************************************************************************************** */
	/* OTHERS                                                                                                         */
	/* ************************************************************************************************************** */

	$("#tabs").tabs({
		select: function(event, ui)
		{
			activeTab = ui.index;
		}
	});

	$("#show_offline_bots").button();
});

/* ****************************************************************************************************************** */
/* SNAPSHOT STUFF                                                                                                     */
/* ****************************************************************************************************************** */

var UpdateSnapshots = function ()
{
	$.getJSON(JsonUrl() + Enum.TCPClientRequest.GetSnapshots,
		function(result)
		{
			result[0].yaxis = 2;
			$.each(result, function(index, item) {
				item.color = index;
			});

			snapshots = result;
			UpdateSnapshotPlot();
		}
	);
};

var UpdateSnapshotPlot = function ()
{
	var days = parseInt($("input[name='snapshot_time']:checked").val());
	var snapshotsMinDate = days > 0 ? new Date().getTime() - (60 * 60 * 24 * days * 1000) : days;

	var data = [];
	var currentSnapshots = $.extend(true, [], snapshots);
	$.each(currentSnapshots, function(index, item) {
		if (index == 0 || $("#snapshot_checkbox_" + index).attr('checked'))
		{
			var itemData = [];
			$.each(item.data, function(index2, item2) {
				if (snapshotsMinDate < item2[0])
				{
					itemData.push(item2);
				}
			});
			item.data = itemData;

			data.push(item);
		}
	});

	var markerFunction;
	var tickSize;
	var timeFormat;
	switch (days)
	{
		case 1:
			timeFormat = "%H:%M";
			tickSize = [2, "hour"];
			markerFunction = function (axes) {
				var markings = [];
				var d = new Date(axes.xaxis.min);
				d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
				d.setUTCSeconds(0);
				d.setUTCMinutes(0);
				d.setUTCHours(0);
				var i = d.getTime();
				do
				{
					markings.push({
						xaxis: {
							from: i,
							to: i + 2 * 60 * 60 * 1000
						}
					});
					i += 4 * 60 * 60 * 1000;
				} while (i < axes.xaxis.max);

				return markings;
			};
			break;

		case 7:
			timeFormat = "%d. %b";
			tickSize = [1, "day"];
			markerFunction = function (axes) {
				var markings = [];
				var d = new Date(axes.xaxis.min);
				d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
				d.setUTCSeconds(0);
				d.setUTCMinutes(0);
				d.setUTCHours(0);
				var i = d.getTime();
				do
				{
					markings.push({
						xaxis: {
							from: i,
							to: i + 2 * 24 * 60 * 60 * 1000
						}
					});
					i += 7 * 24 * 60 * 60 * 1000;
				} while (i < axes.xaxis.max);

				return markings;
			};
			break;

		case 31:
			timeFormat = "%d. %b";
			tickSize = [7, "day"];
			markerFunction = function (axes) {
				var markings = [];
				var d = new Date(axes.xaxis.min);
				d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
				d.setUTCSeconds(0);
				d.setUTCMinutes(0);
				d.setUTCHours(0);
				var i = d.getTime();
				do
				{
					markings.push({
						xaxis: {
							from: i,
							to: i + 7 * 24 * 60 * 60 * 1000
						}
					});
					i += 14 * 24 * 60 * 60 * 1000;
				} while (i < axes.xaxis.max);

				return markings;
			};
			break;

		default:
			timeFormat = "%b %y";
			tickSize = [1, "month"];
			markerFunction = function (axes) {
				var markings = [];
				var d = new Date(axes.xaxis.min);
				d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
				d.setUTCSeconds(0);
				d.setUTCMinutes(0);
				d.setUTCHours(0);
				var i = d.getTime();
				do
				{
					markings.push({
						xaxis: {
							from: i,
							to: i + 7 * 24 * 60 * 60 * 1000
						}
					});
					i += 14 * 24 * 60 * 60 * 1000;
				} while (i < axes.xaxis.max);

				return markings;
			};
			break;
	}

	var snapshotOptions = {
		xaxis: {
			mode: "time",
			timeformat: timeFormat,
			minTickSize: tickSize,
			monthNames: LANG_MONTH_SHORT
		},
		yaxes: [
			{ min: 0 },
			{
				min: 0,
				alignTicksWithAxis: 1,
				position: "right",
				tickFormatter: function (speed) {
					if (speed <= 1)
					{
						return "";
					}
					return Helper.speed2Human(speed);
				}
			}
		],
		legend: { position: "sw" },
		grid: { markings: markerFunction }
	};

	$.plot($("#snapshot"), data, snapshotOptions);
};

/* ****************************************************************************************************************** */
/* SEARCH STUFF                                                                                                       */
/* ****************************************************************************************************************** */

var AddNewSearch = function ()
{
	var tbox = $('#search-text');
	if(tbox.val() != "")
	{
		$.get(NameUrl(Enum.TCPClientRequest.AddSearch, tbox.val()));
		var id = AddSearch(tbox.val());
		tbox.val('');

		$("#search-text").effect("transfer", { to: $("#" + id) }, 500);
	}
};

var AddSearch = function (search)
{
	var datarow =
	{
		Id: idSearchCount,
		Name: search,
		Action: "<i class='icon-cancel-circle2 icon-overlay ScarletRedMiddle button' onclick='RemoveSearch(" + idSearchCount + ");'></i>"
	};
	$("#search_table").addRowData(idSearchCount, datarow);
	return idSearchCount++;
};

var RemoveSearch = function (id)
{
	if(id <= 4)
	{
		return;
	}
	var data = $("#search_table").getRowData(id);
	$.get(NameUrl(Enum.TCPClientRequest.RemoveSearch, data.Name));

	$("#" + id).effect("transfer", { to: $("#search-text") }, 500);
	$('#search_table').delRowData(id);
};

/* ****************************************************************************************************************** */
/* DIALOG BUTTON HANDLER                                                                                              */
/* ****************************************************************************************************************** */

var ButtonConnectClicked = function (dialog)
{
	var passwordElement = $("#password");
	var password = CryptoJS.SHA256(salt + passwordElement.val() + salt);

	if (CheckPassword(password))
	{
		passwordElement.removeClass('ui-state-error');
		SetPassword(password);
		dialog.dialog('close');

		// Get search_table
		$.getJSON(JsonUrl() + Enum.TCPClientRequest.GetSearches,
			function(result) {
				$.each(result.Searches, function(i, item) {
					AddSearch(item.Search);
				});
			}
		);

		ReloadGrid("files", JsonUrl() + Enum.TCPClientRequest.GetFiles);

		outerLayout = $("body").layout({
			onresize: function () {
				ResizeMain();
			},
			spacing_open: 4,
			spacing_closed: 4
		});

		innerLayout = $("#layout_objects_container").layout({
			resizeWithWindow: false,
			onresize: function () {
				ResizeContainer();
			},
			spacing_open: 4,
			spacing_closed: 4
		});

		// resize after all is visible
		ResizeMain();
		// double resize because the first run wont change all values :|
		ResizeMain();

		// start the refresh
		RefreshGrid(0);
	}
	else
	{
		passwordElement.addClass('ui-state-error');
	}
};

/* ****************************************************************************************************************** */
/* PASSWORD STUFF                                                                                                     */
/* ****************************************************************************************************************** */

var SetPassword = function (password)
{
	Password = password;
};

/**
 * @param {String} password
 * @return {Boolean}
 * @constructor
 */
var CheckPassword = function (password)
{
	var res = false;
	$.ajax({
		url: JsonUrl(password) + Enum.TCPClientRequest.Version,
		success: function(result)
		{
			res = true;
		},
		async: false
	});
	return res;
};

/* ****************************************************************************************************************** */
/* RELOAD / REFRESH STUFF                                                                                             */
/* ****************************************************************************************************************** */

var ReloadGrid = function (grid, url)
{
	var gridElement = $("#" + grid);
	gridElement.clearGridData();
	if(url != undefined)
	{
		gridElement.setGridParam({url: url, page: 1});
	}
	gridElement.trigger("reloadGrid");
};

var RefreshGrid = function (count)
{
	// connected things every 2,5 seconds, waiting just every 25 seconds
	var mod = !!(count % 10 == 0);

	// every 5 minutes
	if (!!(count % 120 == 0))
	{
		UpdateSnapshots();
	}

	switch(activeTab)
	{
		case 0:
			// refresh bot grid
			$.each($("#bots_table").getDataIDs(), function(i, id)
			{
				var bot = GetRowData("bots_table", id);
				if(bot.State == 1 || (mod && bot.State == 2))
				{
					RefreshObject("bots_table", id);
				}
			});

			// refresh packet grid
			$.each($("#packets_table").getDataIDs(), function(i, id)
			{
				var pack = GetRowData("packets_table", id);
				if(pack.Connected || (mod && pack.Enabled))
				{
					RefreshObject("packets_table", id);
				}
			});
			break;

		case 1:
			break;

		case 2:
			$.each($("#files").getDataIDs(), function(i, id)
			{
				var file = GetRowData("files", id);

				var state = -1;
				$.each(file.Parts, function(i, part)
				{
					state = part.State;
					if (state == 0)
					{
						return false;
					}
				});

				if(state == 0)
				{
					RefreshObject("files", id);
				}
			});
			break;
	}

	setTimeout("RefreshGrid(" + (count + 1) +")", 2500);
};

var RefreshObject = function (grid, guid)
{
	$.getJSON(GuidUrl(Enum.TCPClientRequest.GetObject, guid),
		function(result)
		{
			result.cell.Object = JSON.stringify(result.cell);
			result.cell.Icon = "";
			
			if(grid == "packets_table")
			{
				result.cell.Speed = "";
				result.cell.TimeMissing = "";
			}

			$("#" + grid).setRowData(guid, result.cell);
		}
	);
};

var RefreshStatistic = function ()
{
	$.getJSON(JsonUrl() + Enum.TCPClientRequest.GetStatistics,
		function(result)
		{
			$("#BytesLoaded").html(Helper.size2Human(result.BytesLoaded));

			$("#PacketsCompleted").html(result.PacketsCompleted);
			$("#PacketsIncompleted").html(result.PacketsIncompleted);
			$("#PacketsBroken").html(result.PacketsBroken);

			$("#PacketsRequested").html(result.PacketsRequested);
			$("#PacketsRemoved").html(result.PacketsRemoved);

			$("#FilesCompleted").html(result.FilesCompleted);
			$("#FilesBroken").html(result.FilesBroken);

			$("#ServerConnectsOk").html(result.ServerConnectsOk);
			$("#ServerConnectsFailed").html(result.ServerConnectsFailed);

			$("#ChannelConnectsOk").html(result.ChannelConnectsOk);
			$("#ChannelConnectsFailed").html(result.ChannelConnectsFailed);
			$("#ChannelsJoined").html(result.ChannelsJoined);
			$("#ChannelsParted").html(result.ChannelsParted);
			$("#ChannelsKicked").html(result.ChannelsKicked);

			$("#BotConnectsOk").html(result.BotConnectsOk);
			$("#BotConnectsFailed").html(result.BotConnectsFailed);

			$("#SpeedMax").html(Helper.speed2Human(result.SpeedMax));
		}
	);
};

/* ****************************************************************************************************************** */
/* HELPER                                                                                                             */
/* ****************************************************************************************************************** */

var GetRowData = function (grid, id)
{
	return $.parseJSON($("#" + grid).getRowData(id).Object);
};

var ResizeMain = function ()
{
	/* left search tab */
	// set table
	$("#search_table")
		.setGridWidth($('#search_layout').width() - 2)
		.setGridHeight($('#search_layout').height() - 28);
	//$("#search_table").width($("#search_table").width() - 1);
	// patching table
	$($("#search_table .jqgfirstrow td")[2]).width("");
	// patch search input
	$("#search-text").width($("#search_layout").width() - 10);
	// patch divs
	$("#search_layout div").width("");

	/* main container */
	$("#layout_objects_container").height($('#search_layout').height() - 68);
	// bots + packets table
	$("#bots_table, #packets_table").setGridWidth($('#layout_objects').width() - 1);
	// patching table
	$($("#bots_table .jqgfirstrow td")[2]).width("");
	$("#bots_table_Name").width("");
	$($("#packets_table .jqgfirstrow td")[3]).width("");
	$("#packets_table_Name").width("");
	// patch divs
	$("#layout_objects_container div").width("");

	$("#searches_xg_bitpir_at, #files")
		.setGridWidth($('#layout_objects').width() - 1)
		.setGridHeight($('#search_div').height() - 110);

	innerLayout.resizeAll();
};

var ResizeContainer = function ()
{
	$("#bots_table").setGridHeight($('#bots_layout').height() - 74);
	$("#packets_table").setGridHeight($('#packets_layout').height() - 74);
};

var FlipPacket = function (id)
{
	var pack = GetRowData('packets_table', id);
	if(pack)
	{
		if(!pack.Enabled)
		{
			$("#" + id).effect("transfer", { to: $("#4") }, 500);

			$.get(GuidUrl(Enum.TCPClientRequest.ActivateObject, id));
			setTimeout("RefreshObject('packets_table', '" + id + "')", 1000);
		}
		else
		{
			$("#4").effect("transfer", { to: $("#" + id) }, 500);
			
			$.get(GuidUrl(Enum.TCPClientRequest.DeactivateObject, id));
			
			if (idSearch == 3 || idSearch == 3)
			{
				setTimeout("ReloadGrid('packets_table')", 1000);
			}
			else
			{
				setTimeout("RefreshObject('packets_table', '" + id + "')", 1000);
			}
		}
	}
};

var FlipObject = function (id, grid)
{
	var obj = GetRowData(grid, id);
	if(obj)
	{
		if(!obj.Enabled)
		{
			$.get(GuidUrl(Enum.TCPClientRequest.ActivateObject, id));
		}
		else
		{
			$.get(GuidUrl(Enum.TCPClientRequest.DeactivateObject, id));
		}
		setTimeout("ReloadGrid('" + grid + "')", 1000);
	}
	ReloadGrid(grid);
};

var DownloadLink = function (id)
{
	var data = GetRowData("searches_xg_bitpir_at", id);
	$.get(NameUrl(Enum.TCPClientRequest.ParseXdccLink, data.IrcLink));
};

var SetCookie = function (name, value)
{
	$.cookie('xg.' + name, value, { expires: 365 });
};

var GetCookie = function (name, value)
{
	var val = $.cookie('xg.' + name);
	return val != undefined && val != "" ? val : value;
};
