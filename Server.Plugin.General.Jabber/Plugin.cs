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
using System.Linq;
using System.Threading;

using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;

using log4net;

using XG.Core;
using XG.Server.Plugin;

namespace XG.Server.Plugin.General.Jabber
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		static readonly ILog log = LogManager.GetLogger(typeof(Plugin));

		XmppClientConnection _client;

		Thread _serverThread;

		#endregion

		#region RUN STOP
		
		public override void Start ()
		{
			// start the server thread
			_serverThread = new Thread(new ThreadStart(OpenClient));
			_serverThread.Start();
		}
		
		
		public override void Stop ()
		{
			CloseClient();
			_serverThread.Abort();
		}
		
		#endregion

		#region SERVER

		/// <summary>
		/// Opens the server port, waiting for clients
		/// </summary>
		void OpenClient()
		{
			_client = new XmppClientConnection(Settings.Instance.JabberServer);
			_client.Open(Settings.Instance.JabberUser, Settings.Instance.JabberPassword);
			_client.OnLogin += delegate(object sender)
			{
				UpdateState(0);
			};
			_client.OnError += delegate(object sender, Exception ex)
			{
				log.Fatal("OpenServer()", ex);
			};
			_client.OnAuthError += delegate(object sender, Element e)
			{
				log.Fatal("OpenServer() " + e.ToString());
			};
		}

		void CloseClient()
		{
			_client.Close();
		}

		void UpdateState(double aSpeed)
		{
			if(_client == null) { return; }

			Presence p = null;
			if(aSpeed > 0)
			{
				string str = "";
				if (aSpeed < 1024) { str =  aSpeed + " B/s"; }
				else if (aSpeed < 1024 * 1024) { str =  (aSpeed / 1024).ToString("0.00") + " KB/s"; }
				else if (aSpeed < 1024 * 1024 * 1024) { str =  (aSpeed / (1024 * 1024)).ToString("0.00") + " MB/s"; }
				else { str =  (aSpeed / (1024 * 1024 * 1024)).ToString("0.00") + " GB/s"; }

				p = new Presence(ShowType.chat, str);
				p.Type = PresenceType.available;
			}
			else
			{
				p = new Presence(ShowType.away, "Idle");
				p.Type = PresenceType.available;
			}
			_client.Send(p);
		}

		#endregion

		#region EVENTHANDLER

		protected new void FileChanged(AObject aObj)
		{
			if(aObj is FilePart)
			{
				double speed = 0;
				try
				{
					speed = (from file in Files.All from part in file.Parts select part.Speed).Sum();
				}
				catch  {}
				UpdateState(speed);
			}
		}

		#endregion
	}
}
