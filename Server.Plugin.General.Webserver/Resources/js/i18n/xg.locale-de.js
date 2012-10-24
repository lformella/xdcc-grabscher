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

Translate = function(light)
{
	/* ************************************************************************************************************** */
	/* PASSWORD DIALOG                                                                                                */
	/* ************************************************************************************************************** */

	$("#password_tip").html("Bitte gib das Passwort f&uuml;r diese Webseite ein.");
	$("#password_label").html("Passwort");
	$("#dialog_password")
		.dialog("option", "title", "Passwort ben&ouml;tigt")
		.dialog("option", "buttons", [{ text: "Verbinden", click: function() { Password.buttonConnectClicked($("#dialog_password")); } }]);

	if (light)
	{
		return;
	}

	/* ************************************************************************************************************** */
	/* OTHER DIALOGS                                                                                                  */
	/* ************************************************************************************************************** */

	$("#dialog_server_channels")
		.dialog("option", "title", "Server und Channels &auml;ndern");

	$("#dialog_statistics")
		.dialog("option", "title", "Statistiken anschauen");

	$("#dialog_snapshots")
		.dialog("option", "title", "Erweiterte Statistiken anschauen");

	/* ************************************************************************************************************** */
	/* BOT GRID                                                                                                       */
	/* ************************************************************************************************************** */

	$("#bots_table")
		.setLabel("Name", "Name")
		.setLabel("Speed", "Geschw.")
		.setLabel("QueuePosition", "S-Pos")
		.setLabel("QueueTime", "S-Zeit")
		.setLabel("InfoSpeedMax", "max. Geschw.")
		.setLabel("InfoSlotTotal", "Pl&auml;tze")
		.setLabel("InfoQueueTotal", "Schlange");

	/* ************************************************************************************************************** */
	/* PACKET GRID                                                                                                    */
	/* ************************************************************************************************************** */

	$("#packets_table")
		.setCaption("Pakete")
		.setLabel("Name", "Name")
		.setLabel("Size", "Gr&ouml;&szlig;e")
		.setLabel("Speed", "Geschw.")
		.setLabel("TimeMissing", "Zeit")
		.setLabel("LastUpdated", "Aktualisiert");

	/* ************************************************************************************************************** */
	/* SEARCH GRID                                                                                                    */
	/* ************************************************************************************************************** */

	var searchTable = $("#search_table");
	searchTable.setRowData(1, {name:"ODay Pakete"});
	searchTable.setRowData(2, {name:"OWeek Pakete"});
	searchTable.setRowData(3, {name:"Downloads"});
	searchTable.setRowData(4, {name:"Aktivierte Pakete"});

	/* ************************************************************************************************************** */
	/* SEARCH GRID                                                                                                    */
	/* ************************************************************************************************************** */

	$("#searches_xg_bitpir_at")
		.setCaption("Suche via xg.bitpir.at")
		.setLabel("Name", "Name")
		.setLabel("LastMentioned", "Aktualisiert")
		.setLabel("Size", "Gr&ouml;&szlig;e")
		.setLabel("BotSpeed", "Geschw.");

	/* ************************************************************************************************************** */
	/* SNAPSHOTS                                                                                                      */
	/* ************************************************************************************************************** */

	$("#label_1").html("1 Tag");
	$("#label_7").html("1 Woche");
	$("#label_31").html("1 Monat");

	/* ************************************************************************************************************** */
	/* GENERIC                                                                                                        */
	/* ************************************************************************************************************** */

	$(".translate_all").html("Alle");
	$(".translate_connected").html("Verbunden");
	$(".translate_disconnected").html("Getrennt");
	$(".translate_size").html("Gr&ouml;&szlig;e");
	$(".translate_size_connected").html("Gr&ouml;&szlig;e verbunden");
	$(".translate_size_disconnected").html("Gr&ouml;&szlig;e getrennt");
	$(".translate_free_queue").html("Freie Warteschlange");
	$(".translate_free_slots").html("Frei Pl&auml;tze");
	$(".translate_timespan").html("Zeitspanne");
	$(".translate_average_speed_current").html("momentane Geschw.");
	$(".translate_average_speed_max").html("maximale Geschw.");

	/* ************************************************************************************************************** */
	/* OTHERS                                                                                                         */
	/* ************************************************************************************************************** */

	$("#statistics_button").button( "option", {label: "Statistiken"});
	$("#show_offline_bots").button( "option", {label: "inaktive Bots ausblenden"});
	$("#snapshots_button").button( "option", {label: "Erweiterte Statistiken"});

	$("#tab_1").html("IRC &Uuml;bersicht");
	$("#tab_2").html("Externe Suche via xg.bitpir.at");
	$("#tab_3").html("Dateien");

	LANG_MONTH_SHORT = ["Jan", "Feb", "Mar", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez"];
	LANG_MONTH = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
	LANG_WEEKDAY = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
};
