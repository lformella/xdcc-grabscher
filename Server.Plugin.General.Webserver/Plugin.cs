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

namespace XG.Server.Plugin.General.Webserver
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		Webserver.Server _server;

		Websocket.Server _socket;

		readonly string _salt = BitConverter.ToString(new SHA256Managed().ComputeHash(BitConverter.GetBytes(new Random().Next()))).Replace("-", "");

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			byte[] inputBytes = Encoding.UTF8.GetBytes(_salt + Settings.Instance.Password + _salt);
			byte[] hashedBytes = new SHA256Managed().ComputeHash(inputBytes);
			string passwortHash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

			_server = new Webserver.Server
			{
				Servers = Servers,
				Files = Files,
				Searches = Searches,
				Snapshots = Snapshots,
				Password = passwortHash,
				Salt = _salt
			};
			_server.Start();

			_socket = new Websocket.Server
			{
				Servers = Servers,
				Files = Files,
				Searches = Searches,
				Snapshots = Snapshots,
				Password = passwortHash,
				Salt = _salt
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
