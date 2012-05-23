//  
//  Copyright(C) 2010 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Threading;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;
using log4net;
using XG.Core;

namespace XG.Server.Jabber
{
	public class JabberClient : IServerPlugin
	{
		#region VARIABLES

		private static readonly ILog myLog = LogManager.GetLogger(typeof(Connection));

		private ServerRunner myRunner;
		private XmppClientConnection myClient;

		private Thread myServerThread;

		#endregion

		#region RUN STOP
		
		public void Start (ServerRunner aParent)
		{
			this.myRunner = aParent;

			// start the server thread
			this.myServerThread = new Thread(new ThreadStart(OpenClient));
			this.myServerThread.Start();

			this.myRunner.ObjectChangedEvent += new ObjectDelegate(myRunner_ObjectChangedEventHandler);
		}
		
		
		public void Stop ()
		{
			this.myRunner.ObjectChangedEvent -= new ObjectDelegate(myRunner_ObjectChangedEventHandler);

			this.CloseClient();
			this.myServerThread.Abort();
		}
		
		#endregion

		#region SERVER

		/// <summary>
		/// Opens the server port, waiting for clients
		/// </summary>
		private void OpenClient()
		{
			this.myClient = new XmppClientConnection(Settings.Instance.JabberServer);
			this.myClient.Open(Settings.Instance.JabberUser, Settings.Instance.JabberPassword);
			this.myClient.OnLogin += delegate(object sender)
			{
				this.UpdateState(0);
			};
			this.myClient.OnError += delegate(object sender, Exception ex)
			{
				myLog.Fatal("OpenServer()", ex);
			};
			this.myClient.OnAuthError += delegate(object sender, Element e)
			{
				myLog.Fatal("OpenServer() " + e.ToString());
			};
		}

		private void CloseClient()
		{
			this.myClient.Close();
		}

		private void UpdateState(double aSpeed)
		{
			if(this.myClient == null) { return; }

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
			this.myClient.Send(p);
		}

		#endregion

		#region EVENTS

		protected void myRunner_ObjectChangedEventHandler(XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGFilePart))
			{
				double speed = 0;
				foreach(XGObject obj in this.myRunner.GetFiles())
				{
					if(obj.GetType() == typeof(XGFilePart))
					{
						speed += ((XGFilePart)obj).Speed;
					}
				}
				this.UpdateState(speed);
			}
		}

		#endregion
	}
}
