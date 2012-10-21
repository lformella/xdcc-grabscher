//
//  xg.password.js
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

var Password;
var XGPassword = Class.create(
{
	initialize: function()
	{
		var self = this;
		Password = this;

		this.url = new XGUrl();
		this.password = "";

		// check for cookies
		if (this.checkPassword(""))
		{
			this.loadGrid();
			return;
		}

		// display login
		$("#dialog_password").dialog({
			bgiframe: true,
			height: 140,
			modal: true,
			resizable: false,
			hide: 'explode',
			buttons: {
				'Connect': function()
				{
					self.buttonConnectClicked($(this));
				}
			},
			close: function()
			{
				if(self.url.password == "")
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

		Translate(true);
	},

	buttonConnectClicked: function (dialog)
	{
		var self = this;

		var passwordElement = $("#password");
		var saltElement = $("#salt");
		var password = CryptoJS.SHA256(saltElement.val() + passwordElement.val() + saltElement.val());

		if (this.checkPassword(password))
		{
			passwordElement.removeClass('ui-state-error');
			dialog.dialog('close');

			this.url.password = password;
			this.loadGrid();
		}
		else
		{
			passwordElement.addClass('ui-state-error');
		}
	},

	loadGrid: function()
	{
		var helper = new XGHelper();
		var cookie = new XGCookie();
		var formatter = new XGFormatter(helper);
		var refresh = new XGRefresh(this.url, helper);

		// load grid
		new XGBase(this.url, helper, refresh, cookie, formatter);

		Translate(false);

		// resize
		var resize = new XGResize();
		$("body").layout({
			onresize: function () {
				resize.resizeMain(innerLayout);
			},
			spacing_open: 4,
			spacing_closed: 4
		});
		var innerLayout = $("#layout_objects_container").layout({
			resizeWithWindow: false,
			onresize: function () {
				resize.resizeContainer();
			},
			spacing_open: 4,
			spacing_closed: 4
		});

		// resize after all is visible - twice, because the first run wont change all values :|
		resize.resizeMain(innerLayout);
		setTimeout(function() { resize.resizeMain(innerLayout); }, 1000);

		// start the refresh
		refresh.refreshGrid(0);
	},

	checkPassword: function(password)
	{
		var self = this;

		var res = false;
		$.ajax({
			url: self.url.jsonUrl(Enum.TCPClientRequest.Version + "&password=" + encodeURIComponent(password)),
			success: function()
			{
				res = true;
			},
			async: false
		});
		return res;
	}
});
