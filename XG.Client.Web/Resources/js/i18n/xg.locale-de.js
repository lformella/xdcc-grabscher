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
	/* ********************************************************************** */
	/* PASSWORD DIALOG                                                        */
	/* ********************************************************************** */

	$("#password_tip").html("Bitte gib das Passwort f&uuml;r diese Webseite ein.");
	$("#password_label").html("Passwort");
	$("#dialog_password").dialog("option", "title", "Passwort ben&ouml;tigt");
	$("#dialog_password").dialog("option", "buttons", [{ text: "Verbinden", click: function() { ButtonConnectClicked($("#dialog_password")); } }]);

	/* ********************************************************************** */
	/* SERVER / CHANNEL DIALOG                                                */
	/* ********************************************************************** */

	$("#server_button").html("Server");
	$("#server_tip").html("Bitte gib den neuen Server Namen ein.");
	$("#server_label").html("Server");
	$("#dialog_server").dialog("option", "title", "Neuen Server einf&uuml;gen");
	$("#dialog_server").dialog("option", "buttons", [{ html: "Server einf&uuml;gen", click: function() { ButtonInsertServerClicked($("#dialog_server")); } }, { html: "Abbrechen", click: function() { ButtonCancelClicked($("#dialog_server")); } }]);

	$("#channel_button").html("Kanal");
	$("#channel_tip").html("Bitte gib den neuen Channel Namen ein.");
	$("#channel_label").html("Kanal");
	$("#dialog_channel").dialog("option", "title", "Neuen Kanal einf&uuml;gen");
	$("#dialog_channel").dialog("option", "buttons", [{ html: "Kanal einf&uuml;gen", click: function() { ButtonInsertChannelClicked($("#dialog_channel")); } }, { html: "Abbrechen", click: function() { ButtonCancelClicked($("#dialog_channel")); } }]);

	$("#current_item_button").html("Aktuelles Objekt");
	$("#delete_tip").html("Willst du wirklich das ausgew&auml;lte Objekt l&ouml;schen.");
	$("#dialog_delete").dialog("option", "title", "Wirklich l&ouml;schen");
	$("#dialog_delete").dialog("option", "buttons", [{ html: "Ja", click: function() { ButtonYesClicked($("#dialog_delete")); } }, { html: "Nein", click: function() { ButtonCancelClicked($("#dialog_delete")); } }]);

	/* ********************************************************************** */
	/* BOT GRID                                                               */
	/* ********************************************************************** */

	jQuery("#bots").setLabel(7, 'Geschwindigkeit');
	jQuery("#bots").setLabel(8, 'S-Pos');
	jQuery("#bots").setLabel(9, 'S-Zeit');
	jQuery("#bots").setLabel(10, 'Geschwindigkeit');
	jQuery("#bots").setLabel(12, 'Pl&auml;tze');
	jQuery("#bots").setLabel(14, 'Schlange');

	/* ********************************************************************** */
	/* PACKET GRID                                                            */
	/* ********************************************************************** */

	jQuery("#packets").setCaption("Pakete");
	jQuery("#packets").setLabel(8, 'Gr&ouml;&szlig;e');
	jQuery("#packets").setLabel(9, 'Geschwindigkeit');
	jQuery("#packets").setLabel(10, 'Zeit');
	jQuery("#packets").setLabel(16, 'Aktualisiert');

	/* ********************************************************************** */
	/* SEARCH GRID                                                            */
	/* ********************************************************************** */

	jQuery("#searches").setCaption("Suche");
	jQuery("#searches").setLabel(1, "Suche");
	jQuery("#searches").setRowData(1, {name:"ODay Pakete"});
	jQuery("#searches").setRowData(2, {name:"OWeek Pakete"});
	jQuery("#searches").setRowData(3, {name:"Downloads"});
	jQuery("#searches").setRowData(4, {name:"Aktivierte Pakete"});

	/* ********************************************************************** */
	/* OTHERS                                                                 */
	/* ********************************************************************** */

	$("#offline_bots").html("inaktive Bots ausblenden");
});