// 
//  Plugin.cs
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
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using log4net;

namespace XG.Server.Plugin.General.Webserver
{
	public class Plugin : APlugin
	{
		#region VARIABLES
		
		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		HttpListener _listener;

		string _salt = BitConverter.ToString(new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(new Guid().ToString()))).Replace("-", "");

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_listener = new HttpListener();
#if !UNSAFE
			try
			{
#endif
				_listener.Prefixes.Add("http://*:" + (Settings.Instance.WebServerPort) + "/");
				_listener.Start();

				FileLoader fileLoader = new FileLoader();
				fileLoader.Salt = _salt;

				while (true)
				{
#if !UNSAFE
					try
					{
#endif
						BrowserConnection connection = new BrowserConnection();
						connection.Context = _listener.GetContext();
						connection.Servers = Servers;
						connection.Files = Files;
						connection.Searches = Searches;
						connection.Snapshots = Snapshots;
						connection.FileLoader = fileLoader;
						connection.Salt = _salt;

						connection.Start();
#if !UNSAFE
					}
					catch (Exception ex)
					{
						_log.Fatal("StartRun() client", ex);
					}
#endif
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				_log.Fatal("StartRun() server", ex);
			}
#endif
		}

		protected override void StopRun()
		{
			_listener.Close();
		}

		#endregion
	}
}
