//
//  main.js
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
	 * @param {XGHelper} helper
	 * @param {XGStatistics} statistics
	 * @param {XGCookie} cookie
	 * @param {XGFormatter} formatter
	 * @param {XGWebsocket} websocket
	 */
	initialize: function(helper, statistics, cookie, formatter, websocket)
	{
		XG = this;
		var self = this;

		this.statistics = statistics;
		this.cookie = cookie;
		this.helper = helper;
		this.formatter = formatter;
		this.websocket = websocket;

		this.idServer = 0;
		this.activeTab = 0;

		this.initializeGrids();
		this.initializeDialogs();
		this.initializeOthers();
	},

	initializeGrids: function()
	{
		var self = this;

		/* ********************************************************************************************************** */
		/* SERVER GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#servers_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name')],
			colModel: [
				{
					name: 'Object',
					index: 'Object',
					formatter: function (cell, grid, obj)
					{
						return JSON.stringify(obj);
					},
					hidden: true
				},
				{
					name: 'Icon',
					index: 'Icon',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatServerIcon(obj, "XG.flipObject(\"" + obj.Guid + "\", \"servers_table\");");
					},
					width: 38,
					sortable: false,
					classes: "icon-cell"
				},
				{
					name: 'Name',
					index: 'Name',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatServerChannelName(obj);
					},
					width: 214,
					editable: true,
					fixed: false
				}
			],
			onSelectRow: function(guid)
			{
				self.idServer = guid;
				var server = self.websocket.getRowData("servers_table", guid);
				self.websocket.sendGuid(Enum.Request.ChannelsFromServer, server.Guid);
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('servers.sort.index', index);
				self.cookie.setCookie('servers.sort.sortorder', sortorder);
			},
			pager: $('#servers_pager'),
			pgbuttons: false,
			pginput: false,
			ExpandColumn: 'Name',
			viewrecords: true,
			width: 400,
			scrollrows: true,
			hidegrid: false,
			height: 400,
			rowNum: 999999999,
			sortname: self.cookie.getCookie('servers.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('servers.sort.sortorder', 'asc'),
			caption: _("Servers")
		}).navGrid('#servers_pager', {edit:false, search:false, refresh:false}, {},
		{
			processing: true,
			reloadAfterSubmit: false,
			savekey: [true,13],
			closeOnEscape: true,
			closeAfterAdd: true,
			onclickSubmit: function (options, obj)
			{
				self.websocket.sendName(Enum.Request.AddServer, obj.Name);
				return {};
			}
		},
		{
			processing: true,
			closeOnEscape: true,
			onclickSubmit: function(options, guid)
			{
				self.websocket.sendGuid(Enum.Request.RemoveServer, guid);
				return true;
			}
		})
		.navButtonAdd('#servers_pager',{
			caption:"",
			buttonicon:"loading-symbol",
			id: "servers_loading"
		});
		$("#servers_loading .ui-icon").hide();

		/* ********************************************************************************************************** */
		/* CHANNEL GRID                                                                                               */
		/* ********************************************************************************************************** */

		$("#channels_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name')],
			colModel: [
				{
					name: 'Object',
					index: 'Object',
					formatter: function (cell, grid, obj)
					{
						return JSON.stringify(obj);
					},
					hidden: true
				},
				{
					name: 'Icon',
					index: 'Icon',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatChannelIcon(obj, "XG.flipObject(\"" + obj.Guid + "\", \"channels_table\");");
					},
					width: 40,
					sortable: false,
					classes: "icon-cell"
				},
				{
					name: 'Name',
					index: 'Name',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatServerChannelName(obj);
					},
					width: 212,
					editable: true,
					fixed: false
				}
			],
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('channels.sort.index', index);
				self.cookie.setCookie('channels.sort.sortorder', sortorder);
			},
			pager: $('#channels_pager'),
			pgbuttons: false,
			pginput: false,
			ExpandColumn: 'Name',
			viewrecords: true,
			width: 400,
			scrollrows: true,
			hidegrid: false,
			height: 400,
			rowNum: 999999999,
			sortname: self.cookie.getCookie('channels.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('channels.sort.sortorder', 'asc'),
			caption: _("Channels")
		}).navGrid('#channels_pager', {edit:false, search:false, refresh:false}, {},
		{
			processing: true,
			reloadAfterSubmit: false,
			savekey: [true,13],
			closeOnEscape: true,
			closeAfterAdd: true,
			onclickSubmit: function (options, obj)
			{
				self.websocket.sendNameGuid(Enum.Request.AddChannel, obj.Name, self.idServer);
				return {};
			}
		},
		{
			processing: true,
			closeOnEscape: true,
			onclickSubmit: function(options, guid)
			{
				self.websocket.sendGuid(Enum.Request.RemoveChannel, guid);
				return true;
			}
		})
		.navButtonAdd('#channels_pager',{
			caption:"",
			buttonicon:"loading-symbol",
			id: "channels_loading"
		});
		$("#channels_loading .ui-icon").hide();

		/* ********************************************************************************************************** */
		/* BOT GRID                                                                                                   */
		/* ********************************************************************************************************** */

		$("#bots_table").jqGrid(
		{
			datatype: "local",
			cmTemplate:{fixed:true},
			colNames: ['', '', _('Name'), _('Speed'), _('Q-Pos'), _('Q-Time'), _('max Speed'), _('Slots'), _('Queue')],
			colModel: [
				{
					name: 'Object',
					index: 'Object',
					formatter: function (cell, grid, obj)
					{
						return JSON.stringify(obj);
					},
					hidden: true
				},
				{
					name: 'Icon',
					index: 'Icon',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatBotIcon(obj);
					},
					width: 34,
					sortable: false,
					classes: "icon-cell"
				},
				{
					name: 'Name',
					index: 'Name',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatBotName(obj);
					},
					fixed: false
				},
				{
					name: 'Speed',
					index: 'Speed',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.helper.speed2Human(obj.Speed);
					},
					sorttype: function (c, o)
					{
						return o.Speed;
					},
					width: 70,
					align: "right"
				},
				{
					name: 'QueuePosition',
					index: 'QueuePosition',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return obj.QueuePosition > 0 ? obj.QueuePosition : "&nbsp;";
					},
					sorttype: function (c, o)
					{
						return o.QueuePosition > 0 ? o.QueuePosition : 0;
					},
					width: 70,
					align: "right"
				},
				{
					name: 'QueueTime',
					index: 'QueueTime',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.helper.time2Human(obj.QueueTime);
					},
					sorttype: function (c, o)
					{
						return o.QueueTime;
					},
					width: 70,
					align: "right"
				},
				{
					name: 'InfoSpeed',
					index: 'InfoSpeed',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatBotSpeed(obj);
					},
					sorttype: function (c, o)
					{
						return o.InfoSpeedCurrent > 0 ? o.InfoSpeedCurrent : 0;
					},
					width: 100,
					align: "right"
				},
				{
					name: 'InfoSlot',
					index: 'InfoSlot',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatBotSlots(obj);
					},
					sorttype: function (c, o)
					{
						return o.InfoSlotCurrent;
					},
					width: 60,
					align: "right"
				},
				{
					name: 'InfoQueue',
					index: 'InfoQueue',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatBotQueue(obj);
					},
					sorttype: function (c, o)
					{
						return o.InfoQueueCurrent;
					},
					width: 60,
					align: "right"
				}
			],
			onSelectRow: function(guid)
			{
				self.websocket.sendGuid(Enum.Request.PacketsFromBot, guid);
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('bots.sort.index', index);
				self.cookie.setCookie('bots.sort.sortorder', sortorder);
			},
			pager: $('#bots_pager'),
			pgbuttons: false,
			pginput: false,
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			hidegrid: false,
			rowNum: 999999999,
			sortname: self.cookie.getCookie('bots.sort.index', 'Name'),
			sortorder: self.cookie.getCookie('bots.sort.sortorder', 'asc'),
			caption: _("Bots")
		})
		.navGrid('#bots_pager',{edit:false,add:false,del:false,search:false,refresh:false})
		.navButtonAdd('#bots_pager',{
			caption:"",
			buttonicon:"loading-symbol",
			id: "bots_loading"
		});
		$("#bots_loading .ui-icon").hide();

		/* ********************************************************************************************************** */
		/* PACKET GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#packets_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Id'), _('Name'), _('Size'), _('Speed'), _('Time'), _('Updated')],
			colModel: [
				{
					name: 'Object',
					index: 'Object',
					formatter: function (cell, grid, obj)
					{
						return JSON.stringify(obj);
					},
					hidden: true},
				{
					name: 'Icon',
					index: 'Icon',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketIcon(obj, "XG.flipPacket(\"" + obj.Guid + "\");");
					},
					width: 38,
					sortable: false,
					classes: "icon-cell"
				},
				{
					name: 'Id',
					index: 'Id',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketId(obj);
					},
					sorttype: function (c, o)
					{
						return o.Id;
					},
					width: 40,
					align: "right"
				},
				{
					name: 'Name',
					index: 'Name',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketName(obj);
					},
					sorttype: function (c, o)
					{
						return o.RealName != undefined && o.RealName != "" ? o.RealName : o.Name;
					},
					fixed: false,
					classes: "progress-cell"
				},
				{
					name: 'Size',
					index: 'Size',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketSize(obj);
					},
					sorttype: function (c, o)
					{
						return o.RealSize > 0 ? o.RealSize : o.Size;
					},
					width: 70,
					align: "right"},
				{
					name: 'Speed',
					index: 'Speed',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketSpeed(obj);
					},
					sorttype: function (c, o)
					{
						return o.Part != null ? o.Part.Speed : 0;
					},
					width: 70,
					align: "right"
				},
				{
					name: 'TimeMissing',
					index: 'TimeMissing',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketTimeMissing(obj)
					},
					sorttype: function (c, o)
					{
						return o.Part != null ? o.Part.TimeMissing : 0;
					},
					width: 90,
					align: "right"
				},
				{
					name: 'LastUpdated',
					index: 'LastUpdated',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.helper.date2Human(obj.LastUpdated);
					},
					sorttype: function (c, o)
					{
						return moment(o.LastUpdated).valueOf();
					},
					width: 135,
					align: "right"
				}
			],
			onSelectRow: function(guid)
			{
				var pack = self.websocket.getRowData('packets_table', guid);
				if(pack)
				{
					$('#bots_table').setSelection(pack.ParentGuid, false);
				}
			},
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('packets.sort.index', index);
				self.cookie.setCookie('packets.sort.sortorder', sortorder);
			},
			pager: $('#packets_pager'),
			pgbuttons: false,
			pginput: false,
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			hidegrid: false,
			rowNum: 999999999,
			sortname: self.cookie.getCookie('packets.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('packets.sort.sortorder', 'asc'),
			caption: _("Packets")
		})
		.navGrid('#packets_pager',{edit:false,add:false,del:false,search:false,refresh:false})
		.navButtonAdd('#packets_pager',{
			caption:"",
			buttonicon:"loading-symbol",
			id: "packets_loading"
		});
		$("#packets_loading .ui-icon").hide();

		/* ********************************************************************************************************** */
		/* SEARCH GRID                                                                                                */
		/* ********************************************************************************************************** */

		$("#search_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', '', ''],
			colModel: [
				{
					name: 'Object',
					index: 'Object',
					formatter: function (cell, grid, obj)
					{
						return JSON.stringify(obj);
					},
					hidden: true
				},
				{
					name: 'Icon',
					index: 'Icon',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatSearchIcon(obj);
					},
					width: 26,
					sortable: false,
					classes: "icon-cell"
				},
				{
					name: 'Name',
					index: 'Name',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return _(obj.Name);
					},
					fixed: false,
					sortable: false
				},
				{
					name: 'Action',
					index: 'Action',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatSearchAction(obj);
					},
					width: 18,
					sortable: false
				}
			],
			onSelectRow: function(guid)
			{
				var search = self.websocket.getRowData("search_table", guid);

				self.websocket.sendGuid(self.activeTab == 0 ? Enum.Request.Search : Enum.Request.SearchExternal, search.Guid);
			},
			ExpandColumn : 'Name',
			viewrecords: true,
			autowidth: true,
			hidegrid: false,
			rowNum: 999999999
		});

		$("#search-text").keyup( function (e)
		{
			if (e.which == 13)
			{
				self.addSearch();
			}
		});

		/**************************************************************************************************************/
		/* SEARCH GRID                                                                                                */
		/**************************************************************************************************************/

		$("#packets_external_table").jqGrid(
		{
			url: "",
			datatype:'local',
			cmTemplate:{fixed:true},
			colNames: ['', '', _('Id'), _('Name'), _('Last Mentioned'), _('Size'), _('Bot'), _('Speed')],
			colModel: [
				{
					name: 'Object',
					index: 'Object',
					formatter: function (cell, grid, obj)
					{
						return JSON.stringify(obj);
					},
					hidden: true
				},
				{
					name: 'Icon',
					index: 'Icon',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketIcon(obj, "XG.downloadLink(\"" + obj.Guid + "\");", true);
					},
					width: 24,
					sortable: false
				},
				{
					name: 'Id',
					index: 'Id',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketId(obj);
					},
					sorttype: function (c, o)
					{
						return o.Id;
					},
					width: 38,
					align: "right"
				},
				{
					name: 'Name',
					index: 'Name',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatPacketName(obj);
					},
					fixed: false
				},
				{
					name: 'LastMentioned',
					index: 'LastMentioned',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.helper.date2Human(obj.LastMentioned);
					},
					sorttype: function (c, o)
					{
						return moment(o.LastMentioned).valueOf();
					},
					width: 140,
					align: "right"
				},
				{
					name: 'Size',
					index: 'Size',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.helper.size2Human(obj.Size);
					},
					sorttype: function (c, o)
					{
						return o.Size;
					},
					width: 60,
					align: "right"
				},
				{
					name: 'BotName',
					index: 'BotName',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return obj.BotName;
					},
					width: 160
				},
				{
					name: 'BotSpeed',
					index: 'BotSpeed',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.helper.speed2Human(obj.BotSpeed);
					},
					sorttype: function (c, o)
					{
						return o.BotSpeed;
					},
					width: 80,
					align: "right"
				}
			],
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('packets_external.sort.index', index);
				self.cookie.setCookie('packets_external.sort.sortorder', sortorder);
			},
			pager:$('#packets_external_pager'),
			pgbuttons: false,
			pginput: false,
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			hidegrid: false,
			rowNum: 999999999,
			sortname: self.cookie.getCookie('packets_external.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('packets_external.sort.sortorder', 'asc'),
			caption: _("Search via xg.bitpir.at")
		})
		.navGrid('#packets_external_pager',{edit:false,add:false,del:false,search:false,refresh:false})
		.navButtonAdd('#packets_external_pager',{
			caption:"",
			buttonicon:"loading-symbol",
			id: "packets_external_loading"
		});
		$("#packets_external_loading .ui-icon").hide();

		/* ********************************************************************************************************** */
		/* FILE GRID                                                                                                  */
		/* ********************************************************************************************************** */

		$("#files_table").jqGrid(
		{
			datatype: "local",
			cmTemplate: {fixed:true},
			colNames: ['', '', _('Name'), _('Size'), _('Speed'), _('Time')],
			colModel: [
				{
					name: 'Object',
					index: 'Object',
					formatter: function (cell, grid, obj)
					{
						return JSON.stringify(obj);
					},
					hidden: true
				},
				{
					name: 'Icon',
					index: 'Icon',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatFileIcon(obj);
					},
					width: 24,
					sortable: false
				},
				{
					name: 'Name',
					index: 'Name',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatFileName(obj);
					},
					fixed: false,
					classes: "progress-cell"
				},
				{
					name: 'Size',
					index: 'Size',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatFileSize(obj);
					},
					sorttype: function (c, o)
					{
						return o.Size;
					},
					width: 70,
					align: "right"
				},
				{
					name: 'Speed',
					index: 'Speed',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatFileSpeed(obj);
					},
					sorttype: function (c, o)
					{
						var speed = 0;
						$.each(o.Parts, function (i, part)
						{
							speed += part.Speed;
						});
						return speed;
					},
					width: 70,
					align: "right"
				},
				{
					name: 'TimeMissing',
					index: 'TimeMissing',
					formatter: function (cell, grid, obj)
					{
						obj = JSON.parse(obj.Object);
						return self.formatter.formatFileTimeMissing(obj)
					},
					sorttype: function (c, o)
					{
						var time = 0;
						$.each(o.Parts, function (i, part)
						{
							time = time == 0 ? part.TimeMissing : (time < part.TimeMissing ? time : part.TimeMissing);
						});
						return time;
					},
					width: 90,
					align: "right"
				}
			],
			onSortCol: function(index, iCol, sortorder)
			{
				self.cookie.setCookie('files.sort.index', index);
				self.cookie.setCookie('files.sort.sortorder', sortorder);
			},
			pager: $('#files_pager'),
			pgbuttons: false,
			pginput: false,
			ExpandColumn: 'Name',
			viewrecords: true,
			autowidth: true,
			scrollrows: true,
			hidegrid: false,
			rowNum: 999999999,
			sortname: self.cookie.getCookie('files.sort.index', 'Id'),
			sortorder: self.cookie.getCookie('files.sort.sortorder', 'asc'),
			caption: _("Files")
		})
		.navGrid('#files_pager',{edit:false,add:false,del:false,search:false,refresh:false})
		.navButtonAdd('#files_pager',{
			caption:"",
			buttonicon:"loading-symbol",
			id: "files_loading"
		});
		$("#files_loading .ui-icon").hide();
	},

	initializeDialogs: function()
	{
		var self = this;

		/* ********************************************************************************************************** */
		/* SERVER / CHANNEL DIALOG                                                                                    */
		/* ********************************************************************************************************** */

		$("#server_channel_button")
			.button({icons: { primary: "icon-globe-1" }})
			.click( function()
			{
				$("#dialog_server_channels").dialog("open");
			});

		$("#dialog_server_channels").dialog({
			autoOpen: false,
			width: 830,
			modal: true,
			resizable: false
		});

		/* ********************************************************************************************************** */
		/* STATISTICS DIALOG                                                                                          */
		/* ********************************************************************************************************** */

		$("#statistics_button")
			.button({icons: { primary: "icon-chart-bar" }})
			.click( function()
			{
				self.websocket.send(Enum.Request.Statistics);
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
			self.statistics.updateSnapshotPlot();
		});

		$("#snapshots_button")
			.button({icons: { primary: "icon-chart-bar" }})
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

		/* ********************************************************************************************************** */
		/* ERROR DIALOG                                                                                               */
		/* ********************************************************************************************************** */

		$("#dialog_error").dialog({
			autoOpen: false,
			modal: true,
			resizable: false,
			close: function()
			{
				$('#dialog_error').dialog('open');
			}
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
		$("#show_offline_bots")
			.button({icons: { primary: "icon-eye" }})
			.click( function()
			{
				self.cookie.setCookie("show_offline_bots", $("#show_offline_bots").attr('checked') ? "1" : "0" );
			});
		$("#human_dates")
			.button({icons: { primary: "icon-clock" }})
			.click( function()
			{
				self.cookie.setCookie("human_dates", $("#human_dates").attr('checked') ? "1" : "0" );
			});
	},

	addSearch: function ()
	{
		var tbox = $('#search-text');
		if(tbox.val() != "")
		{
			this.websocket.sendName(Enum.Request.AddSearch, tbox.val());
			tbox.val('');
		}
	},

	/**
	 * @param {String} guid
	 */
	removeSearch: function (guid)
	{
		this.websocket.sendGuid(Enum.Request.RemoveSearch, guid);
	},

	/**
	 * @param {String} guid
	 */
	flipPacket: function (guid)
	{
		var self = this;

		var pack = self.websocket.getRowData('packets_table', guid);
		if(pack)
		{
			if(!pack.Enabled)
			{
				$("#" + pack.Guid).effect("transfer", { to: $("#00000000-0000-0000-0000-000000000004") }, 500);
			}
			else
			{
				$("#00000000-0000-0000-0000-000000000004").effect("transfer", { to: $("#" + pack.Guid) }, 500);
			}
			self.flipObject(guid, "packets_table");
		}
	},

	/**
	 * @param {String} guid
	 * @param {String} grid
	 */
	flipObject: function (guid, grid)
	{
		var self = this;

		var obj = self.websocket.getRowData(grid, guid);
		if(obj)
		{
			if(!obj.Enabled)
			{
				self.websocket.sendGuid(Enum.Request.ActivateObject, obj.Guid);
			}
			else
			{
				self.websocket.sendGuid(Enum.Request.DeactivateObject, obj.Guid);
			}
		}
	},

	/**
	 * @param {String} guid
	 */
	downloadLink: function (guid)
	{
		var self = XG;

		$("#" + guid).effect("transfer", { to: $("#00000000-0000-0000-0000-000000000004") }, 500);

		var data = self.websocket.getRowData("packets_external_table", guid);
		self.websocket.sendName(Enum.Request.ParseXdccLink, data.IrcLink);
	}
});
