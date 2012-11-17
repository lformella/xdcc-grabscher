//
//  Server.cs
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

namespace XG.Server.Plugin.General.Webserver.Webserver
{
	public class Server : SaltedPassword
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
#if !UNSAFE
			try
			{
#endif
				_listener.Prefixes.Add("http://*:" + (Settings.Instance.WebServerPort) + "/");
				try
				{
					_listener.Start();
				}
				catch (HttpListenerException ex)
				{
#if WINDOWS
					if (ex.NativeErrorCode == 5)
					{
						Log.Fatal(@"TO GET XG UP AND RUNNING YOU MUST RUN 'netsh http add urlacl url=http://*:5556/ user=%USERDOMAIN%\%USERNAME%' AS ADMINISTRATOR");
					}
#endif
					throw;
				}

				var fileLoader = new FileLoader {Salt = Salt};

				while (_allowRunning)
				{
#if !UNSAFE
					try
					{
#endif
						var connection = new BrowserConnection
						{
							Context = _listener.GetContext(),
							Servers = Servers,
							Files = Files,
							Searches = Searches,
							Snapshots = Snapshots,
							FileLoader = fileLoader,
							Password = Password,
							Salt = Salt
						};

						connection.Start();
#if !UNSAFE
					}
					catch (HttpListenerException)
					{
						// this is ok
					}
#endif
				}
#if !UNSAFE
			}
			catch (HttpListenerException)
			{
				// this is ok
			}
#endif
		}

		protected override void StopRun()
		{
			_allowRunning = false;

			_listener.Close();
		}

		#endregion
	}
}

