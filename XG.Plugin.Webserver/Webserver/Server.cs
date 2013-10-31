//
//  Server.cs
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

using System.Net;
using System.Reflection;
using log4net;
using XG.Config.Properties;

namespace XG.Plugin.Webserver.Webserver
{
	public class Server : ASaltedPassword
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		HttpListener _listener;

		bool _allowRunning = true;

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_listener = new HttpListener();
			try
			{
				_listener.Prefixes.Add("http://*:" + (Settings.Default.WebserverPort) + "/");
				try
				{
					_listener.Start();
				}
				catch (HttpListenerException ex)
				{
#if !__MonoCS__
					if (ex.NativeErrorCode == 5)
					{
						Log.Fatal(@"TO GET XG UP AND RUNNING YOU MUST RUN 'netsh http add urlacl url=http://*:" + Settings.Default.WebserverPort + @"/ user=%USERDOMAIN%\%USERNAME%' AS ADMINISTRATOR");
					}
#endif
					throw;
				}

				var fileLoader = new FileLoader {Salt = Salt};

				while (_allowRunning)
				{
					try
					{
						var connection = new BrowserConnection
						{
							Context = _listener.GetContext(),
							Servers = Servers,
							Files = Files,
							Searches = Searches,
							FileLoader = fileLoader,
							Password = Password,
							Salt = Salt
						};

						connection.Start();
					}
					catch (HttpListenerException)
					{
						// this is ok
					}
				}
			}
			catch (HttpListenerException)
			{
				// this is ok
			}
		}

		protected override void StopRun()
		{
			_allowRunning = false;

			_listener.Close();
		}

		#endregion
	}
}

