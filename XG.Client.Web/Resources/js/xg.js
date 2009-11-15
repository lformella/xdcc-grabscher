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

/* ************************************************************************** */
/* ENUM STUFF                                                                 */
/* ************************************************************************** */

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

	GetServersChannels : 16,
	GetActivePackets : 17,
	GetFiles : 18,
	GetObject : 19,
	GetChildrenFromObject : 20,

	CloseClient : 21,
	RestartServer : 22,
	CloseServer : 23
}
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
}

/* ************************************************************************** */
/* GLOBAL VARS / FUNCTIONS                                                    */
/* ************************************************************************** */

var Password = "";

var obj_current;
var id_server;
var id_channel;
var id_bot;
var id_search;

var search_active = false;

function JsonUrl(password) { return "/?password=" + (password != undefined ? password : Password) + "&request="; }
function GuidUrl(id, guid) { return JsonUrl() + id + "&guid=" + guid; }
function NameUrl(id, name) { return JsonUrl() + id + "&name=" + name; }
function GuidNameUrl(id, guid, name) { return JsonUrl() + id + "&guid=" + guid + "&name=" + name; }

/* ************************************************************************** */
/* GRID / FORM LOADER                                                         */
/* ************************************************************************** */

$(function()
{
	/* ********************************************************************** */
	/* SERVER / CHANNEL GRID                                                  */
	/* ********************************************************************** */

	jQuery("#servers").jqGrid(
	{
		datatype: "json",
		colNames:['', '', '', '', '', 'Name'],
		colModel:[
			{name:'parent',			index:'parent',			hidden:true},
			{name:'connected',		index:'connected',		hidden:true},
			{name:'enabled',		index:'enabled',		hidden:true},
			{name:'lastmodified',	index:'lastmodified',	hidden:true},
			{name:'icon',			index:'icon',			hidden:true},
			{name:'name',			index:'name',			width:200,	formatter:FormatServerIcon}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				var serv = jQuery('#servers').getRowData(id);
				obj_current = serv;
				if(serv && serv.level == 1)
				{
					if(id !== id_channel)
					{
						search_active = false;
						jQuery("#bots").setGridParam({url:GuidUrl(Enum.TCPClientRequest.GetChildrenFromObject, id)}).trigger("reloadGrid");
						id_channel = id;
						//jQuery("#add-channel").attr("disabled","disabled");
					}
					jQuery("#add-channel").hide();
				}
				else
				{
					id_server = id;
					//jQuery("#add-channel").removeAttr("disabled");
					jQuery("#add-channel").show();
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
					if(serv.enabled == "false")
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
		treeGrid: true,
		treeGridModel: 'adjacency',
		rowNum:100,
		rowList:[100, 200, 400, 800],
		//imgpath: gridimgpath,
		sortname: 'name',
		ExpandColumn: 'name',
		viewrecords: true,
		//autowidth: true,
		height: 400,
		sortorder: "asc",
		caption:"Servers"
	});

	/* ********************************************************************** */
	/* BOT GRID                                                               */
	/* ********************************************************************** */

	jQuery("#bots").jqGrid(
	{
		datatype: "json",
		colNames:['', '', '', '', '', 'Name', '', 'Speed', 'Q-Pos', 'Q-Time', 'Speed', '', 'Slots', '', 'Queue', '', '', ''],
		colModel:[
			{name:'parent',			index:'parent',			hidden:true},
			{name:'connected',		index:'connected',		hidden:true},
			{name:'enabled',		index:'enabled',		hidden:true},
			{name:'lastmodified',	index:'lastmodified',	hidden:true},
			{name:'icon',			index:'icon',			width:26,	formatter:FormatBotIcon},
			{name:'name',			index:'name',			width:370,	formatter:FormatBotName},
			{name:'botstate',		index:'botstate',		hidden:true},
			{name:'speed',			index:'speed',			width:70,	formatter:FormatSpeed, align:"right"},
			{name:'queueposition',	index:'queueposition',	width:70,	formatter:FormatInteger, align:"right"},
			{name:'queuetime',		index:'queuetime',		width:70,	formatter:FormatTime, align:"right"},
			{name:'speedmax',		index:'speedmax',		width:100,	formatter:FormatBotSpeed, align:"right"},
			{name:'speedcurrent',	index:'speedcurrent',	hidden:true},
			{name:'slottotal',		index:'slottotal',		width:60,	formatter:FormatBotSlots, align:"right"},
			{name:'slotcurrent',	index:'slotcurrent',	hidden:true},
			{name:'queuetotal',		index:'queuetotal',		width:60,	formatter:FormatBotQueue, align:"right"},
			{name:'queuecurrent',	index:'queuecurrent',	hidden:true},
			{name:'lastmessage',	index:'lastmessage',	hidden:true},
			{name:'lastcontact',	index:'lastcontact',	hidden:true}
		],
		onSelectRow: function(id)
		{
			if(id && id !== id_bot)
			{
				search_active = false;
				var bot = jQuery('#bots').getRowData(id);
				jQuery("#packets").setGridParam({url:GuidUrl(Enum.TCPClientRequest.GetChildrenFromObject, id)}).trigger("reloadGrid");
				id_bot = id;
				jQuery('#servers').setSelection(bot.parent, false);
			}
		},
		rowNum:100,
		rowList:[100, 200, 400, 800],
		//imgpath: gridimgpath,
		pager: jQuery('#bot-pager'),
		sortname: 'name',
		viewrecords: true,
		//rownumbers: true,
		//autowidth: true,
		scrollrows: true,
		//forceFit: true,
		//height: '100%',
		height: 300,
		sortorder: "asc",
		caption:"Bots"
	}).navGrid('#bot-pager',{edit:false,add:false,del:false,search:false});

	/* ********************************************************************** */
	/* PACKET GRID                                                            */
	/* ********************************************************************** */

	jQuery("#packets").jqGrid(
	{
		datatype: "json",
		colNames:['', '', '', '', '', '', 'Id', 'Name', 'Size', 'Speed', 'Time', '', '', '', 'Order', 'Lastupdated'],
		colModel:[
			{name:'parent',			index:'parent',			hidden:true},
			{name:'connected',		index:'connected',		hidden:true},
			{name:'enabled',		index:'enabled',		hidden:true},
			{name:'lastmodified',	index:'lastmodified',	hidden:true},
			{name:'icon',			index:'icon',			width:26,	formatter:FormatPacketIcon},
			{name:'channelguid',	index:'channelguid',	hidden:true},
			{name:'id',				index:'id',				width:40,	formatter:FormatPacketId, align:"right"},
			{name:'name',			index:'name',			width:400,	formatter:FormatPacketName},
			{name:'size',			index:'size',			width:70,	formatter:FormatSize, align:"right"},
			{name:'speed',			index:'speed',			width:70,	formatter:FormatSpeed, align:"right"},
			{name:'time',			index:'time',			width:90,	formatter:FormatTime, align:"right"},
			{name:'sizestart',		index:'sizestart',		hidden:true},
			{name:'sizestop',		index:'sizestop',		hidden:true},
			{name:'sizecur',		index:'sizecur',		hidden:true},
			{name:'order',			index:'order',			hidden:true},
			{name:'lastupdated',	index:'lastupdated',	width:130, align:"right"}
		],
		onSelectRow: function(id)
		{
			if(id)
			{
				var pack = jQuery('#packets').getRowData(id);
				if(pack)
				{
					var bot = jQuery('#bots').getRowData(pack.parent);
					if(bot)
					{
						jQuery('#bots').setSelection(pack.parent, false);
					}
					jQuery('#servers').setSelection(pack.channelguid, false);
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
					if(pack.enabled == "false")
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
		afterInsertRow: function(rowid, rowdata, rowelem)
		{
			if(search_active)
			{
				var color = GetColorByGuid(rowdata.channelguid);
				jQuery('#packets').setCell(rowid, 'id', '', {'background-color': '#' + color}, '');
			}
		},
		rowNum:100,
		rowList:[100, 200, 400, 800],
		//imgpath: gridimgpath,
		pager: jQuery('#packet-pager'),
		sortname: 'id',
		viewrecords: true,
		//rownumbers: true,
		//autowidth: true,
		height: 300,
		sortorder: "asc",
		caption:"Packets"
	}).navGrid('#packet-pager',{edit:false,add:false,del:false,search:false});

	/* ********************************************************************** */
	/* SEARCH GRID                                                            */
	/* ********************************************************************** */

	jQuery("#searches").jqGrid(
	{
		datatype: "local",
		colNames:['', 'Search'],
		colModel:[
			{name:'id',		index:'id',		width:26, formatter:FormatSearchIcon},
			{name:'name',	index:'name',	width:174}
		],
		onSelectRow: function(id)
		{
			search_active = true;
			if(id && id !== id_search)
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
						url1 = NameUrl(Enum.TCPClientRequest.SearchBotActiveDownloads, data.name);
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacketActiveDownloads, data.name);
						break;
					case "4":
						url1 = NameUrl(Enum.TCPClientRequest.SearchBotsEnabled, data.name);
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacketsEnabled, data.name);
						break;
					default:
						url1 = NameUrl(Enum.TCPClientRequest.SearchBot, data.name);
						url2 = NameUrl(Enum.TCPClientRequest.SearchPacket, data.name);
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
				jQuery('#searches').delRowData(id);
				//id_search_count--;
			}
		},
		//imgpath: gridimgpath,
		sortname: 'name',
		ExpandColumn : 'name',
		viewrecords: true,
		//autowidth: true,
		height: 'auto',
		sortorder: "desc",
		caption:"Search"
	});

	var id_search_count = 1;
	var mydata = [
		{id:"1",name:"ODay Packets"},
		{id:"2",name:"OWeek Packets"},
		{id:"3",name:"Downloads"},
		{id:"4",name:"Enabled Packets"}
	];
	for(var i=0;i<=mydata.length;i++)
	{
		jQuery("#searches").addRowData(i+1, mydata[i]);
		id_search_count++;
	}

	/* ********************************************************************** */
	/* SEARCH STUFF                                                           */
	/* ********************************************************************** */

	jQuery("#search-button").click( function()
	{
		AddNewSearch();
	});

	$("#search-text").keypress( function (e)
	{
		if (e.which == 13)
		{
			AddNewSearch();
		}
	});

	function AddNewSearch()
	{
		var tbox = jQuery('#search-text');
		if(tbox.val() != "")
		{
			var datarow = {id:id_search_count, name:tbox.val()};
			var su = jQuery("#searches").addRowData(id_search_count, datarow);
			id_search_count++;
			tbox.val('');
		}
	}

	$("#search-text").width($("#searches").width() - $("#search-button").width() - 20);

	/* ********************************************************************** */
	/* PASSWORD DIALOG                                                        */
	/* ********************************************************************** */

	$("#dialog_password").dialog({
		bgiframe: true,
		height: 160,
		modal: true,
		resizable: false,
		buttons: {
			'Connect': function()
			{
				var bValid = true;
				bValid = CheckPassword($("#password").val());

				if (bValid)
				{
					$("#password").removeClass('ui-state-error');
					SetPassword($("#password").val());
					$(this).dialog('close');
				}
				else
				{
					$("#password").addClass('ui-state-error');
				}
			}
		},
		close: function()
		{
			if(Password == "");
			{
				$('#dialog_password').dialog('open');
			}
			$("#password").val('').removeClass('ui-state-error');
		}
	});

	$("#password").keypress(function (e) {
		if (e.which == 13) {
			//$('[aria-labelledby$=dialog_password]').find(":button:contains('Connect')").click();
		}
	});

	/* ********************************************************************** */
	/* SERVER / CHANNEL DIALOG                                                */
	/* ********************************************************************** */

	jQuery("#add-server").click( function()
	{
		$("#dialog_server").dialog("open");
	});

	$("#dialog_server").dialog({
		bgiframe: true,
		autoOpen: false,
		height: 160,
		modal: true,
		resizable: false,
		buttons: {
			'Insert Server': function()
			{
				if($("#server").val() != "")
				{
					$.get(NameUrl(Enum.TCPClientRequest.AddServer, $("#server").val()));
					$("#server").val("");
					setTimeout("ReloadGrid('servers')", 1000);
					$(this).dialog('close');
				}
			},
			Cancel: function()
			{
				$(this).dialog('close');
			}
		},
		close: function()
		{
			//allFields.val('').removeClass('ui-state-error');
		}
	});

	jQuery("#add-channel").click( function()
	{
		$("#dialog_channel").dialog("open");
	});
	
	$("#dialog_channel").dialog({
		bgiframe: true,
		autoOpen: false,
		height: 160,
		modal: true,
		resizable: false,
		buttons: {
			'Insert Channel': function()
			{
				if($("#channel").val() != "")
				{
					$.get(GuidNameUrl(Enum.TCPClientRequest.AddChannel, id_server , $("#channel").val()));
					$("#channel").val("");
					setTimeout("ReloadGrid('servers')", 1000);
					$(this).dialog('close');
				}
			},
			Cancel: function()
			{
				$(this).dialog('close');
			}
		},
		close: function()
		{
			//allFields.val('').removeClass('ui-state-error');
		}
	});

	jQuery("#delete").click( function()
	{
		$("#dialog_delete").dialog("open");
	});
	
	$("#dialog_delete").dialog({
		bgiframe: true,
		autoOpen: false,
		height: 160,
		modal: true,
		resizable: false,
		buttons: {
			'Yes': function()
			{
				if(obj_current)
				{
					if(obj_current.level == 0)
					{
						$.get(GuidUrl(Enum.TCPClientRequest.RemoveServer, id_server));
					}
					else
					{
						$.get(GuidUrl(Enum.TCPClientRequest.RemoveChannel, id_channel));
					}
					setTimeout("ReloadGrid('servers')", 1000);
					obj_current = "";
					id_server = "";
					id_channel = "";

					$(this).dialog('close');
				}
			},
			'No': function()
			{
				$(this).dialog('close');
			}
		},
		close: function()
		{
			//allFields.val('').removeClass('ui-state-error');
		}
	});

	/* ********************************************************************** */
	/* START TIMER                                                            */
	/* ********************************************************************** */
	
	// start the refresh
	RefreshGrid();
});

/* ************************************************************************** */
/* COLOR STUFF                                                                */
/* ************************************************************************** */

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
}

/* ************************************************************************** */
/* PASSWORD STUFF                                                             */
/* ************************************************************************** */

function SetPassword(password)
{
	Password = password;
	ReloadGrid("servers", GuidUrl(Enum.TCPClientRequest.GetServersChannels, ''));
}

function CheckPassword(password)
{
	var res = false;
	jQuery.ajax({
		url: JsonUrl(password) + Enum.TCPClientRequest.Version,
		success: function(result)
		{
			res = result != "" ? true : false;
		},
		async: false
	});
	return res;
}

/* ************************************************************************** */
/* RELOAD / REFRESH STUFF                                                     */
/* ************************************************************************** */

function ReloadGrid(grid, url)
{
	if(url != undefined)
	{
		jQuery("#" + grid).setGridParam({url: url});
	}
	jQuery("#" + grid).trigger("reloadGrid");
}

function RefreshGrid()
{
	// refresh bot grid
	var ids = jQuery("#bots").getDataIDs();
	for each (var id in ids)
	{
		var data = jQuery("#bots").getRowData(id);
		if(data.botstate == "Active")
		{
			RefreshBot(id);
		}
	}

	// refresh packet grid
	var ids = jQuery("#packets").getDataIDs();
	for each (var id in ids)
	{
		var data = jQuery("#packets").getRowData(id);
		if(data.connected == "true")
		{
			RefreshPacket(id);
		}
	}

	setTimeout("RefreshGrid()", 2500);
}

function RefreshServer(guid)
{
	$.getJSON(GuidUrl(Enum.TCPClientRequest.GetObject, guid),
		function(result)
		{
			var data = new Array();
			data['parent'] = result.cell[0];
			data['connected'] = result.cell[1];
			data['enabled'] = result.cell[2];
			data['lastmodified'] = result.cell[3];
			data['icon'] = "";
			data['name'] = result.cell[5];
			var check = jQuery("#servers").setRowData(result.id, data);
			return;
		}
	);
}

function RefreshBot(guid)
{
	$.getJSON(GuidUrl(Enum.TCPClientRequest.GetObject, guid),
		function(result)
		{
			var data = new Array();
			data['parent'] = result.cell[0];
			data['connected'] = result.cell[1];
			data['enabled'] = result.cell[2];
			data['lastmodified'] = result.cell[3];
			data['icon'] = "";
			data['name'] = result.cell[5];
			data['botstate'] = result.cell[6];
			data['speed'] = result.cell[7];
			data['queueposition'] = result.cell[8];
			data['queuetime'] = result.cell[9];
			data['speedmax'] = result.cell[10];
			data['speedcurrent'] = result.cell[11];
			data['slottotal'] = result.cell[12];
			data['slotcurrent'] = result.cell[13];
			data['queuetotal'] = result.cell[14];
			data['queuecurrent'] = result.cell[15];
			data['lastmessage'] = result.cell[16];
			data['lastcontact'] = result.cell[17];
			var check = jQuery("#bots").setRowData(result.id, data);
			return;
		}
	);
}

function RefreshPacket(guid)
{
	$.getJSON(GuidUrl(Enum.TCPClientRequest.GetObject, guid),
		function(result)
		{
			var data = new Array();
			data['parent'] = result.cell[0];
			data['connected'] = result.cell[1];
			data['enabled'] = result.cell[2];
			data['lastmodified'] = result.cell[3];
			data['icon'] = "";
			data['channelguid'] = result.cell[5];
			data['id'] = result.cell[6];
			data['name'] = result.cell[7];
			data['size'] = result.cell[8];
			data['speed'] = result.cell[9];
			data['time'] = result.cell[10];
			data['sizestart'] = result.cell[11];
			data['sizestop'] = result.cell[12];
			data['sizecur'] = result.cell[13];
			data['order'] = result.cell[14];
			data['lastupdated'] = result.cell[15];
			var check = jQuery("#packets").setRowData(result.id, data);
			return;
		}
	);
}

/* ************************************************************************** */
/* OBJECT / ARRAY HELPER                                                      */
/* ************************************************************************** */

function Array2Packet(array)
{
	var data = new Object();
	data.parent = array[0];
	data.connected = array[1];
	data.enabled = array[2];
	data.lastmodified = array[3];
	data.icon = "";
	data.channelguid = array[5];
	data.id = array[6];
	data.name = array[7];
	data.size = array[8];
	data.speed = array[9];
	data.time = array[10];
	data.sizestart = array[11];
	data.sizestop = array[12];
	data.sizecur = array[13];
	data.order = array[14];
	data.lastupdated = array[15];
	return data;
}

function Array2Bot(array)
{
	var data = new Object();
	data.parent = array[0];
	data.connected = array[1];
	data.enabled = array[2];
	data.lastmodified = array[3];
	data.icon = "";
	data.name = array[5];
	data.botstate = array[6];
	data.speed = array[7];
	data.queueposition = array[8];
	data.queuetime = array[9];
	data.speedmax = array[10];
	data.speedcurrent = array[11];
	data.slottotal = array[12];
	data.slotcurrent = array[13];
	data.queuetotal = array[14];
	data.queuecurrent = array[15];
	data.lastmessage = array[16];
	data.lastcontact = array[17];
	return data;
}

/* ************************************************************************** */
/* SERVER FORMATER                                                            */
/* ************************************************************************** */

function FormatServerIcon(cellvalue, options, rowObject)
{
	var str = "";

	if(rowObject[6] == 0) { str += "Server"; }
	else { str += "Channel"; }

	if(!rowObject[2]) { str += "Disabled"; }
	else if(rowObject[1]) { str += "Connected"; }

	return FormatIcon(str) + " " + rowObject[5];
}

/* ************************************************************************** */
/* SEARCH FORMATER                                                            */
/* ************************************************************************** */

function FormatSearchIcon(cellvalue, options, rowObject)
{
	var str = "";
	switch(cellvalue)
	{
		case "1": str = "ODay"; break;
		case "2": str = "OWeek"; break;
		case "3": str = "BotDL0"; break;
		case "4": str = "Ok"; break;
		default: str = "Search"; break;
	}
	return FormatIcon(str);
}

/* ************************************************************************** */
/* BOT FORMATER                                                               */
/* ************************************************************************** */

function FormatBotIcon(cellvalue, options, rowObject)
{
	if(rowObject[0] != undefined) { rowObject = Array2Bot(rowObject); }

	var str = "Bot";

	if(!rowObject.connected) { str += "Off"; }
	else
	{
		switch(rowObject.botstate)
		{
			case "Idle": 
				if(rowObject.speedcurrent > 0)
				{
					if(rowObject.slotcurrent > 0) str += "Free";
					else if(rowObject.slotcurrent == 0) str += "Full";
				}
				break;
			case "Active":
				str += Speed2Image(rowObject.speed); break;
			case "Waiting":
				str += "Queued"; break;
		}
	}

	return FormatIcon(str);
}

function FormatBotName(cellvalue, options, rowObject)
{
	if(rowObject[0] != undefined) { rowObject = Array2Bot(rowObject); }

	var ret = "";
	ret += cellvalue;
	if(rowObject.botstate != "Idle" && rowObject.lastmessage != "")
	{
		ret += "<br /><small>" + rowObject.lastmessage + "</small>";
	}
	return ret;
}

function FormatBotSpeed(cellvalue, options, rowObject)
{
	if(rowObject[0] != undefined) { rowObject = Array2Bot(rowObject); }

	var ret = "";
	ret += rowObject.speedcurrent;
	ret += " / ";
	ret += rowObject.speedmax;
	return ret;
}

function FormatBotSlots(cellvalue, options, rowObject)
{
	if(rowObject[0] != undefined) { rowObject = Array2Bot(rowObject); }

	var ret = "";
	ret += rowObject.slotcurrent;
	ret += " / ";
	ret += rowObject.slottotal;
	return ret;
}

function FormatBotQueue(cellvalue, options, rowObject)
{
	if(rowObject[0] != undefined) { rowObject = Array2Bot(rowObject); }

	var ret = "";
	ret += rowObject.queuecurrent;
	ret += " / ";
	ret += rowObject.queuetotal;
	return ret;
}

/* ************************************************************************** */
/* PACKET FORMATER                                                            */
/* ************************************************************************** */

function FormatPacketIcon(cellvalue, options, rowObject)
{
	if(rowObject[0] != undefined) { rowObject = Array2Packet(rowObject); }

	var str = "Packet";

	if(!rowObject.enabled) { str += "Disabled"; }
	else
	{
		if(rowObject.connected) { str += Speed2Image(rowObject.speed); }
		else if (rowObject.order == 1) { str += "Queued"; }
		else { str += "New"; }
	}

	return FormatIcon(str);
}

function FormatPacketId(cellvalue, options, rowObject)
{
	return "#" + cellvalue;
}

function FormatPacketName(cellvalue, options, rowObject)
{
	if(rowObject[0] != undefined) { rowObject = Array2Packet(rowObject); }

	var ext = cellvalue.toLowerCase().substr(-3);
	var ret = "";
	if(ext == "avi" || ext == "wmv" || ext == "mkv")
	{
		ret += FormatIcon("ExtVideo") + "&nbsp;&nbsp;";
	}
	else if(ext == "mp3")
	{
		ret += FormatIcon("ExtAudio") + "&nbsp;&nbsp;";
	}
	else if(ext == "rar" || ext == "tar" || ext == "zip")
	{
		ret += FormatIcon("ExtCompressed") + "&nbsp;&nbsp;";
	}
	else
	{
		ret += FormatIcon("ExtDefault") + "&nbsp;&nbsp;";
	}

	if(cellvalue.toLowerCase().indexOf("german") > -1)
	{
		ret += FormatIcon("LanguageDe") + "&nbsp;&nbsp;";
	}

	ret += cellvalue;

	if(rowObject.connected)
	{
		ret += "<br />";

		var val = ((rowObject.sizecur - rowObject.sizestart) / (rowObject.sizestop - rowObject.sizestart)).toFixed(2) * 100;
		ret += "<div role='progressbar' class='ui-progressbar ui-widget ui-widget-content ui-corner-all' style='height:5px'><div style='width: " + val + "%' class='ui-progressbar-value ui-widget-header ui-corner-left'></div></div>";
	}


	return ret;
}

/* ************************************************************************** */
/* GLOBAL FORMATER                                                            */
/* ************************************************************************** */

function FormatIcon(img)
{
	return "<img src='image&" + img + "' />";
}

function FormatSize(cellvalue, options, rowObject)
{
	return Size2Human(cellvalue);
}

function FormatTime(cellvalue, options, rowObject)
{
	return Time2Human(cellvalue);
}

function FormatSpeed(cellvalue, options, rowObject)
{
	return Speed2Human(cellvalue);
}

function FormatInteger(cellvalue, options, rowObject)
{
	return cellvalue > 0 ? cellvalue : "";
}

/* ************************************************************************** */
/* HELPER                                                                     */
/* ************************************************************************** */

function Size2Human(size)
{
	if (size == 0) { return ""; }
	if (size < 1024) { return size + " B"; }
	else if (size < 1024 * 1024) { return (size / 1024).toFixed(2) + " KB"; }
	else if (size < 1024 * 1024 * 1024) { return (size / (1024 * 1024)).toFixed(2) + " MB"; }
	else { return (size / (1024 * 1024 * 1024)).toFixed(2) + " GB"; }
}

function Speed2Image(speed)
{
	if (speed < 1024 * 125) { return "DL0"; }
	else if (speed < 1024 * 250) { return "DL1"; }
	else if (speed < 1024 * 500) { return "DL2"; }
	else if (speed < 1024 * 750) { return "DL3"; }
	else if (speed < 1024 * 1000) { return "DL4"; }
	else { return "DL5"; }
}

function Speed2Human(speed)
{
	if (speed == 0) { return ""; }
	if (speed < 1024) { return speed + " B"; }
	else if (speed < 1024 * 1024) { return (speed / 1024).toFixed(2) + " KB"; }
	else { return (speed / (1024 * 1024)).toFixed(2) + " MB"; }
}

function Time2Human(time)
{
	var str = "";
	if(time < 0) { return str; }

	var buff = 0;

	if (time > 86400)
	{
		buff = Math.floor(time / 86400);
		str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
		time -= buff * 86400;
	}
	//else { str += "00:"; }

	if (time > 3600)
	{
		buff = Math.floor(time / 3600);
		str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
		time -= buff * 3600;
	}
	//else { str += "00:"; }

	if (time > 60)
	{
		buff = Math.floor(time / 60);
		str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
		time -= buff * 60;
	}
	//else { str += "00:"; }

	if (time > 0)
	{
		buff = time;
		str += (buff >= 10 ? "" + buff : "0" + buff);
		time -= buff;
	}
	//else { str += "00"; }

	return str;
}
