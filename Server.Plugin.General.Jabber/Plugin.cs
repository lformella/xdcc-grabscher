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
using System.Linq;

using XG.Core;

using agsXMPP;
using agsXMPP.protocol.client;

using log4net;

namespace XG.Server.Plugin.General.Jabber
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(typeof (Plugin));

		XmppClientConnection _client;

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_client = new XmppClientConnection(Settings.Instance.JabberServer);
			_client.Open(Settings.Instance.JabberUser, Settings.Instance.JabberPassword);
			_client.OnLogin += delegate { UpdateState(0); };
			_client.OnError += (sender, ex) => Log.Fatal("StartRun()", ex);
			_client.OnAuthError += (sender, e) => Log.Fatal("StartRun() " + e);
		}

		protected override void StopRun()
		{
			_client.Close();
		}

		#endregion

		#region EVENTHANDLER

        protected new void FileChanged(AObject aObj, string[] aFields)
		{
			if (aObj is FilePart && aFields.Contains("Speed"))
			{
				double speed;
				try
				{
					speed = (from file in Files.All from part in file.Parts select part.Speed).Sum();
				}
				catch (Exception)
				{
					speed = 0;
				}
				UpdateState(speed);
			}
		}

		#endregion

		#region FUNCTIONS

		void UpdateState(double aSpeed)
		{
			if (_client == null)
			{
				return;
			}

			Presence p;
			if (aSpeed > 0)
			{
				string str;
				if (aSpeed < 1024)
				{
					str = aSpeed + " B/s";
				}
				else if (aSpeed < 1024 * 1024)
				{
					str = (aSpeed / 1024).ToString("0.00") + " KB/s";
				}
				else if (aSpeed < 1024 * 1024 * 1024)
				{
					str = (aSpeed / (1024 * 1024)).ToString("0.00") + " MB/s";
				}
				else
				{
					str = (aSpeed / (1024 * 1024 * 1024)).ToString("0.00") + " GB/s";
				}

				p = new Presence(ShowType.chat, str) {Type = PresenceType.available};
			}
			else
			{
				p = new Presence(ShowType.away, "Idle") {Type = PresenceType.available};
			}
			_client.Send(p);
		}

		#endregion
	}
}
