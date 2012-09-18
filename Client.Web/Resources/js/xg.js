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
	ParseXdccLink: 20,

	CloseServer: 21
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

var initialLoad = true;
var searchActive = false;
var activeTab = 0;

function JsonUrl(password) { return "/?password=" + (password != undefined ? encodeURIComponent(password) : encodeURIComponent(Password)) + "&offbots=" + ($("#show_offline_bots").attr('checked') ? "1" : "0" ) + "&request="; }
function GuidUrl(id, guid) { return JsonUrl() + id + "&guid=" + guid; }
function NameUrl(id, name) { return JsonUrl() + id + "&name=" + encodeURIComponent(name); }

var idSearchCount = 1;

var LANG_MONTH = new Array("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December");
var LANG_WEEKDAY = new Array("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday");

var Formatter;
var Helper = new XGHelper();

var outerLayout, innerLayout;


/* ****************************************************************************************************************** */
/* GRID / FORM LOADER                                                                                                 */
/* ****************************************************************************************************************** */

$(function()
{
	Formatter = new XGFormatter();

	/* ************************************************************************************************************** */
	/* SERVER GRID                                                                                                    */
	/* ************************************************************************************************************** */

	jQuery("#servers").jqGrid(
	{
		datatype: "json",
		colNames: ['', '', 'Name'],
		colModel: [
			{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return Formatter.formatServerIcon(r, "FlipObject(\"" + o.rowId + "\", \"servers\");"); }, width:24, sortable: false},
			{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:220, editable:true}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				idServer = id;
				var serv = GetRowData('servers', id);
				if(serv)
				{
					ReloadGrid("channels", GuidUrl(Enum.TCPClientRequest.GetChannelsFromServer, id));
				}
			}
		},
		ondblClickRow: function(id)
		{
			if(id)
			{
				FlipObject(id, "servers");
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('servers.sort.index', index);
			SetCookie('servers.sort.sortorder', sortorder);
		},
		pager: jQuery('#servers_pager'),
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

	jQuery("#channels").jqGrid(
	{
		datatype: "json",
		colNames: ['', '', 'Name'],
		colModel: [
			{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',	index:'Icon',	formatter: function(c, o, r) { return Formatter.formatChannelIcon(r, "FlipObject(\"" + o.rowId + "\", \"channels\");"); }, width:24, sortable: false},
			{name:'Name',	index:'Name',	formatter: function(c, o, r) { return r.Name; }, width:220, editable:true}
		],
		ondblClickRow: function(id)
		{
			if(id)
			{
				FlipObject(id, "channels");
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('channels.sort.index', index);
			SetCookie('channels.sort.sortorder', sortorder);
		},
		pager: jQuery('#channels_pager'),
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

	jQuery("#bots").jqGrid(
	{
		datatype: "json",
		cmTemplate:{fixed:true},
		colNames: ['', '', 'Name', 'Speed', 'Q-Pos', 'Q-Time', 'Speed', 'Slots', 'Queue'],
		colModel: [
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatBotIcon(r); }, width:24, sortable: false},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatBotName(r); }, width:370, fixed:false},
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
				ReloadGrid("packets", GuidUrl(Enum.TCPClientRequest.GetPacketsFromBot, id));
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('bots.sort.index', index);
			SetCookie('bots.sort.sortorder', sortorder);
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager: jQuery('#bots_pager'),
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

	jQuery("#packets").jqGrid(
	{
		datatype: "json",
		cmTemplate: {fixed:true},
		colNames: ['', '', 'Id', 'Name', 'Size', 'Speed', 'Time', 'Updated'],
		colModel: [
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatPacketIcon(r, "FlipPacket(\"" + o.rowId + "\");"); }, width:24, sortable: false},
			{name:'Id',				index:'Id',				formatter: function(c, o, r) { return Formatter.formatPacketId(r); }, width:40, align:"right"},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatPacketName(r); }, width:400, fixed:false},
			{name:'Size',			index:'Size',			formatter: function(c, o, r) { return Formatter.formatPacketSize(r); }, width:70, align:"right"},
			{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return Formatter.formatPacketSpeed(r); }, width:70, align:"right"},
			{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return Formatter.formatPacketTimeMissing(r) }, width:90, align:"right"},
			{name:'LastUpdated',	index:'LastUpdated',	formatter: function(c, o, r) { return r.LastUpdated; }, width:135, align:"right"}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				var pack = GetRowData('packets', id);
				if(pack)
				{
					jQuery('#bots').setSelection(pack.ParentGuid, false);
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
		pager: jQuery('#packets_pager'),
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

	jQuery("#searches").jqGrid(
	{
		datatype: "local",
		cmTemplate: {fixed:true},
		colNames: ['', '', '', ''],
		colModel: [
			{name:'Object',	index:'Object',	formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Id',		index:'Id',		formatter: function(c) { return Formatter.formatSearchIcon(c); }, width:30, sortable: false},
			{name:'Name',	index:'Name',	width:203, fixed:false, sortable: false},
			{name:'Action',	index:'Action',	width:17, sortable: false}
		],
		onSelectRow: function(id)
		{
			searchActive = true;
			if(id)
			{
				var data = jQuery("#searches").getRowData(id);
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
					ReloadGrid("bots", url1);
				}
				if(url2 != "")
				{
					ReloadGrid("packets", url2);
				}
				idSearch = id;
			}
		},
		afterInsertRow: function (id)
		{
			if (!initialLoad)
			{
				//$("#" + id).width($("#4").width());
				//$("#" + id).hide().show('slide', {}, 500);
				$("#search-text").effect("transfer", { to: $("#" + id) }, 500);
			}
		},
		pager: jQuery('#searches_pager'),
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
	}).navGrid('#searches_pager', {edit:false, add:false, del:false, search:false, refresh:false});

	jQuery("#searches_pager_left").html("<input type=\"text\" id=\"search-text\" />");

	var mydata = [
		{Id:"1", Name:"ODay Packets", Action: ""},
		{Id:"2", Name:"OWeek Packets", Action: ""},
		{Id:"3", Name:"Downloads", Action: ""},
		{Id:"4", Name:"Enabled Packets", Action: ""}
	];
	for(var i=0; i<=mydata.length; i++)
	{
		jQuery("#searches").addRowData(i + 1, mydata[i]);
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

	jQuery("#searches_xg_bitpir_at").jqGrid(
	{
		datatype:'jsonp',
		cmTemplate:{fixed:true},
		colNames:['', '', 'Id', 'Name', 'Last Mentioned', 'Size', 'Bot', 'Speed', ''],
		colModel:[
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return Formatter.formatPacketIcon(r, "DowloadLink(\"" + o.rowId + "\");", true); }, width:26, sortable: false},
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
				DowloadLink(id);
			}
		},
		onSortCol: function(index, iCol, sortorder)
		{
			SetCookie('searches_xg_bitpir_at.sort.index', index);
			SetCookie('searches_xg_bitpir_at.sort.sortorder', sortorder);
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager:jQuery('#searches_pager_xg_bitpir_at'),
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

	jQuery("#files").jqGrid(
	{
		datatype: "json",
		cmTemplate: {fixed:true},
		colNames: ['', '', 'Name', 'Size', 'Speed', 'Time'],
		colModel: [
			{name:'Object',			index:'Object',			formatter: function(c, o, r) { return JSON.stringify(r); }, hidden:true},
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatFileIcon(r); }, width:24, sortable: false},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatFileName(r); }, width:400, fixed:false},
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
		pager: jQuery('#files_pager'),
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
		height: 200,
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

	jQuery("#server_channel_button").button({icons: { primary: "ui-icon-gear" }});

	jQuery("#server_channel_button").click( function()
	{
		ReloadGrid("servers", GuidUrl(Enum.TCPClientRequest.GetServers, ''));
		ReloadGrid("channels");
		$("#dialog_server_channels").dialog("open");
	});

	$("#dialog_server_channels").dialog({
		bgiframe: true,
		autoOpen: false,
		width: 545,
		modal: true,
		resizable: false
	});

	/* ************************************************************************************************************** */
	/* STATISTICS DIALOG                                                                                              */
	/* ************************************************************************************************************** */

	jQuery("#statistics_button").button({icons: { primary: "ui-icon-comment" }});

	jQuery("#statistics_button").click( function()
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
	/* OTHERS                                                                                                         */
	/* ************************************************************************************************************** */

	jQuery("#tabs").tabs();
	$("#tabs").tabs({
		select: function(event, ui)
		{
			activeTab = ui.index;
		}
	});

	jQuery("#show_offline_bots").button();
});

/* ****************************************************************************************************************** */
/* SEARCH STUFF                                                                                                       */
/* ****************************************************************************************************************** */

function AddNewSearch()
{
	var tbox = jQuery('#search-text');
	if(tbox.val() != "")
	{
		$.get(NameUrl(Enum.TCPClientRequest.AddSearch, tbox.val()));
		AddSearch(tbox.val());
		tbox.val('');
	}
}

function AddSearch(search)
{
	var datarow =
	{
		Id: idSearchCount,
		Name: search,
		Action: "<div class='remove_button' onclick='RemoveSearch(" + idSearchCount + ");'></div>"
	};
	jQuery("#searches").addRowData(idSearchCount, datarow);
	idSearchCount++;
}

function RemoveSearch(id)
{
	if(id <= 4)
	{
		return;
	}
	var data = jQuery("#searches").getRowData(id);
	$.get(NameUrl(Enum.TCPClientRequest.RemoveSearch, data.Name));
	//$("#" + id).width($("#4").width());
	//$("#" + id).hide('slide', {}, 500);
	$("#" + id).effect("transfer", { to: $("#search-text") }, 500);
	jQuery('#searches').delRowData(id);
}

/* ****************************************************************************************************************** */
/* DIALOG BUTTON HANDLER                                                                                              */
/* ****************************************************************************************************************** */

function ButtonConnectClicked(dialog)
{
	var bValid = CheckPassword($("#password").val());

	if (bValid)
	{
		$("#password").removeClass('ui-state-error');
		SetPassword($("#password").val());
		dialog.dialog('close');

		// Get searches
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
			spacing_open: 1,
			spacing_closed: 4
		});

		innerLayout = $("#layout_objects_container").layout({
			resizeWithWindow: false,
			onresize: function () {
				ResizeContainer();
			},
			spacing_open: 1,
			spacing_closed: 4
		});

		// resize after all is visible
		ResizeMain();
		// double resize because the first run wont change all values :|
		ResizeMain();

		// start the refresh
		RefreshGrid(0);
		
		initialLoad = false;
	}
	else
	{
		$("#password").addClass('ui-state-error');
	}
}

/* ****************************************************************************************************************** */
/* PASSWORD STUFF                                                                                                     */
/* ****************************************************************************************************************** */

function SetPassword(password)
{
	Password = password;
}

function CheckPassword(password)
{
	var res = false;
	jQuery.ajax({
		url: JsonUrl(password) + Enum.TCPClientRequest.Version,
		success: function(result)
		{
			res = true;
		},
		async: false
	});
	return res;
}

/* ****************************************************************************************************************** */
/* RELOAD / REFRESH STUFF                                                                                             */
/* ****************************************************************************************************************** */

function ReloadGrid(grid, url)
{
	jQuery("#" + grid).clearGridData();
	if(url != undefined)
	{
		jQuery("#" + grid).setGridParam({url: url, page: 1});
	}
	jQuery("#" + grid).trigger("reloadGrid");
}

function RefreshGrid(count)
{
	// connected things every 2,5 seconds, waiting just every 25 seconds
	var mod = !!(count % 10 == 0);

	switch(activeTab)
	{
		case 0:
			// refresh bot grid
			$.each(jQuery("#bots").getDataIDs(), function(i, id)
			{
				var bot = GetRowData("bots", id);
				if(bot.State == 1 || (mod && bot.State == 2))
				{
					RefreshObject("bots", id);
				}
			});

			// refresh packet grid
			$.each(jQuery("#packets").getDataIDs(), function(i, id)
			{
				var pack = GetRowData("packets", id);
				if(pack.Connected || (mod && pack.Enabled))
				{
					RefreshObject("packets", id);
				}
			});
			break;

		case 1:
			break;

		case 2:
			$.each(jQuery("#files").getDataIDs(), function(i, id)
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
}

function RefreshObject(grid, guid)
{
	$.getJSON(GuidUrl(Enum.TCPClientRequest.GetObject, guid),
		function(result)
		{
			result.cell.Object = JSON.stringify(result.cell);
			result.cell.Icon = "";
			
			if(grid == "packets")
			{
				result.cell.Speed = "";
				result.cell.TimeMissing = "";
			}

			jQuery("#" + grid).setRowData(guid, result.cell);
		}
	);
}

function RefreshStatistic()
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
}

/* ****************************************************************************************************************** */
/* HELPER                                                                                                             */
/* ****************************************************************************************************************** */

function GetRowData(grid, id)
{
	return jQuery.parseJSON(jQuery("#" + grid).getRowData(id).Object);
}

function ResizeMain()
{
	jQuery("#searches").setGridWidth($('#layout_search').width() - 1);
	jQuery("#searches").setGridHeight($('#layout_search').height() - 28);

	$("#search-text").width($("#layout_search").width() - 10);

	$("#layout_objects_container").height($('#layout_search').height() - 66);

	jQuery("#bots, #packets").setGridWidth($('#layout_objects').width() - 1);

	jQuery("#searches_xg_bitpir_at, #files").setGridWidth($('#layout_objects').width() - 1);
	jQuery("#searches_xg_bitpir_at, #files").setGridHeight($('#layout_search').height() - 110);

	innerLayout.resizeAll();
}

function ResizeContainer()
{
	jQuery("#bots").setGridHeight($('#layout_bots').height() - 74);
	jQuery("#packets").setGridHeight($('#layout_packets').height() - 74);
}

function FlipPacket(id)
{
	var pack = GetRowData('packets', id);
	if(pack)
	{
		if(!pack.Enabled)
		{
			$("#" + id).effect("transfer", { to: $("#4") }, 500);

			$.get(GuidUrl(Enum.TCPClientRequest.ActivateObject, id));
			setTimeout("RefreshObject('packets', '" + id + "')", 1000);
		}
		else
		{
			$("#4").effect("transfer", { to: $("#" + id) }, 500);
			
			$.get(GuidUrl(Enum.TCPClientRequest.DeactivateObject, id));
			
			if (idSearch == 3 || idSearch == 3)
			{
				setTimeout("ReloadGrid('packets')", 1000);
			}
			else
			{
				setTimeout("RefreshObject('packets', '" + id + "')", 1000);
			}
		}
	}				
}

function FlipObject(id, grid)
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
}

function DowloadLink(id)
{
	var data = GetRowData("searches_xg_bitpir_at", id);
	$.get(NameUrl(Enum.TCPClientRequest.ParseXdccLink, data.IrcLink));
}

function SetCookie(name, value)
{
	$.cookie('xg.' + name, value, { expires: 365 });
}

function GetCookie(name, value)
{
	var val = $.cookie('xg.' + name);
	return val != undefined && val != "" ? val : value;
}
