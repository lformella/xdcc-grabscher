//
//  password.js
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
var XGPassword = Class.create(
{
	initialize: function (salt, host, port)
	{
		var self = this;
		password = this;

		this.password = "";
		this.salt = salt;
		this.host = host;
		this.port = port;

		var buttonText = { text: _("Connect") };
		var buttons = {};
		buttons[buttonText["text"]] = function()
		{
			self.buttonConnectClicked($(this));
		};

		// display login
		$("#dialog_password").dialog({
			bgiframe: true,
			height: 140,
			modal: true,
			resizable: false,
			hide: 'explode',
			buttons: buttons,
			close: function()
			{
				if(self.password == "")
				{
					$('#dialog_password').dialog('open');
				}
				$("#password").val('').removeClass('ui-state-error');
			}
		});

		$("#password").keyup(function (e) {
			if (e.which == 13)
			{
				self.buttonConnectClicked($("#dialog_password"));
			}
		});
	},

	buttonConnectClicked: function (dialog)
	{
		var passwordElement = $("#password");
		var saltElement = $("#salt");
		var password = encodeURIComponent(CryptoJS.SHA256(this.salt + passwordElement.val() + this.salt));

		if (this.checkPassword(password))
		{
			passwordElement.removeClass('ui-state-error');
			dialog.dialog('close');

			this.password = password;
			this.loadGrid();
		}
		else
		{
			passwordElement.addClass('ui-state-error');
		}
	},

	loadGrid: function ()
	{
		var cookie = new XGCookie();
		var helper = new XGHelper(cookie);
		var formatter = new XGFormatter(helper);
		var statistics = new XGStatistics(helper);
		var websocket = new XGWebsocket(cookie, this.host, this.port, this.password, statistics);

		// load grid
		new XGBase(helper, statistics, cookie, formatter, websocket);

		// resize
		var resize = new XGResize(statistics);
	},

	checkPassword: function (password)
	{
		$("#loading_password").show();
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
			$("#loading_password").hide();
		}
		return res;
	}
});
