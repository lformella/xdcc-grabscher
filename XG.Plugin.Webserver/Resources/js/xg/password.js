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

define(['xg/config'], function(config)
{
	var passwordOk = false;
	var passwordDialog = $("#passwordDialog");
	var passwordButton = $("#passwordButton");
	var passwordInput = $("#password");
	var passwordLoading = $("#passwordLoading");

	function buttonConnectClicked ()
	{
		config.setPassword(encodeURIComponent(CryptoJS.SHA256(config.getSalt() + passwordInput.val() + config.getSalt())));

		if (checkPassword(config.getPassword()))
		{
			passwordOk = true;
			$("#passwordDialog .form-group").removeClass('has-error');
			passwordDialog.modal('hide');
		}
		else
		{
			$("#passwordDialog .form-group").addClass('has-error');
		}
	}

	function checkPassword (password)
	{
		passwordButton.prop("disabled", true);
		passwordLoading.show();
		var res = false;
		$.ajax({
			url: "?password=" + password,
			success: function ()
			{
				res = true;
			},
			async: false
		});
		passwordLoading.hide();
		passwordButton.prop("disabled", false);
		return res;
	}

	var self = {
		onPasswordOk: new Slick.Event(),

		initialize: function ()
		{
			passwordDialog.modal("show");
			passwordDialog.bind('shown.bs.modal',
				function ()
				{
					passwordInput.focus();
				}
			);
			passwordDialog.bind('hide.bs.modal',
				function (e)
				{
					if (!passwordOk)
					{
						e.preventDefault();
					}
				}
			);
			passwordDialog.bind('hidden.bs.modal',
				function ()
				{
					if (passwordOk)
					{
						self.onPasswordOk.notify({}, null, this);
					}
				}
			);

			passwordButton.click(function ()
			{
				buttonConnectClicked();
			});

			passwordInput.keyup(function (e)
			{
				$("#passwordDialog .form-group").removeClass('has-error');
				if (e.which == 13)
				{
					e.preventDefault();
					buttonConnectClicked();
				}
			});
		}
	};

	return self;
});
