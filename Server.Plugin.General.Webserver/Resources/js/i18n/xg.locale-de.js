// 
//  xg.locale-de.js
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

$(function()
{
	/* ************************************************************************************************************** */
	/* PASSWORD DIALOG                                                                                                */
	/* ************************************************************************************************************** */

	$("#password_tip").html("Bitte gib das Passwort f&uuml;r diese Webseite ein.");
	$("#password_label").html("Passwort");
	$("#dialog_password").dialog("option", "title", "Passwort ben&ouml;tigt");
	$("#dialog_password").dialog("option", "buttons", [{ text: "Verbinden", click: function() { ButtonConnectClicked($("#dialog_password")); } }]);

	/* ************************************************************************************************************** */
	/* BOT GRID                                                                                                       */
	/* ************************************************************************************************************** */

	jQuery("#bots").setLabel("Name", "Name");
	jQuery("#bots").setLabel("Speed", "Geschw.");
	jQuery("#bots").setLabel("QueuePosition", "S-Pos");
	jQuery("#bots").setLabel("QueueTime", "S-Zeit");
	jQuery("#bots").setLabel("InfoSpeedMax", "max. Geschw.");
	jQuery("#bots").setLabel("InfoSlotTotal", "Pl&auml;tze");
	jQuery("#bots").setLabel("InfoQueueTotal", "Schlange");

	/* ************************************************************************************************************** */
	/* PACKET GRID                                                                                                    */
	/* ************************************************************************************************************** */

	jQuery("#packets").setCaption("Pakete");
	jQuery("#packets").setLabel("Name", "Name");
	jQuery("#packets").setLabel("Size", "Gr&ouml;&szlig;e");
	jQuery("#packets").setLabel("Speed", "Geschw.");
	jQuery("#packets").setLabel("TimeMissing", "Zeit");
	jQuery("#packets").setLabel("LastUpdated", "Aktualisiert");

	/* ************************************************************************************************************** */
	/* SEARCH GRID                                                                                                    */
	/* ************************************************************************************************************** */

	jQuery("#searches").setRowData(1, {name:"ODay Pakete"});
	jQuery("#searches").setRowData(2, {name:"OWeek Pakete"});
	jQuery("#searches").setRowData(3, {name:"Downloads"});
	jQuery("#searches").setRowData(4, {name:"Aktivierte Pakete"});

	/* ************************************************************************************************************** */
	/* SEARCH GRID                                                                                                    */
	/* ************************************************************************************************************** */

	jQuery("#searches_xg_bitpir_at").setCaption("Suche via xg.bitpir.at");
	jQuery("#searches_xg_bitpir_at").setLabel("Name", "Name");
	jQuery("#searches_xg_bitpir_at").setLabel("LastMentioned", "Aktualisiert");
	jQuery("#searches_xg_bitpir_at").setLabel("Size", "Gr&ouml;&szlig;e");
	jQuery("#searches_xg_bitpir_at").setLabel("BotSpeed", "Geschw.");

	/* ************************************************************************************************************** */
	/* SNAPSHOTS                                                                                                      */
	/* ************************************************************************************************************** */

	$("#snapshot_timespan").html("Zeitspanne");

	/* ************************************************************************************************************** */
	/* OTHERS                                                                                                         */
	/* ************************************************************************************************************** */

	$( "#statistics_button" ).button( "option", {label: "Statistiken"});
	$("#show_offline_bots").button( "option", {label: "inaktive Bots ausblenden"});
	$("#tab_1").html("IRC &Uuml;bersicht");
	$("#tab_2").html("Externe Suche via xg.bitpir.at");
	$("#tab_3").html("Dateien");

	LANG_MONTH_SHORT = ["Jan", "Feb", "Mar", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez"];
	LANG_MONTH = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
	LANG_WEEKDAY = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
});
