//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
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
using System.Collections.Generic;
using System.Threading;
using Gtk;
using XG.Client.GTK;
using XG.Core;
#if MONO1
using Notifications;
#endif

namespace XG.Client.TCP.GTK
{
	public partial class MainWindow : Gtk.Window
	{
		#region VARIABLES

		private Thread myThread;
		private TCPClient myClient;

		private StatusIcon myTrayIcon;

		#endregion

		#region INIT

		public MainWindow()
			: base(Gtk.WindowType.Toplevel)
		{
			Build();

			#region MAINWIDGET EVENTS

			this.mainWidget.GetEnabledPacketsEvent += new EmptyDelegate(GetEnabledPacketsEventHandler);
			this.mainWidget.SearchPacketEvent += new DataTextDelegate(SearchPacketEventHandler);
			this.mainWidget.SearchPacketTimeEvent += new DataTextDelegate(SearchPacketTimeEventHandler);
			this.mainWidget.ObjectFlippedEvent += new ObjectDelegate(ObjectFlippedEventHandler);
			this.mainWidget.GetChildrenEvent += new ObjectDelegate(GetChildren);

			this.mainWidget.ServerAddedEvent += new DataTextDelegate(ServerAddedEventHandler);
			this.mainWidget.ChannelAddedEvent += new StringGuidDelegate(ChannelAddedEventHandler);

			this.mainWidget.ServerRemovedEvent += new GuidDelegate(ServerRemovedEventHandler);
			this.mainWidget.ChannelRemovedEvent += new GuidDelegate(ChannelRemovedEventHandler);

			#endregion

			#region GUI FIXES

			this.Icon = ImageLoaderGTK.Instance.pbClient;

			// evil hacks, dont try that at home kids!
			(((this.btnConnect.Children[0] as Gtk.Alignment).Child as Gtk.HBox).Children[0] as Gtk.Image).Pixbuf = ImageLoaderGTK.Instance.pbConnect;
			(((this.btnDisconnect.Children[0] as Gtk.Alignment).Child as Gtk.HBox).Children[0] as Gtk.Image).Pixbuf = ImageLoaderGTK.Instance.pbDisconnect;

			#endregion

			#region TRAY ICON

			this.myTrayIcon = new StatusIcon(ImageLoaderGTK.Instance.pbClient);
			this.myTrayIcon.Visible = true;
			this.myTrayIcon.Activate += delegate { this.Visible = !this.Visible; };
			this.myTrayIcon.PopupMenu += OnTrayIconPopup;
			this.myTrayIcon.Tooltip = "XG rocks the shit fat, twice!";
			/*
						NotifyIcon ni = new NotifyIcon();
						ni.Visible = true;
						ni.ShowBalloonTip(2000, "Help", "You are infected", ToolTipIcon.Info);
			*/

#if MONO1
			Notification myNote = new Notification();
			myNote.IconName = Stock.Harddisk; 
			myNote.Summary = "Download complete"; 
			myNote.Body = "File " + " is complete";
			myNote.Show();
#endif

			#endregion

			#region SETTINGS

			this.entryPassword.Text = ClientSettings.Default.Password;
			this.entryPort.Text = ClientSettings.Default.Port.ToString();
			this.entryServer.Text = ClientSettings.Default.Server;

			#endregion

			this.mainWidget.Connected = false;

			#region TEST
			/** /

			XGFile f1 = new XGFile("test.tar", 10000000);
			this.myClient_ObjectAddedOrChanged(XGHelper.CloneObject(f1, true));

			XGServer s = new XGServer();
			s.Name = "serv";
			s.Connected = true;
			s.Enabled = true;
			this.myClient_ObjectAddedOrChanged(XGHelper.CloneObject(s, true));

			XGChannel c = new XGChannel(s);
			c.Name = "chan";
			c.Connected = true;
			c.Enabled = true;
			this.myClient_ObjectAddedOrChanged(XGHelper.CloneObject(c, true));

			XGBot b = new XGBot(c);
			b.Name = "bot";
			b.Connected = true;
			b.BotState = BotState.Active;
			this.myClient_ObjectAddedOrChanged(XGHelper.CloneObject(b, true));

			XGPacket pa = new XGPacket(b);
			pa.Name = "test.tar";
			pa.Connected = true;
			pa.Enabled = true;
			pa.Size = 1000000;
			this.myClient_ObjectAddedOrChanged(XGHelper.CloneObject(pa, true));

			pa = new XGPacket(b);
			pa.Name = "der film des jahrtausend avi rip flip mpeg mc dubbed 160 clubbed bombe.tar";
			pa.Connected = true;
			pa.Enabled = true;
			pa.Size = 1000000;
			this.myClient_ObjectAddedOrChanged(XGHelper.CloneObject(pa, true));

			XGFilePart p1 = null;

			p1 = new XGFilePart(f1);
			p1.StartSize = 9000000;
			p1.CurrentSize = 10000000;
			p1.StopSize = 10000000;
			p1.PartState = FilePartState.Ready;
			p1.IsChecked = true;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 4000000;
			p1.CurrentSize = 6000000;
			p1.StopSize = 6000000;
			p1.PartState = FilePartState.Ready;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 0000000;
			p1.CurrentSize = 1500000;
			p1.StopSize = 2000000;
			p1.PartState = FilePartState.Open;
			p1.Speed = 164687;
			p1.Packet = pa;
			p1.IsChecked = true;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 6000000;
			p1.CurrentSize = 7500000;
			p1.StopSize = 8000000;
			p1.PartState = FilePartState.Open;
			p1.Speed = 244666;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 8000000;
			p1.CurrentSize = 8500000;
			p1.StopSize = 9000000;
			p1.PartState = FilePartState.Broken;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 2000000;
			p1.CurrentSize = 2500000;
			p1.StopSize = 4000000;
			p1.PartState = FilePartState.Closed;
			this.myClient_ObjectAddedOrChanged(p1);

			/**/
			#endregion
		}

		#endregion

		#region CONNECTION

		private void Connect()
		{
			this.btnConnect.Sensitive = false;

			this.myClient = new TCPClient();

			this.myClient.ConnectedEvent += new EmptyDelegate(myClient_Connected);
			this.myClient.ConnectionErrorEvent += new DataTextDelegate(myClient_ConnectionError);
			this.myClient.DisconnectedEvent += new EmptyDelegate(myClient_Disconnected);
			this.myClient.ObjectAddedEvent += new ObjectObjectDelegate(myClient_ObjectAdded);
			this.myClient.ObjectChangedEvent += new ObjectDelegate(myClient_ObjectChanged);
			this.myClient.ObjectRemovedEvent += new ObjectDelegate(myClient_ObjectRemoved);
			this.myClient.ObjectBlockStartEvent += new EmptyDelegate(myClient_ObjectBlockStart);
			this.myClient.ObjectBlockStopEvent += new EmptyDelegate(myClient_ObjectBlockStop);

			this.myClient.Connect(this.entryServer.Text, int.Parse(this.entryPort.Text), this.entryPassword.Text);
		}

		private void myClient_Connected()
		{
			this.btnDisconnect.Sensitive = true;
			this.mainWidget.Connected = true;
			this.statusWidget.RootObject = this.myClient.RootObject;

			this.myClient.GetFiles();
		}
		private void myClient_ConnectionError(string aData)
		{
			XGHelper.Log(aData, LogLevel.Exception);

			this.btnConnect.Sensitive = true;
			this.btnDisconnect.Sensitive = false;
			this.mainWidget.Connected = false;
		}

		private void Disconnect()
		{
			if (this.myClient != null)
			{
				this.myClient.Disconnect();
			}
		}

		private void myClient_Disconnected()
		{
			this.btnConnect.Sensitive = true;
			this.btnDisconnect.Sensitive = false;
			this.mainWidget.Connected = false;

			try
			{
				this.myClient.ConnectedEvent -= new EmptyDelegate(myClient_Connected);
				this.myClient.ConnectionErrorEvent -= new DataTextDelegate(myClient_ConnectionError);
				this.myClient.DisconnectedEvent -= new EmptyDelegate(myClient_Disconnected);
				this.myClient.ObjectAddedEvent -= new ObjectObjectDelegate(myClient_ObjectAdded);
				this.myClient.ObjectChangedEvent -= new ObjectDelegate(myClient_ObjectChanged);
				this.myClient.ObjectRemovedEvent -= new ObjectDelegate(myClient_ObjectRemoved);
				this.myClient.ObjectBlockStartEvent -= new EmptyDelegate(myClient_ObjectBlockStart);
				this.myClient.ObjectBlockStopEvent -= new EmptyDelegate(myClient_ObjectBlockStop);
			}
			catch (Exception ex) { Console.WriteLine(ex.ToString()); }

			this.statusWidget.RootObject = new RootObject();
			this.statusWidget.Update();
			this.myClient = null;
		}

		#endregion

		#region DATA HANDLING

		void myClient_ObjectAdded(XGObject aObj, XGObject aParent)
		{
			this.mainWidget.AddObject(aObj, aParent);

			if (aObj.GetType() == typeof(XGFilePart))
			{
				XGFilePart tPart = aObj as XGFilePart;
				XGPacket tPack = this.myClient.RootObject.getChildByGuid(tPart.PacketGuid) as XGPacket;
				if (tPack != null)
				{
					this.mainWidget.ChangeObject(tPart, tPack);
				}
			}

			this.statusWidget.Update();
		}

		void myClient_ObjectChanged(XGObject aObj)
		{
			if (aObj.GetType() == typeof(XGFilePart))
			{
				XGFilePart tPart = aObj as XGFilePart;
				XGPacket tPack = this.myClient.RootObject.getChildByGuid(tPart.PacketGuid) as XGPacket;
				if (tPack != null)
				{
					this.mainWidget.ChangeObject(aObj, tPack);
				}
			}
			else
			{
				this.mainWidget.ChangeObject(aObj, null);
			}

			this.statusWidget.Update();
		}

		void myClient_ObjectRemoved(XGObject aObj)
		{
			this.mainWidget.RemoveObject(aObj);
		}

		private void myClient_ObjectBlockStart()
		{
			this.mainWidget.ObjectBlockStart();
		}

		private void myClient_ObjectBlockStop()
		{
			this.mainWidget.ObjectBlockStop();
		}

		#endregion

		#region EVENTHANDLER

		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			this.myTrayIcon.Visible = false;
			this.Disconnect();
			Gtk.Application.Quit();
			a.RetVal = true;
		}

		protected virtual void btnConnectClicked(object sender, System.EventArgs e)
		{
			this.myThread = new Thread(new ThreadStart(Connect));
			this.myThread.Start();

			#region SETTINGS

			ClientSettings.Default.Password = this.entryPassword.Text;
			ClientSettings.Default.Port = int.Parse(this.entryPort.Text);
			ClientSettings.Default.Server = this.entryServer.Text;
			ClientSettings.Default.Save();

			#endregion
		}
		protected virtual void entryServerActivated(object sender, System.EventArgs e)
		{
			this.btnConnectClicked(sender, e);
		}
		protected virtual void entryPortActivated(object sender, System.EventArgs e)
		{
			this.btnConnectClicked(sender, e);
		}
		protected virtual void entryPasswordActivated(object sender, System.EventArgs e)
		{
			this.btnConnectClicked(sender, e);
		}

		protected virtual void btnDisconnectHandler(object sender, System.EventArgs e)
		{
			this.Disconnect();
		}

		protected void OnTrayIconPopup(object o, EventArgs args)
		{
			Gtk.Menu popupMenu = new Gtk.Menu();

			ImageMenuItem menuItemQuit = new ImageMenuItem("Quit");
			menuItemQuit.Image = new Gtk.Image(ImageLoaderGTK.Instance.pbNo);
			popupMenu.Add(menuItemQuit);
			menuItemQuit.Activated += delegate { Gtk.Application.Quit(); };

			popupMenu.ShowAll();
			popupMenu.Popup();
		}

		#endregion

		#region EVENTHANDLER MAINWIDGET

		private void GetChildren(XGObject aObj)
		{
			if (this.myClient != null)
			{
				this.myClient.GetChildren(aObj);
			}
		}

		private void ObjectFlippedEventHandler(XGObject aObj)
		{
			if (this.myClient != null)
			{
				this.myClient.FlipObject(aObj);
			}
		}

		private void GetEnabledPacketsEventHandler()
		{
			if (this.myClient != null)
			{
				this.myClient.GetEnabledPackets();
			}
		}

		private void SearchPacketEventHandler(string aSearch)
		{
			if (this.myClient != null)
			{
				this.myClient.SearchPacket(aSearch);
			}
		}

		private void SearchPacketTimeEventHandler(string aSearch)
		{
			if (this.myClient != null)
			{
				this.myClient.SearchPacketTime(aSearch);
			}
		}

		private void ServerAddedEventHandler(string aData)
		{
			if (this.myClient != null)
			{
				this.myClient.AddServer(aData);
			}
		}

		private void ChannelAddedEventHandler(string aData, Guid aGuid)
		{
			if (this.myClient != null)
			{
				this.myClient.AddChannel(aData, aGuid);
			}
		}

		private void ServerRemovedEventHandler(Guid aGuid)
		{
			if (this.myClient != null)
			{
				this.myClient.RemoveServer(aGuid);
			}
		}

		private void ChannelRemovedEventHandler(Guid aGuid)
		{
			if (this.myClient != null)
			{
				this.myClient.RemoveChannel(aGuid);
			}
		}

		#endregion
	}
}
