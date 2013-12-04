//
//  de.js
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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

define(['./module'], function (i18n) {
	'use strict';

	i18n.config(['$translateProvider',
		function ($translateProvider) {
			$translateProvider.translations('de', {
				/* ************************************************************************************************************** */
				/* DIALOGS                                                                                                        */
				/* ************************************************************************************************************** */

				"Please enter the password for this webfrontend.": "Bitte gib das Passwort für diese Webseite ein.",
				"Password": "Passwort",
				"Password required": "Passwort benötigt",
				"Connect": "Verbinden",
				"Change Servers and Channels": "Server und Channels ändern",
				"Error": "Fehler",
				"Lost connection to XG server - please reload page!": "Verbindung zum XG Server verloren - bitte Seite neu laden!",
				"Add": "Hinzufügen",
				"Settings": "Einstellungen",
				"Download": "Runterladen",
				"XDCC link": "XDCC Link",
				"Example link": "Beispiel Link",
				"XDCC link input": "XDCC Link Eingabe",
				"Please enter a valid XDCC link.": "Bitte gib einen gültigen XDCC Link ein.",

				/* ************************************************************************************************************** */
				/* GRIDS                                                                                                          */
				/* ************************************************************************************************************** */

				"Id": "Id",
				"Name": "Name",
				"Speed": "Geschwindigkeit",
				"Q-Pos": "S-Pos",
				"Q-Time": "S-Zeit",
				"max Speed": "max. Geschwindigkeit",
				"Slots": "Plätze",
				"Queue": "Schlange",
				"Size": "Größe",
				"BotName": "Bot",
				"BotSpeed": "Geschwindigkeit",
				"Content": "Inhalt",
				"Packets": "Pakete",
				"Time Missing": "Zeit",
				"Last Updated": "Aktualisiert",
				"Updated": "Aktualisiert",
				"ODay Packets": "neue Pakete von heute",
				"OWeek Packets": "neue Pakete dieser Woche",
				"Downloads": "Downloads",
				"Enabled Packets": "Aktivierte Pakete",
				"External search": "Externe Suche",
				"Last Mentioned": "Aktualisiert",
				"Bot Speed": "Geschw.",
				"User": "Benutzer",

				/* ************************************************************************************************************** */
				/* SNAPSHOTS                                                                                                      */
				/* ************************************************************************************************************** */

				"1 Day": "1 Tag",
				"1 Week": "1 Woche",
				"1 Month": "1 Monat",

				"Snapshot_1": "Geschwindigkeit",
				"Snapshot_2": "Servers",
				"Snapshot_21": "Server aktiviert",
				"Snapshot_22": "Server deaktiviert",
				"Snapshot_3": "Server verbunden",
				"Snapshot_4": "Server getrennt",
				"Snapshot_5": "Channels",
				"Snapshot_23": "Channel aktiviert",
				"Snapshot_24": "Channel deaktiviert",
				"Snapshot_6": "Channel verbunden",
				"Snapshot_7": "Channel getrennt",
				"Snapshot_8": "Bots",
				"Snapshot_9": "Bots verbunden",
				"Snapshot_10": "Bots getrennt",
				"Snapshot_11": "Bots frei Plätze",
				"Snapshot_12": "Bots freie Warteschlange",
				"Snapshot_19": "Bots momentane Geschwindigkeit",
				"Snapshot_20": "Bots maximale Geschwindigkeit",
				"Snapshot_13": "Pakete",
				"Snapshot_14": "Pakete verbunden",
				"Snapshot_15": "Pakete getrennt",
				"Snapshot_16": "Paketgröße",
				"Snapshot_17": "Paketgröße ladend",
				"Snapshot_18": "Paketgröße nicht ladend",
				"Snapshot_25": "Paketgröße verbunden",
				"Snapshot_26": "Paketgröße getrennt",
				"Snapshot_27": "Dateigröße fertig",
				"Snapshot_28": "Dateigröße fehlend",
				"Snapshot_29": "Zeit bis fertig",

				/* ************************************************************************************************************** */
				/* NOTIFICATIONS                                                                                                  */
				/* ************************************************************************************************************** */

				"Notification_1": "Paket {{Name}} ({{ParentName}}) ist fertig",
				"Notification_2": "Paket {{Name}} ({{ParentName}}) ist nicht fertig",
				"Notification_3": "Paket {{Name}} ({{ParentName}}) ist kaputt",
				"Notification_4": "Paket {{Name}} ({{ParentName}}) wurde angefragt",
				"Notification_5": "Paket {{Name}} ({{ParentName}}) wurde entfernt",
				"Notification_6": "Die Datei {{Name}} ist fertig",
				"Notification_7": "Die Datei {{Name}} hat die falsche Größe",
				"Notification_8": "Die Datei {{Name}} konnte nicht erstellt werden",
				"Notification_9": "Server {{Name}} ist verbunden",
				"Notification_10": "Server {{Name}} konnte nicht verbunden werden",
				"Notification_11": "Channel {{Name}} ({{ParentName}}) beigetreten",
				"Notification_12": "Channel {{Name}} ({{ParentName}}) konnte nicht beigetreten werden",
				"Notification_13": "Channel {{Name}} ({{ParentName}}) ist geblockt",
				"Notification_14": "Channel {{Name}} ({{ParentName}}) verlassen",
				"Notification_15": "Wurde aus Channel {{Name}} ({{ParentName}}) gekickt",
				"Notification_16": "Paket {{Name}} ({{ParentName}}) läd runter",
				"Notification_17": "Paket {{Name}} ({{ParentName}}) konnte nicht runtergeladen werden",
				"Notification_18": "Bot {{ParentName}} hat einen falschen Port für {{Name}} übermittelt",

				/* ************************************************************************************************************** */
				/* OTHERS                                                                                                         */
				/* ************************************************************************************************************** */

				"Dashboard": "Übersicht",
				"Graphs": "Graphen",
				"Hide offline Bots": "inaktive Bots ausblenden",
				"IRC View": "IRC Übersicht",
				"Files": "Dateien",
				"Notifications": "Benachrichtigungen",
				"Predefined": "Vorgefertigt",
				"Custom": "Eigene",
				"Ready": "Fertig",

				"of": "von",
				"connected": "verbunden",
				"downloaded": "herunter geladen",
				"All": "Alle",
				"Connected": "Verbunden",
				"Disconnected": "Getrennt",
				"Size Connected": "Größe verbunden",
				"Size Disconnected": "Größe getrennt",
				"Size Downloading": "Größe ladend",
				"Size not Downloading": "Größe n. ladend",
				"Free Queue": "Freie Warteschlange",
				"Free Slots": "Frei Plätze",
				"Timespan": "Zeitspanne",
				"Current Speed": "momentane Geschw.",
				"Max Speed": "maximale Geschw.",
				"Enabled": "Aktiviert",
				"Disabled": "Deaktiviert",
				"Count": "Zähler",
				"Time": "Zeit",
				"Human readable dates": "Lesbare Zeiten",
				"Combine IRC View": "Kombinierte IRC Übersicht"
			});
		}
	]);
});
