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
	public class Plugin : AServerGeneralPlugin
	{
		#region VARIABLES

		private static readonly ILog log = LogManager.GetLogger(typeof(Plugin));

		private XmppClientConnection client;

		private Thread serverThread;

		#endregion

		#region RUN STOP
		
		public override void Start ()
		{
			// start the server thread
			this.serverThread = new Thread(new ThreadStart(OpenClient));
			this.serverThread.Start();
		}
		
		
		public override void Stop ()
		{
			this.CloseClient();
			this.serverThread.Abort();
		}
		
		#endregion

		#region SERVER

		/// <summary>
		/// Opens the server port, waiting for clients
		/// </summary>
		private void OpenClient()
		{
			this.client = new XmppClientConnection(Settings.Instance.JabberServer);
			this.client.Open(Settings.Instance.JabberUser, Settings.Instance.JabberPassword);
			this.client.OnLogin += delegate(object sender)
			{
				this.UpdateState(0);
			};
			this.client.OnError += delegate(object sender, Exception ex)
			{
				log.Fatal("OpenServer()", ex);
			};
			this.client.OnAuthError += delegate(object sender, Element e)
			{
				log.Fatal("OpenServer() " + e.ToString());
			};
		}

		private void CloseClient()
		{
			this.client.Close();
		}

		private void UpdateState(double aSpeed)
		{
			if(this.client == null) { return; }

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
			this.client.Send(p);
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectRepository_ObjectAddedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void ObjectRepository_ObjectRemovedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void ObjectRepository_ObjectChangedEventHandler(XGObject aObj)
		{
		}

		protected override void FileRepository_ObjectAddedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void FileRepository_ObjectRemovedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void FileRepository_ObjectChangedEventHandler(XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGFilePart))
			{
				double speed = 0;
				try
				{
					speed = (from file in this.Parent.FileRepository.Files from part in file.Parts select part.Speed).Sum();
				}
				catch  {}
				this.UpdateState(speed);
			}
		}

		#endregion
	}
}
