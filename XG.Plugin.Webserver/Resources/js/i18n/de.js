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
				"Api Keys": "Api Schlüssel",
				"Api Key": "Api Schlüssel",
				"Host": "Server",
				"Temp Path": "Temporärer Pfad",
				"Ready Path": "Fertig Pfad",
				"Auto Register Nickserv": "Automatisch Nick registrieren",
				"Enable Multi Downloads": "Multi Downloads erlauben",
				"Config": "Konfiguration",
				"Save": "Speichern",
				"Path in filesystem to store temporary files": "Pfad auf der Festplatte für temporäre Dateien",
				"Path in filesystem to store downloaded files": "Pfad auf der Festplatte für fertig geladene Dateien",
				"File regex": "Datei Regex",
				"Remove": "Entfernen",
				"Add new": "Neu hinzufügen",
				"Command": "Befehl",
				"Arguments": "Argumente",
				"File Handlers": "Datei Routinen",
				"File Handler": "Datei Routine",
				"Changes on the properties marked with an asterisk require a restart.": "Änderungen an Werten die mit einem Sternchen markiert sind benötigen einen Neustart.",
				"Max Download Speed": "Max. Download Geschw.",
				"Error Count": "Fehler Zähler",
				"Success Count": "Erfolgs Zähler",
				"Shutdown": "Beenden",
				"Do you really want to shutdown XG?": "Willst du XG wirklich beenden?",
				"Max Connections": "Max Verbindungen",
				"This message should be send after a successful connection": "Diese Nachricht wird gesendet wenn der Channel verbunden ist",
				"XG is offline": "XG ist offline",
				"XG is offline / not ready yet. Please try again later.": "XG ist offline oder noch nicht bereit. Bitte versuche es später nochmal.",

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
				"Bot Speed": "Geschwindigkeit",
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

				"Notification_Header_1": "Paket ist fertig",
				"Notification_Description_1": "Das Paket {{ObjectName1}} von {{ParentName1}} ist fertig",
				"Notification_Header_2": "Paket ist nicht fertig",
				"Notification_Description_2": "Das Paket {{ObjectName1}} von {{ParentName1}} ist nicht fertig",
				"Notification_Header_3": "Paket ist kaputt",
				"Notification_Description_3": "Das Paket {{ObjectName1}} von {{ParentName1}} ist kaputt",
				"Notification_Header_4": "Paket wurde angefragt",
				"Notification_Description_4": "Das Paket {{ObjectName1}} von {{ParentName1}} wurde angefragt",
				"Notification_Header_5": "Paket wurde entfernt",
				"Notification_Description_5": "Das Paket {{ObjectName1}} von {{ParentName1}} wurde entfernt",
				"Notification_Header_6": "Datei ist fertig",
				"Notification_Description_6": "Die Datei {{ObjectName1}} ist fertig",
				"Notification_Header_7": "Paket passt nicht auf Datei",
				"Notification_Description_7": "Das Paket {{ObjectName1}} passt nicht auf die Datei {{ObjectName2}}",
				"Notification_Header_8": "Datei konnte nicht erstellt werden",
				"Notification_Description_8": "Die Datei {{ObjectName1}} konnte nicht erstellt werden",
				"Notification_Header_9": "Server ist verbunden",
				"Notification_Description_9": "Der Server {{ObjectName1}} ist verbunden",
				"Notification_Header_10": "Server konnte nicht verbunden werden",
				"Notification_Description_10": "Der Server {{ObjectName1}} konnte nicht verbunden werden",
				"Notification_Header_11": "Channel beigetreten",
				"Notification_Description_11": "Der Channel {{ObjectName1}} von {{ParentName1}} wurde beigetreten",
				"Notification_Header_12": "Channel konnte nicht beigetreten werden",
				"Notification_Description_12": "Dem Channel {{ObjectName1}} von {{ParentName1}} konnte nicht beigetreten werden",
				"Notification_Header_13": "Channel ist geblockt",
				"Notification_Description_13": "Der Channel {{ObjectName1}} von {{ParentName1}} ist geblockt",
				"Notification_Header_14": "Channel verlassen",
				"Notification_Description_14": "Habe den Channel {{ObjectName1}} von {{ParentName1}} verlassen",
				"Notification_Header_15": "Wurde aus Channel gekickt",
				"Notification_Description_15": "Wurde aus Channel {{ObjectName1}} von {{ParentName1}} gekickt",
				"Notification_Header_16": "Paket läd runter",
				"Notification_Description_16": "Das Paket {{ObjectName1}} ({{ParentName1}}) läd runter",
				"Notification_Header_17": "Paket konnte nicht runtergeladen werden",
				"Notification_Description_17": "Das Paket {{ObjectName1}} von {{ParentName1}} konnte nicht runtergeladen werden",
				"Notification_Header_18": "Bot hat einen falschen Daten übermittelt",
				"Notification_Description_18": "Der Bot {{ParentName1}} hat falsche Daten für {{ObjectName1}} übermittelt",
				"Notification_Header_19": "Paketname is unterschiedlich",
				"Notification_Description_19": "Der übermittelte Name des Paketes {{ObjectName1}} unterscheided sich von dem erwarteten",

				/* ************************************************************************************************************** */
				/* OTHERS                                                                                                         */
				/* ************************************************************************************************************** */

				"NewVersionAvailable": "Es gibt eine neue Version: <a href='{{Url}}' target='_blank'>{{Latest}} ({{Name}})</a>",

				"Search": "Suche",
				"Dashboard": "Übersicht",
				"Graphs": "Graphen",
				"Show offline Bots": "Inaktive Bots anzeigen",
				"IRC View": "IRC Übersicht",
				"Files": "Dateien",
				"Notifications": "Benachrichtigungen",
				"Predefined": "Vorgefertigt",
				"Custom": "Eigene",
				"Ready": "Fertig",
				"None": "Nichts",
				"Group Search Results By": "Suchergebnisse gruppieren durch",

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
				"Combine IRC View": "Kombinierte IRC Übersicht",

				"Download this packet": "Lade dieses Paket runter",
				"Skip download": "Brich den Download ab",
				"Remove from queue": "Entferne aus der Warteschlange",
				"Enable": "Aktivieren",
				"Disable": "Deaktivieren",
				"Just display packets from this bot": "Zeige nur Pakte von diesem Bot an"
			});
		}
	]);
});
