;(function($){
/**
 * jqGrid German Translation
 * Version 1.0.0 (developed for jQuery Grid 3.3.1)
 * Olaf Kl&ouml;ppel opensource@blue-hit.de
 * http://blue-hit.de/ 
 *
 * Updated for jqGrid 3.8
 * Andreas Flack
 * http://www.contentcontrol-berlin.de
 *
 * Dual licensed under the MIT and GPL licenses:
 * http://www.opensource.org/licenses/mit-license.php
 * http://www.gnu.org/licenses/gpl.html
**/
$.jgrid = {
	defaults : {
		recordtext: "Zeige {0} - {1} von {2}",
	    emptyrecords: "Keine Datens&auml;tze vorhanden",
		loadtext: "L&auml;dt...",
		pgtext : "Seite {0} von {1}"
	},
	search : {
		caption: "Suche...",
		Find: "Suchen",
		Reset: "Zur&uuml;cksetzen",
	    odata : ['gleich', 'ungleich', 'kleiner', 'kleiner gleich','gr&ouml;ßer','gr&ouml;ßer gleich', 'beginnt mit','beginnt nicht mit','ist in','ist nicht in','endet mit','endet nicht mit','enth&auml;lt','enth&auml;lt nicht'],
	    groupOps: [	{ op: "AND", text: "alle" },	{ op: "OR",  text: "mindestens eine" }	],
		matchText: " erf&uuml;lle",
		rulesText: " Bedingung(en)"
	},
	edit : {
		addCaption: "Datensatz hinzuf&uuml;gen",
		editCaption: "Datensatz bearbeiten",
		bSubmit: "Speichern",
		bCancel: "Abbrechen",
		bClose: "Schließen",
		saveData: "Daten wurden ge&auml;ndert! &auml;nderungen speichern?",
		bYes : "ja",
		bNo : "nein",
		bExit : "abbrechen",
		msg: {
		    required:"Feld ist erforderlich",
		    number: "Bitte geben Sie eine Zahl ein",
		    minValue:"Wert muss gr&ouml;ßer oder gleich sein, als ",
		    maxValue:"Wert muss kleiner oder gleich sein, als ",
		    email: "ist keine g&uuml;ltige E-Mail-Adresse",
		    integer: "Bitte geben Sie eine Ganzzahl ein",
			date: "Bitte geben Sie ein g&uuml;ltiges Datum ein",
			url: "ist keine g&uuml;ltige URL. Pr&auml;fix muss eingegeben werden ('http://' oder 'https://')",
			nodefined : " ist nicht definiert!",
			novalue : " R&uuml;ckgabewert ist erforderlich!",
			customarray : "Benutzerdefinierte Funktion sollte ein Array zur&uuml;ckgeben!",
			customfcheck : "Benutzerdefinierte Funktion sollte im Falle der benutzerdefinierten &uuml;berpr&uuml;fung vorhanden sein!"
		}
	},
	view : {
	    caption: "Datensatz anzeigen",
	    bClose: "Schließen"
	},
	del : {
		caption: "L&ouml;schen",
		msg: "Ausgew&auml;hlte Datens&auml;tze l&ouml;schen?",
		bSubmit: "L&ouml;schen",
		bCancel: "Abbrechen"
	},
	nav : {
		edittext: " ",
	    edittitle: "Ausgew&auml;hlte Zeile editieren",
		addtext:" ",
	    addtitle: "Neue Zeile einf&uuml;gen",
	    deltext: " ",
	    deltitle: "Ausgew&auml;hlte Zeile l&ouml;schen",
	    searchtext: " ",
	    searchtitle: "Datensatz suchen",
	    refreshtext: "",
	    refreshtitle: "Tabelle neu laden",
	    alertcap: "Warnung",
	    alerttext: "Bitte Zeile ausw&auml;hlen",
		viewtext: "",
		viewtitle: "Ausgew&auml;hlte Zeile anzeigen"
	},
	col : {
		caption: "Spalten ausw&auml;hlen",
		bSubmit: "Speichern",
		bCancel: "Abbrechen"	
	},
	errors : {
		errcap : "Fehler",
		nourl : "Keine URL angegeben",
		norecords: "Keine Datens&auml;tze zu bearbeiten",
		model : "colNames und colModel sind unterschiedlich lang!"
	},
	formatter : {
		integer : {thousandsSeparator: ".", defaultValue: '0'},
		number : {decimalSeparator:",", thousandsSeparator: ".", decimalPlaces: 2, defaultValue: '0,00'},
		currency : {decimalSeparator:",", thousandsSeparator: ".", decimalPlaces: 2, prefix: "", suffix:" €", defaultValue: '0,00'},
		date : {
			dayNames:   [
				"So", "Mo", "Di", "Mi", "Do", "Fr", "Sa",
				"Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag"
			],
			monthNames: [
				"Jan", "Feb", "Mar", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez",
				"Januar", "Februar", "M&auml;rz", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"
			],
			AmPm : ["am","pm","AM","PM"],
			S: function (j) {return 'ter'},
			srcformat: 'Y-m-d',
			newformat: 'd.m.Y',
			masks : {
		        ISO8601Long: "Y-m-d H:i:s",
		        ISO8601Short: "Y-m-d",
		        ShortDate: "j.n.Y",
		        LongDate: "l, j. F Y",
		        FullDateTime: "l, d. F Y G:i:s",
		        MonthDay: "d. F",
		        ShortTime: "G:i",
		        LongTime: "G:i:s",
		        SortableDateTime: "Y-m-d\\TH:i:s",
		        UniversalSortableDateTime: "Y-m-d H:i:sO",
		        YearMonth: "F Y"
		    },
		    reformatAfterEdit : false
		},
		baseLinkUrl: '',
		showAction: '',
	    target: '',
	    checkbox : {disabled:true},
		idName : 'id'
	}
};
})(jQuery);
