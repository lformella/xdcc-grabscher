// 
//  Plugin.cs
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

using System;
using System.Security.Cryptography;
using System.Text;

using SharpRobin.Core;

namespace XG.Server.Plugin.General.Webserver
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		SHA256Managed _sha256 = new SHA256Managed();

		Webserver.Server _server;

		Websocket.Server _socket;

		public RrdDb RrdDb { get; set; }

		#endregion

		string Hash(string aStr = null)
		{
			byte[] bytes = aStr == null ? BitConverter.GetBytes(new Random().Next()) : Encoding.UTF8.GetBytes(aStr);
			return BitConverter.ToString(_sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
		}

		#region AWorker

		protected override void StartRun()
		{
			string salt = Hash();
			string passwortHash = Hash(salt + Settings.Instance.Password + salt);

			_server = new Webserver.Server
			{
				Servers = Servers,
				Files = Files,
				Searches = Searches,
				Notifications = Notifications,
				Password = passwortHash,
				Salt = salt
			};
			_server.Start();

			_socket = new Websocket.Server
			{
				Servers = Servers,
				Files = Files,
				Searches = Searches,
				Notifications = Notifications,
				Password = passwortHash,
				Salt = salt,
				RrdDb = RrdDb
			};
			_socket.Start();
		}

		protected override void StopRun()
		{
			_socket.Stop();

			_server.Stop();
		}

		#endregion
	}
}
