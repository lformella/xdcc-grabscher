//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

/* ****************************************************************************************************************** */
/* ENUM STUFF                                                                                                         */
/* ****************************************************************************************************************** */

function Enum() {}

Enum.TCPClientRequest =
{
	None : 0,
	Version : 1,

	AddServer : 2,
	RemoveServer : 3,
	AddChannel : 4,
	RemoveChannel : 5,

	ActivateObject : 6,
	DeactivateObject : 7,

	SearchPacket : 8,
	SearchPacketTime : 9,
	SearchPacketActiveDownloads : 10,
	SearchPacketsEnabled : 11,
	SearchBot : 12,
	SearchBotTime : 13,
	SearchBotActiveDownloads : 14,
	SearchBotsEnabled : 15,

	GetServers : 16,
	GetActivePackets : 17,
	GetFiles : 18,
	GetObject : 19,
	GetChildrenFromObject : 20,

	AddSearch : 21,
	RemoveSearch : 22,
	GetSearches : 23,

	GetStatistics : 24,

	CloseClient : 25,
	CloseServer : 26,

	ParseXdccLink : 27
};

Enum.TangoColor =
{
	Butter		: { Light : "fce94f", Middle : "edd400", Dark : "c4a000"},
	Orange		: { Light : "fcaf3e", Middle : "f57900", Dark : "ce5c00"},
	Chocolate	: { Light : "e9b96e", Middle : "c17d11", Dark : "8f5902"},
	Chameleon	: { Light : "8ae234", Middle : "73d216", Dark : "4e9a06"},
	SkyBlue		: { Light : "729fcf", Middle : "3465a4", Dark : "204a87"},
	Plum		: { Light : "ad7fa8", Middle : "75507b", Dark : "5c3566"},
	ScarletRed	: { Light : "ef2929", Middle : "cc0000", Dark : "a40000"},
	Aluminium1	: { Light : "eeeeec", Middle : "d3d7cf", Dark : "babdb6"},
	Aluminium2	: { Light : "888a85", Middle : "555753", Dark : "2e3436"}
};

/* ****************************************************************************************************************** */
/* GLOBAL VARS / FUNCTIONS                                                                                            */
/* ****************************************************************************************************************** */

var Password = "";

var id_server;
var id_search;
var last_search;

var search_active = false;

function JsonUrl(password) { return "/?password=" + (password != undefined ? escape(password) : escape(Password)) + "&offbots=" + ($("#offbots").attr('checked') ? "1" : "0" ) + "&request="; }
function GuidUrl(id, guid) { return JsonUrl() + id + "&guid=" + guid; }
function NameUrl(id, name) { return JsonUrl() + id + "&name=" + escape(name); }

var id_search_count = 1;

var LANG_MONTH = new Array("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December");
var LANG_WEEKDAY = new Array("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday");

var Formatter;
var Helper = new XGHelper();


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
		colNames: ['', 'Name', '', '', ''],
		colModel: [
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatServerIcon(r); }, width:24},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return r.Name; }, width:200, editable:true},
			{name:'ParentGuid',		index:'ParentGuid',		formatter: function(c, o, r) { return r.ParentGuid; }, hidden:true},
			{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return r.Connected; }, hidden:true},
			{name:'Enabled',		index:'Enabled',		formatter: function(c, o, r) { return r.Enabled; }, hidden:true}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				id_server = id;
				var serv = jQuery('#servers').getRowData(id);
				if(serv)
				{
					jQuery("#channels").setGridParam({url:GuidUrl(Enum.TCPClientRequest.GetChildrenFromObject, id), page:1}).trigger("reloadGrid");
				}
			}
		},
		ondblClickRow: function(id)
		{
			if(id)
			{
				var serv = jQuery('#servers').getRowData(id);
				if(serv)
				{
					if(serv.Enabled == "false")
					{
						$.get(GuidUrl(Enum.TCPClientRequest.ActivateObject, id));
					}
					else
					{
						$.get(GuidUrl(Enum.TCPClientRequest.DeactivateObject, id));
					}
					setTimeout("ReloadGrid('servers')", 1000);
				}
				ReloadGrid("servers");
			}
		},
		pager: jQuery('#server-pager'),
		rowNum: 100,
		pgbuttons: false,
		pginput: false,
		recordtext: '',
		pgtext: '',
		sortname: 'Name',
		ExpandColumn: 'Name',
		viewrecords: true,
		autowidth: true,
		scrollrows: true,
		height: 300,
		sortorder: "asc",
		caption: "Servers"
	}).navGrid('#server-pager', {edit:false, search:false}, {},
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
	jQuery("#servers").jqGrid('gridResize', {minWidth: 200, maxWidth: 200});

	/* ************************************************************************************************************** */
	/* CHANNEL GRID                                                                                                   */
	/* ************************************************************************************************************** */

	jQuery("#channels").jqGrid(
		{
			datatype: "json",
			colNames: ['', 'Name', '', '', ''],
			colModel: [
				{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatChannelIcon(r); }, width:24},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return r.Name; }, width:200, editable:true},
				{name:'ParentGuid',		index:'ParentGuid',		formatter: function(c, o, r) { return r.ParentGuid; }, hidden:true},
				{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return r.Connected; }, hidden:true},
				{name:'Enabled',		index:'Enabled',		formatter: function(c, o, r) { return r.Enabled; }, hidden:true}
			],
			ondblClickRow: function(id)
			{
				if(id)
				{
					var chan = jQuery('#channels').getRowData(id);
					if(chan)
					{
						if(chan.Enabled == "false")
						{
							$.get(GuidUrl(Enum.TCPClientRequest.ActivateObject, id));
						}
						else
						{
							$.get(GuidUrl(Enum.TCPClientRequest.DeactivateObject, id));
						}
						setTimeout("ReloadGrid('channels')", 1000);
					}
					ReloadGrid("channels");
				}
			},
			pager: jQuery('#channel-pager'),
			rowNum: 100,
			pgbuttons: false,
			pginput: false,
			recordtext: '',
			pgtext: '',
			sortname: 'Name',
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			height: 300,
			sortorder: "asc",
			caption: "channels"
		}).navGrid('#channel-pager', {edit:false, search:false}, {},
		{
			mtype: "GET",
			url: "/",
			serializeEditData: function (postdata)
			{
				return { password: escape(Password), request: Enum.TCPClientRequest.AddChannel, name: postdata.Name, guid: id_server };
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
	jQuery("#channels").jqGrid('gridResize', {minWidth: 300, maxWidth: 300});

	/* ************************************************************************************************************** */
	/* BOT GRID                                                                                                       */
	/* ************************************************************************************************************** */

	jQuery("#bots").jqGrid(
	{
		datatype: "json",
		cmTemplate:{fixed:true},
		colNames: ['', 'Name', 'Speed', 'Q-Pos', 'Q-Time', 'Speed', 'Slots', 'Queue', '', '', '', '', '', '', '', '', '', ''],
		colModel: [
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatBotIcon(r); }, width:24},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatBotName(r); }, width:370, fixed:false},
			{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return Helper.speed2Human(r.Speed); }, width:70, align:"right"},
			{name:'QueuePosition',	index:'QueuePosition',	formatter: function(c, o, r) { return r.QueuePosition > 0 ? r.QueuePosition : ""; }, width:70, align:"right"},
			{name:'QueueTime',		index:'QueueTime',		formatter: function(c, o, r) { return Helper.time2Human(r.QueueTime); }, width:70, align:"right"},
			{name:'SpeedMax',		index:'SpeedMax',		formatter: function(c, o, r) { return Formatter.formatBotSpeed(r); }, width:100, align:"right"},
			{name:'SlotTotal',		index:'SlotTotal',		formatter: function(c, o, r) { return Formatter.formatBotSlots(r); }, width:60, align:"right"},
			{name:'QueueTotal',		index:'QueueTotal',		formatter: function(c, o, r) { return Formatter.formatBotQueue(r); }, width:60, align:"right"},
			{name:'SlotCurrent',	index:'SlotCurrent',	formatter: function(c, o, r) { return r.SlotCurrent; }, hidden:true},
			{name:'SpeedCurrent',	index:'SpeedCurrent',	formatter: function(c, o, r) { return r.SpeedCurrent; }, hidden:true},
			{name:'BotState',		index:'BotState',		formatter: function(c, o, r) { return r.BotState; }, hidden:true},
			{name:'ParentGuid',		index:'ParentGuid',		formatter: function(c, o, r) { return r.ParentGuid; }, hidden:true},
			{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return r.Connected; }, hidden:true},
			{name:'Enabled',		index:'Enabled',		formatter: function(c, o, r) { return r.Enabled; }, hidden:true},
			{name:'LastModified',	index:'LastModified',	formatter: function(c, o, r) { return r.LastModified; }, hidden:true},
			{name:'QueueCurrent',	index:'QueueCurrent',	formatter: function(c, o, r) { return r.QueueCurrent; }, hidden:true},
			{name:'LastMessage',	index:'LastMessage',	formatter: function(c, o, r) { return r.LastMessage; }, hidden:true},
			{name:'LastContact',	index:'LastContact',	formatter: function(c, o, r) { return r.LastContact; }, hidden:true}
		],
		onHeaderClick: function(gridstate)
		{
			$.cookie('xg.bots', gridstate);
			Resize();
		},
		onSelectRow: function(id)
		{
			if(id)
			{
				search_active = false;
				var bot = jQuery('#bots').getRowData(id);
				jQuery("#packets").setGridParam({url:GuidUrl(Enum.TCPClientRequest.GetChildrenFromObject, id), page:1}).trigger("reloadGrid");
			}
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager: jQuery('#bot-pager'),
		sortname: 'Name',
		ExpandColumn: 'Name',
		viewrecords: true,
		autowidth: true,
		scrollrows: true,
		height: 300,
		sortorder: "asc",
		caption: "Bots"
	}).navGrid('#bot-pager', {edit:false, add:false, del:false, search:false});
	jQuery("#bots").setGridState($.cookie('xg.bots'));

	/* ************************************************************************************************************** */
	/* PACKET GRID                                                                                                    */
	/* ************************************************************************************************************** */

	jQuery("#packets").jqGrid(
	{
		datatype: "json",
		cmTemplate:{fixed:true},
		colNames: ['', 'Id', 'Name', 'Size', 'Speed', 'Time', 'Updated', '', '', '', '', '', '', '', '', ''],
		colModel: [
			{name:'Icon',			index:'Icon',			formatter: function(c, o, r) { return Formatter.formatPacketIcon(r) }, width:24},
			{name:'Id',				index:'Id',				formatter: function(c, o, r) { return Formatter.formatPacketId(r) }, width:40, align:"right"},
			{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatPacketName(r) }, width:400, fixed:false},
			{name:'Size',			index:'Size',			formatter: function(c, o, r) { return Helper.size2Human(r.Size); }, width:70, align:"right"},
			{name:'Speed',			index:'Speed',			formatter: function(c, o, r) { return Helper.speed2Human(r.Speed); }, width:70, align:"right"},
			{name:'TimeMissing',	index:'TimeMissing',	formatter: function(c, o, r) { return Helper.time2Human(r.TimeMissing); }, width:90, align:"right"},
			{name:'LastUpdated',	index:'LastUpdated',	formatter: function(c, o, r) { return r.LastUpdated; }, width:135, align:"right"},
			{name:'StartSize',		index:'StartSize',		formatter: function(c, o, r) { return r.StartSize; }, hidden:true},
			{name:'StopSize',		index:'StopSize',		formatter: function(c, o, r) { return r.StopSize; }, hidden:true},
			{name:'CurrentSize',	index:'CurrentSize',	formatter: function(c, o, r) { return r.CurrentSize; }, hidden:true},
			{name:'IsChecked',		index:'IsChecked',		formatter: function(c, o, r) { return r.IsChecked; }, hidden:true},
			{name:'Order',			index:'Order',			formatter: function(c, o, r) { return r.Order; }, hidden:true},
			{name:'ParentGuid',		index:'ParentGuid',		formatter: function(c, o, r) { return r.ParentGuid; }, hidden:true},
			{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return r.Connected; }, hidden:true},
			{name:'Enabled',		index:'Enabled',		formatter: function(c, o, r) { return r.Enabled; }, hidden:true},
			{name:'LastModified',	index:'LastModified',	formatter: function(c, o, r) { return r.LastModified; }, hidden:true}
		],
		onHeaderClick: function(gridstate)
		{
			$.cookie('xg.packets', gridstate);
			Resize();
		},
		onSelectRow: function(id)
		{
			if(id)
			{
				var pack = jQuery('#packets').getRowData(id);
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
				var pack = jQuery('#packets').getRowData(id);
				if(pack)
				{
					if(pack.Enabled == "false")
					{
						$.get(GuidUrl(Enum.TCPClientRequest.ActivateObject, id));
						setTimeout("RefreshPacket('" + id + "')", 1000);
					}
					else
					{
						$.get(GuidUrl(Enum.TCPClientRequest.DeactivateObject, id));
						setTimeout("ReloadGrid('packets')", 1000);
					}
				}				
			}
		},
		afterInsertRow: function(id, rowdata)
		{
			var pack = jQuery('#packets').getRowData(id);
			if(search_active && pack)
			{
				var color = GetColorByGuid(pack.ChannelGuid);
				jQuery('#packets').setCell(id, 'Id', '', {'background-color': '#' + color}, '');
			}
		},
		rowNum: 100,
		rowList: [100, 200, 400, 800],
		pager: jQuery('#packet-pager'),
		sortname: 'Id',
		ExpandColumn: 'Name',
		viewrecords: true,
		autowidth: true,
		height: 300,
		sortorder: "asc",
		caption: "Packets"
	}).navGrid('#packet-pager', {edit:false, add:false, del:false, search:false});
	jQuery("#packets").setGridState($.cookie('xg.packets'));

	/* ************************************************************************************************************** */
	/* SEARCH GRID                                                                                                    */
	/* ************************************************************************************************************** */

	jQuery("#searches").jqGrid(
	{
		datatype: "local",
		colNames: ['', 'Search'],
		colModel: [
			{name:'Id',		index:'Id',		formatter: function(c) { return Formatter.formatSearchIcon(c); }, width:24},
			{name:'Name',	index:'Name',	width:259}
		],
		onSelectRow: function(id)
		{
			search_active = true;
			if(id)
			{
				var data = jQuery('#searches').getRowData(id);
				var url1 = "";
				var url2 = "";
				switch(id)
				{
					case "1":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBotTime, "0-86400000");
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacketTime, "0-86400000");
						break;
					case "2":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBotTime, "0-604800000");
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacketTime, "0-604800000");
						break;
					case "3":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBotActiveDownloads, data.Name);
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacketActiveDownloads, data.Name);
						break;
					case "4":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBotsEnabled, data.Name);
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacketsEnabled, data.Name);
						break;
					default:
						url1 = NameUrl(Enum.TCPClientRequest.SearchBot, data.Name);
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacket, data.Name);
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
				id_search = id;
			}
		},
		ondblClickRow: function(id)
		{
			if(id)
			{
				if(id <= 4)
				{
					return;
				}
				var data = jQuery('#searches').getRowData(id);
				$.get(NameUrl(Enum.TCPClientRequest.RemoveSearch, data.Name));
				jQuery('#searches').delRowData(id);
			}
		},
		pager: jQuery('#searches-pager'),
		pgbuttons: false,
		pginput: false,
		recordtext: '',
		pgtext: '',
		sortname: 'name',
		ExpandColumn : 'name',
		viewrecords: true,
		height: 300,
		sortorder: "desc",
		caption: "Search"
	}).navGrid('#searches-pager', {edit:false, add:false, del:false, search:false, refresh:false});
	jQuery("#searches").jqGrid('gridResize', {minWidth: 200, maxWidth: 200});
	jQuery("#searches").setGridState($.cookie('xg.searches'));

	jQuery("#searches-pager_left").html("<input type=\"text\" id=\"search-text\" />");

	var mydata = [
		{Id:"1", Name:"ODay Packets"},
		{Id:"2", Name:"OWeek Packets"},
		{Id:"3", Name:"Downloads"},
		{Id:"4", Name:"Enabled Packets"}
	];
	for(var i=0; i<=mydata.length; i++)
	{
		jQuery("#searches").addRowData(i + 1, mydata[i]);
		id_search_count++;
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

	$("#search-text").width($("#gbox_searches").width() - 9);

	/******************************************************************************************************************/
	/* SEARCH GRID                                                                                                    */
	/******************************************************************************************************************/

	jQuery("#search-xg-bitpir-at").jqGrid(
		{
			datatype:'jsonp',
			cmTemplate:{fixed:true},
			colNames:['', 'Id', 'Name', 'Last Mentioned', 'Size', 'Bot', 'Speed', ''],
			colModel:[
				{name:'Connected',		index:'Connected',		formatter: function(c, o, r) { return Formatter.formatPacketIcon(r); }, width:26},
				{name:'Id',				index:'Id',				formatter: function(c, o, r) { return Formatter.formatPacketId(r); }, width:38, align:"right"},
				{name:'Name',			index:'Name',			formatter: function(c, o, r) { return Formatter.formatPacketName(r); }, fixed:false},
				{name:'LastMentioned',	index:'LastMentioned',	formatter: function(c, o, r) { return Helper.timeStampToHuman(r.LastMentioned); }, width:140, align:"right"},
				{name:'Size',			index:'Size',			formatter: function(c, o, r) { return Helper.size2Human(r.Size); }, width:60, align:"right"},
				{name:'BotName',		index:'BotName',		formatter: function(c, o, r) { return r.BotName; }, width:160},
				{name:'BotSpeed',		index:'BotSpeed',		formatter: function(c, o, r) { return Helper.speed2Human(r.BotSpeed); }, width:80, align:"right"},

				{name:'IrcLink',		index:'IrcLink',		formatter: function(c, o, r) { return r.IrcLink; }, hidden:true}
			],
			rowNum:20,
			rowList:[20, 40, 80, 160],
			pager:jQuery('#search-pager-xg-bitpir-at'),
			sortname:'Id',
			viewrecords:true,
			ExpandColumn:'Name',
			height:'100%',
			autowidth:true,
			sortorder:"asc",
			caption: "Search via xg.bitpir.at"
		}).navGrid('#search-pager-xg-bitpir-at', {edit:false, add:false, del:false, search:false});

	$("#search-input").keyup(function (e)
	{
		if (e.which == 13)
		{
			DoSearch($(this).val());
		}
	});

	/* ************************************************************************************************************** */
	/* PASSWORD DIALOG                                                                                                */
	/* ************************************************************************************************************** */

	$("#dialog_password").dialog({
		bgiframe: true,
		height: 200,
		modal: true,
		resizable: false,
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
		if (e.which == 13) {
			//$('[aria-labelledby$=dialog_password]').find(":button:contains('Connect')").click();
		}
	});

	/* ************************************************************************************************************** */
	/* SERVER / CHANNEL DIALOG                                                                                        */
	/* ************************************************************************************************************** */

	jQuery("#change-server-channels").button({icons: { primary: "ui-icon-gear" }});
	jQuery("#change-server-channels").click( function()
	{
		$("#dialog_server_channels").dialog("open");
	});

	$("#dialog_server_channels").dialog({
		bgiframe: true,
		autoOpen: false,
		width: 505,
		modal: true,
		resizable: false
	});

	/* ************************************************************************************************************** */
	/* RESIZE HELPER                                                                                                  */
	/* ************************************************************************************************************** */
	
	$(window).resize(function()
	{
		Resize();
	});
	Resize();

	/* ************************************************************************************************************** */
	/* OTHERS                                                                                                         */
	/* ************************************************************************************************************** */

	jQuery("#tabs").tabs();

	jQuery("#offbots").button();
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
	var datarow = {Id:id_search_count, Name:search};
	jQuery("#searches").addRowData(id_search_count, datarow);
	id_search_count++;
}

/* ****************************************************************************************************************** */
/* COLOR STUFF                                                                                                        */
/* ****************************************************************************************************************** */

var colors = new Array();
var color_count = 0;

function GetColorByGuid(guid)
{
	if(colors[guid] == undefined)
	{
		colors[guid] = GetColor(color_count);
		color_count += 3;
	}
	return colors[guid];
}

function GetColor(id)
{
	switch(id)
	{
		case 0: return Enum.TangoColor.Butter.Light;
		case 1: return Enum.TangoColor.Butter.Middle;
		case 2: return Enum.TangoColor.Butter.Dark;
		case 3: return Enum.TangoColor.Orange.Light;
		case 4: return Enum.TangoColor.Orange.Middle;
		case 5: return Enum.TangoColor.Orange.Dark;
		case 6: return Enum.TangoColor.Chocolate.Light;
		case 7: return Enum.TangoColor.Chocolate.Middle;
		case 8: return Enum.TangoColor.Chocolate.Dark;
		case 9: return Enum.TangoColor.Chameleon.Light;
		case 10: return Enum.TangoColor.Chameleon.Middle;
		case 11: return Enum.TangoColor.Chameleon.Dark;
		case 12: return Enum.TangoColor.SkyBlue.Light;
		case 13: return Enum.TangoColor.SkyBlue.Middle;
		case 14: return Enum.TangoColor.SkyBlue.Dark;
		case 15: return Enum.TangoColor.Plum.Light;
		case 16: return Enum.TangoColor.Plum.Middle;
		case 17: return Enum.TangoColor.Plum.Dark;
		case 18: return Enum.TangoColor.ScarletRed.Light;
		case 19: return Enum.TangoColor.ScarletRed.Middle;
		case 20: return Enum.TangoColor.ScarletRed.Dark;
		case 21: return Enum.TangoColor.Aluminium1.Light;
		case 22: return Enum.TangoColor.Aluminium1.Middle;
		case 23: return Enum.TangoColor.Aluminium1.Dark;
		case 24: return Enum.TangoColor.Aluminium2.Light;
		case 25: return Enum.TangoColor.Aluminium2.Middle;
		case 26: return Enum.TangoColor.Aluminium2.Dark;
	}
	return "";
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

		$("#tabs").show();

		// start the refresh
		RefreshGrid(0);
		RefreshStatistic();
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
	ReloadGrid("servers", GuidUrl(Enum.TCPClientRequest.GetServers, ''));
}

function CheckPassword(password)
{
	var res = false;
	jQuery.ajax({
		url: JsonUrl(password) + Enum.TCPClientRequest.Version,
		success: function(result)
		{
			res = !!(result != "");
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

	// refresh bot grid
	var ids = jQuery("#bots").getDataIDs();
	for (var i = 0; i < ids.length; i++)
	{
		var bot = jQuery("#bots").getRowData(ids[i]);
		if(bot.BotState == "Active" || (mod && bot.BotState == "Waiting"))
		{
			RefreshBot(ids[i]);
		}
	}

	// refresh packet grid
	ids = jQuery("#packets").getDataIDs();
	for (i = 0; i < ids.length; i++)
	{
		var pack = jQuery("#packets").getRowData(ids[i]);
		if(pack.Connected == "true" || (mod && pack.Enabled == "true"))
		{
			RefreshPacket(ids[i]);
		}
	}

	setTimeout("RefreshGrid(" + (count + 1) +")", 2500);
}

function RefreshBot(guid)
{
	$.getJSON(GuidUrl(Enum.TCPClientRequest.GetObject, guid),
		function(result)
		{
			jQuery("#bots").setRowData(result.id, result.cell);
		}
	);
}

function RefreshPacket(guid)
{
	$.getJSON(GuidUrl(Enum.TCPClientRequest.GetObject, guid),
		function(result)
		{
			jQuery("#packets").setRowData(result.id, result.cell);
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
			$("#SpeedMin").html(Helper.speed2Human(result.SpeedMin));
			$("#SpeedAvg").html(Helper.speed2Human(result.SpeedAvg));
		}
	);

	setTimeout("RefreshStatistic()", 10000);
}

/* ****************************************************************************************************************** */
/* HELPER                                                                                                             */
/* ****************************************************************************************************************** */

function Resize()
{
	var max_height = $(window).height() - 175;
	var max_width = $(window).width() - 25;

	jQuery("#searches").setGridHeight(max_height);

	jQuery("#search-xg-bitpir-at").setGridHeight(max_height);
	jQuery("#search-xg-bitpir-at").setGridWidth(max_width);

	var width = max_width - 305;
	var height = max_height / 2 - 32;

	if(jQuery("#packets").getGridParam("gridstate") == "hidden")
	{
		if(jQuery("#bots").getGridParam("gridstate") != "hidden")
		{
			jQuery("#bots").setGridHeight((height + 20) * 2);
		}
	}
	else if(jQuery("#bots").getGridParam("gridstate") != "hidden")
	{
		jQuery("#bots").setGridHeight(height);
	}
	jQuery("#bots").setGridWidth(width);
	jQuery("#bots").jqGrid('gridResize', {minWidth: width, maxWidth: width});

	if(jQuery("#bots").getGridParam("gridstate") == "hidden")
	{
		if(jQuery("#packets").getGridParam("gridstate") != "hidden")
		{
			jQuery("#packets").setGridHeight((height + 20) * 2);
		}
	}
	else if(jQuery("#packets").getGridParam("gridstate") != "hidden")
	{
		jQuery("#packets").setGridHeight(height);
	}
	jQuery("#packets").setGridWidth(width);
	jQuery("#packets").jqGrid('gridResize', {minWidth: width, maxWidth: width});
}

/**********************************************************************************************************************/
/* DO SOMETHING                                                                                                       */
/**********************************************************************************************************************/

function DoSearch (value)
{
	if (last_search != value)
	{
		last_search = value;

		jQuery("#search-xg-bitpir-at").clearGridData();
		jQuery("#search-xg-bitpir-at").setGridParam({url:"http://xg.bitpir.at/index.php?show=search&action=json&do=search_packets&searchString=" + value}).trigger("reloadGrid");
	}
}
