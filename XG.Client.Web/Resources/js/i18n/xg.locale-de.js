//  
//  Copyright (C) 2011 Lars Formella <ich@larsformella.de>
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
	jQuery("#bots").setLabel("Speed", "Geschwindigkeit");
	jQuery("#bots").setLabel("QueuePosition", "S-Pos");
	jQuery("#bots").setLabel("QueueTime", "S-Zeit");
	jQuery("#bots").setLabel("SpeedMax", "max. Geschwindigkeit");
	jQuery("#bots").setLabel("SlotTotal", "Pl&auml;tze");
	jQuery("#bots").setLabel("QueueTotal", "Schlange");

	/* ************************************************************************************************************** */
	/* PACKET GRID                                                                                                    */
	/* ************************************************************************************************************** */

	jQuery("#packets").setCaption("Pakete");
	jQuery("#packets").setLabel("Name", "Name");
	jQuery("#packets").setLabel("Size", "Gr&ouml;&szlig;e");
	jQuery("#packets").setLabel("Speed", "Geschwindigkeit");
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
	jQuery("#searches_xg_bitpir_at").setLabel("Speed", "Geschwindigkeit");

	/* ************************************************************************************************************** */
	/* OTHERS                                                                                                         */
	/* ************************************************************************************************************** */

	$( "#statistics_button" ).button( "option", {label: "Statistiken"});
	$("#show_offline_bots").button( "option", {label: "inaktive Bots ausblenden"});
	$("#tab_1").html("IRC &Uuml;bersicht");
	$("#tab_2").html("Externe Suche via xg.bitpir.at");
	
	//LANG_MONTH = new Array("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December");
	//LANG_WEEKDAY = new Array("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday");
});