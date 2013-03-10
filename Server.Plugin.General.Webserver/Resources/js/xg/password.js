//
//  password.js
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

var password;
var XGPassword = (function()
{
	var salt, host, port, password;

	function buttonConnectClicked (dialog)
	{
		var passwordElement = $("#password");
		password = encodeURIComponent(CryptoJS.SHA256(salt + passwordElement.val() + salt));

		if (checkPassword(password))
		{
			passwordElement.removeClass('ui-state-error');
			dialog.dialog('close');

			startMain();
		}
		else
		{
			passwordElement.addClass('ui-state-error');
		}
	}

	function startMain ()
	{
		var dataView = Object.create(XGDataView);
		dataView.initialize();
		var cookie = Object.create(XGCookie);
		var helper = Object.create(XGHelper);
		helper.setHumanDates(cookie.getCookie("humanDates", "0") == "1");
		var formatter = Object.create(XGFormatter);
		formatter.initialize(helper);
		var statistics = Object.create(XGStatistics);
		statistics.initialize(helper);
		var websocket = Object.create(XGWebsocket);
		websocket.initialize(host, port, password);
		var grid = Object.create(XGGrid);
		grid.initialize(formatter, helper, dataView);
		//grid.setFilterOfflineBots(cookie.getCookie("filterOfflineBots", "0") == "1");
		var resize = Object.create(XGResize);
		var notification = Object.create(XGNotification);

		// start frontend
		var main = Object.create(XGMain);
		main.initialize(helper, statistics, cookie, formatter, websocket, dataView, grid, resize, notification);
		main.start();
	}

	function checkPassword (password)
	{
		var element = $("#loadingPassword");
		element.show();
		var res = false;
		$.ajax({
			url: "?password=" + password,
			success: function()
			{
				res = true;
			},
			async: false
		});
		if (!res)
		{
			element.hide();
		}
		return res;
	}

	return {
		/**
		 * @param {String} salt1
		 * @param {String} host1
		 * @param {String} port1
		 */
		initialize: function (salt1, host1, port1)
		{
			salt = salt1;
			host = host1;
			port = port1;

			var buttonText = { text: _("Connect") };
			var buttons = {};
			buttons[buttonText["text"]] = function()
			{
				buttonConnectClicked($(this));
			};

			// display login
			$("#dialogPassword").dialog({
				bgiframe: true,
				height: 140,
				modal: true,
				resizable: false,
				hide: 'explode',
				buttons: buttons,
				close: function()
				{
					if(password == "")
					{
						$('#dialogPassword').dialog('open');
					}
					$("#password").val('').removeClass('ui-state-error');
				}
			});

			$("#password").keyup(function (e) {
				if (e.which == 13)
				{
					buttonConnectClicked($("#dialogPassword"));
				}
			});
		}
	}
}());
