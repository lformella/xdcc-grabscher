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

var Version					= 1;

var AddServer				= 2;
var RemoveServer			= 3;
var AddChannel				= 4;
var RemoveChannel			= 5;

var ActivateObject			= 6;
var DeactivateObject		= 7;

var SearchPacket			= 8;
var SearchPacketTime		= 9;
var SearchBot				= 10;
var SearchBotTime			= 11;

var GetServersChannels		= 12;
var GetActivePackets		= 13;
var GetFiles				= 14;
var GetObject				= 15;
var GetChildrenFromObject	= 16;

var CloseClient				= 17;
var RestartServer			= 18;
var CloseServer				= 19;

var Password = "";

var id_server;
var id_bot;
var id_search;

var search_active = false;

function JsonUrl(password) { return "/?password=" + (password != undefined ? password : Password) + "&request="; }
function GuidUrl(id, guid) { return JsonUrl() + id + "&guid=" + guid; }
function NameUrl(id, name) { return JsonUrl() + id + "&name=" + name; }


$(function()
{
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
			if(id && id !== id_server)
			{
				search_active = false;
				var serv = jQuery('#servers').getRowData(id);
				if(serv && serv.level == 1)
				{
					jQuery("#bots").setGridParam({url:GuidUrl(GetChildrenFromObject, id)}).trigger("reloadGrid");
					id_server = id;
					jQuery("#add-channel").enabled = false;
				}
				else
				{
					jQuery("#add-channel").enabled = true;
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
						$.get(GuidUrl(ActivateObject, id));
					}
					else
					{
						$.get(GuidUrl(DeactivateObject, id));
					}
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

	jQuery("#bots").jqGrid(
	{
		datatype: "json",
		colNames:['', '', '', '', '', 'Name', '', 'Speed', '', '', 'Queue Pos', 'Queue Time', 'Speed', '', 'Slots', '', 'Queue', ''],
		colModel:[
			{name:'parent',			index:'parent',			hidden:true},
			{name:'connected',		index:'connected',		hidden:true},
			{name:'enabled',		index:'enabled',		hidden:true},
			{name:'lastmodified',	index:'lastmodified',	hidden:true},
			{name:'icon',			index:'icon',			width:26,	formatter:FormatBotIcon},
			{name:'name',			index:'name',			width:270},
			{name:'botstate',		index:'botstate',		hidden:true},
			{name:'speed',			index:'speed',			width:70,	formatter:FormatSpeed},
			{name:'lastmessage',	index:'lastmessage',	hidden:true},
			{name:'lastcontact',	index:'lastcontact',	hidden:true},
			{name:'queueposition',	index:'queueposition',	width:80},
			{name:'queuetime',		index:'queuetime',		width:80},
			{name:'speedmax',		index:'speedmax',		width:100},
			{name:'speecurrent',	index:'speecurrent',	hidden:true},
			{name:'slottotal',		index:'slottotal',		width:100},
			{name:'slotcurrent',	index:'slotcurrent',	hidden:true},
			{name:'queuetotal',		index:'queuetotal',		width:100},
			{name:'queuecurrent',	index:'queuecurrent',	hidden:true}
		],
		onSelectRow: function(id)
		{
			if(id && id !== id_bot)
			{
				search_active = false;
				var bot = jQuery('#bots').getRowData(id);
				jQuery("#packets").setGridParam({url:GuidUrl(GetChildrenFromObject, id)}).trigger("reloadGrid");
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
		rownumbers: true,
		//autowidth: true,
		scrollrows: true,
		//forceFit: true,
		//height: '100%',
		height: 300,
		sortorder: "asc",
		caption:"Bots"
	}).navGrid('#bot-pager',{edit:false,add:false,del:false,search:false});

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
			{name:'size',			index:'size',			width:70,	formatter:FormatPacketSize, align:"right"},
			{name:'speed',			index:'speed',			width:70,	formatter:FormatSpeed},
			{name:'time',			index:'time',			width:90,	formatter:FormatPacketTime},
			{name:'sizestart',		index:'sizestart',		hidden:true},
			{name:'sizestop',		index:'sizestop',		hidden:true},
			{name:'sizecur',		index:'sizecur',		hidden:true},
			{name:'order',			index:'order',			hidden:true},
			{name:'lastupdated',	index:'lastupdated',	width:130}
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
						jQuery('#servers').setSelection(bot.parent, false);
					}
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
						$.get(GuidUrl(ActivateObject, id));
					}
					else
					{
						$.get(GuidUrl(DeactivateObject, id));
					}
				}
				ReloadGrid("packets");
			}
		},
		afterInsertRow: function(rowid, rowdata, rowelem)
		{
			if(search_active)
			{
				// TODO create color sets for each channel
			}
		},
		rowNum:100,
		rowList:[100, 200, 400, 800],
		//imgpath: gridimgpath,
		pager: jQuery('#packet-pager'),
		sortname: 'id',
		viewrecords: true,
		rownumbers: true,
		//autowidth: true,
		height: 300,
		sortorder: "asc",
		caption:"Packets"
	}).navGrid('#packet-pager',{edit:false,add:false,del:false,search:false});

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
						url1 = NameUrl(SearchBotTime, "0-86400000");
						url2 = NameUrl(SearchPacketTime, "0-86400000");
						break;
					case "2":
						url1 = NameUrl(SearchBotTime, "0-604800000");
						url2 = NameUrl(SearchPacketTime, "0-604800000");
						break;
					case "3":
						url1 = "";
						url2 = "";
						break;
					case "4":
						url1 = "";
						url2 = "";
						break;
					default:
						url1 = NameUrl(SearchBot, data.name);
						url2 = NameUrl(SearchPacket, data.name);
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

	jQuery("#search-button").click( function()
	{
		var tbox = jQuery('#search-text');
		if(tbox.val() != "")
		{
			var datarow = {id:id_search_count,name:tbox.val()};
			var su = jQuery("#searches").addRowData(id_search_count, datarow);
			id_search_count++;
			tbox.val('');
		}
	});

	$("#dialog_password").dialog({
		bgiframe: true,
		height: 140,
		modal: true,
		resizable: false,
		buttons: {
			'Connect': function() {
				var bValid = true;
				bValid = CheckPassword($("#password").val());

				if (bValid) {
					$("#password").removeClass('ui-state-error');
					SetPassword($("#password").val());
					$(this).dialog('close');
				}
				else {
					$("#password").addClass('ui-state-error');
				}
			}
		},
		close: function() {
			if(Password == "");
			{
				$('#dialog_password').dialog('open');
			}
			$("#password").val('').removeClass('ui-state-error');
		}
	});
	
	$("#dialog_server").dialog({
		bgiframe: true,
		autoOpen: false,
		height: 300,
		modal: true,
		buttons: {
			'Insert Server': function() {
				$(this).dialog('close');
			},
			Cancel: function() {
				$(this).dialog('close');
			}
		},
		close: function() {
			//allFields.val('').removeClass('ui-state-error');
		}
	});
	
	$("#dialog_channel").dialog({
		bgiframe: true,
		autoOpen: false,
		height: 300,
		modal: true,
		buttons: {
			'Insert Channel': function() {
				$(this).dialog('close');
			},
			Cancel: function() {
				$(this).dialog('close');
			}
		},
		close: function() {
			//allFields.val('').removeClass('ui-state-error');
		}
	});
});

/* ************************************************************************** */

function SetPassword(password)
{
	Password = password;
	ReloadGrid("servers", GuidUrl(GetServersChannels, ''));
}

function CheckPassword(password)
{
	var res = false;
	jQuery.ajax({
		url: JsonUrl(password) + Version,
		success: function(result)
		{
			res = result != "" ? true : false;
		},
		async: false
	});
	return res;
}

function ReloadGrid(grid, url)
{
	if(url != undefined)
	{
		jQuery("#" + grid).setGridParam({url: url});
	}
	jQuery("#" + grid).trigger("reloadGrid");
}

/* ************************************************************************** */

function FormatIcon(img)
{
	return "<img src='image&" + img + "'>";
}

function FormatServerIcon(cellvalue, options, rowObject)
{
	var str = "";

	if(rowObject[6] == 0) { str += "Server"; }
	else { str += "Channel"; }

	if(!rowObject[2]) { str += "Disabled"; }
	else if(rowObject[1]) { str += "Connected"; }

	return FormatIcon(str) + " " + rowObject[5];
}

function FormatBotIcon(cellvalue, options, rowObject)
{
	var str = "Bot";

	if(!rowObject[1]) { str += "Off"; }
	else
	{
		switch(rowObject[6])
		{
			case "Idle": 
				if(rowObject[11] > 0)
				{
					if(rowObject[10] > 0) str += "Free";
					else if(rowObject[10] == 0) str += "Full";
				}
				break;
			case "Active": str += Speed2Image(rowObject[7]); break;
			case "Waiting": str += "Queued"; break;
		}
	}

	return FormatIcon(str);
}

function FormatPacketIcon(cellvalue, options, rowObject)
{
	var str = "Packet";

	if(!rowObject[2]) { str += "Disabled"; }
	else
	{
		if(rowObject[1]) { str += Speed2Image(rowObject[9]); }
		else if (rowObject[14] == 1) { str += "Queued"; }
		else { str += "New"; }
	}

	return FormatIcon(str);
}

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

function FormatPacketId(cellvalue, options, rowObject)
{
	return "#" + cellvalue;
}

function FormatPacketName(cellvalue, options, rowObject)
{
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
	return ret;
}

function FormatPacketSize(cellvalue, options, rowObject)
{
	return Size2Human(cellvalue);
}

function FormatPacketTime(cellvalue, options, rowObject)
{
	return Time2Human(cellvalue);
}

function FormatSpeed(cellvalue, options, rowObject)
{
	return Speed2Human(cellvalue);
}

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
	if (speed < 1024) { return speed.toFixed(2) + " B"; }
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
		buff = (time / 86400);
		str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
		time -= buff * 86400;
	}
	//else { str += "00:"; }

	if (time > 3600)
	{
		buff = (time / 3600);
		str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
		time -= buff * 3600;
	}
	//else { str += "00:"; }

	if (time > 60)
	{
		buff = (time / 60);
		str += (buff >= 10 ? "" + buff : "0" + buff) + ":";
		time -= buff * 60;
	}
	//else { str += "00:"; }

	if (time > 0)
	{
		buff = time;
		str += buff >= 10 ? "" + buff : "0" + buff;
		time -= buff;
	}
	//else { str += "00"; }

	return str;
}
