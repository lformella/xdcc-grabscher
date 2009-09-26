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
		private RootObject myRootObject;

		private List<string> myCompleteSearches;
		private List<XGObject> myCompleteObjects;
		private List<XGObject> myParentlessObjects;

		private Thread myThread;
		private TCPClient myClient;

		private StatusIcon myTrayIcon;

		private bool myEnabledPacketsFetched = false;

		#region INIT

		public MainWindow()
			: base(Gtk.WindowType.Toplevel)
		{
			Build();

			this.myParentlessObjects = new List<XGObject>();
			this.myRootObject = new RootObject();
			this.statusWidget.RootObject = this.myRootObject;

			this.myCompleteSearches = new List<string>();
			this.myCompleteObjects = new List<XGObject>();

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
			b.ActionState = ActionState.Active;
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
			p1.State = PartState.Ready;
			p1.IsChecked = true;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 4000000;
			p1.CurrentSize = 6000000;
			p1.StopSize = 6000000;
			p1.State = PartState.Ready;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 0000000;
			p1.CurrentSize = 1500000;
			p1.StopSize = 2000000;
			p1.State = PartState.Open;
			p1.Speed = 164687;
			p1.Packet = pa;
			p1.IsChecked = true;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 6000000;
			p1.CurrentSize = 7500000;
			p1.StopSize = 8000000;
			p1.State = PartState.Open;
			p1.Speed = 244666;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 8000000;
			p1.CurrentSize = 8500000;
			p1.StopSize = 9000000;
			p1.State = PartState.Broken;
			this.myClient_ObjectAddedOrChanged(p1);
			p1 = new XGFilePart(f1);
			p1.StartSize = 2000000;
			p1.CurrentSize = 2500000;
			p1.StopSize = 4000000;
			p1.State = PartState.Closed;
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

			this.myClient.RootGuidReceivedEvent += new GuidDelegate(myClient_RootGuidReceived);
			this.myClient.ConnectedEvent += new EmptyDelegate(myClient_Connected);
			this.myClient.ConnectionErrorEvent += new DataTextDelegate(myClient_ConnectionError);
			this.myClient.DisconnectedEvent += new EmptyDelegate(myClient_Disconnected);
			this.myClient.ObjectAddedEvent += new ObjectDelegate(myClient_ObjectAddedOrChanged);
			this.myClient.ObjectChangedEvent += new ObjectDelegate(myClient_ObjectAddedOrChanged);
			this.myClient.ObjectRemovedEvent += new GuidDelegate(myClient_ObjectRemoved);
			this.myClient.ObjectBlockStartEvent += new EmptyDelegate(myClient_ObjectBlockStart);
			this.myClient.ObjectBlockStopEvent += new EmptyDelegate(myClient_ObjectBlockStop);

			this.myClient.Connect(this.entryServer.Text, int.Parse(this.entryPort.Text), this.entryPassword.Text);
		}

		private void myClient_Connected()
		{
			this.btnDisconnect.Sensitive = true;
			this.mainWidget.Connected = true;

			this.WriteData(TCPClientRequest.GetFiles, Guid.Empty, null);
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
				this.WriteData(TCPClientRequest.CloseClient, Guid.Empty, null);
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
				this.myClient.RootGuidReceivedEvent -= new GuidDelegate(myClient_RootGuidReceived);
				this.myClient.ConnectedEvent -= new EmptyDelegate(myClient_Connected);
				this.myClient.ConnectionErrorEvent -= new DataTextDelegate(myClient_ConnectionError);
				this.myClient.DisconnectedEvent -= new EmptyDelegate(myClient_Disconnected);
				this.myClient.ObjectAddedEvent -= new ObjectDelegate(myClient_ObjectAddedOrChanged);
				this.myClient.ObjectChangedEvent -= new ObjectDelegate(myClient_ObjectAddedOrChanged);
				this.myClient.ObjectRemovedEvent -= new GuidDelegate(myClient_ObjectRemoved);
				this.myClient.ObjectBlockStartEvent -= new EmptyDelegate(myClient_ObjectBlockStart);
				this.myClient.ObjectBlockStopEvent -= new EmptyDelegate(myClient_ObjectBlockStop);
			}
			catch (Exception ex) { Console.WriteLine(ex.ToString()); }

			#region CLEAR THINGS

			this.myParentlessObjects.Clear();
			this.myCompleteSearches.Clear();
			this.myCompleteObjects.Clear();

			this.myEnabledPacketsFetched = false;

			this.myRootObject = new RootObject();

			this.statusWidget.RootObject = this.myRootObject;
			this.statusWidget.Update();

			#endregion

			this.myClient = null;
		}

		private void WriteData(TCPClientRequest aMessage, Guid aGuid, string aData)
		{
			if (this.myClient != null)
			{
				this.myClient.WriteData(aMessage, aGuid, aData);
			}
		}

		#endregion

		#region DATA HANDLING

		void myClient_RootGuidReceived(Guid aGuid)
		{
			this.myRootObject.SetGuid(aGuid);
		}

		void myClient_ObjectAddedOrChanged(XGObject aObj)
		{
			XGObject oldObj = this.myRootObject.getChildByGuid(aObj.Guid);
			if (oldObj != null)
			{
				XGHelper.CloneObject(aObj, oldObj, true);

				if (aObj.GetType() == typeof(XGServer) ||
					aObj.GetType() == typeof(XGChannel) ||
					aObj.GetType() == typeof(XGBot) ||
					aObj.GetType() == typeof(XGPacket) ||
					aObj.GetType() == typeof(XGFile))
				{
					this.mainWidget.ChangeObject(aObj, null);
				}
				else if (aObj.GetType() == typeof(XGFilePart))
				{
					XGFilePart tPart = aObj as XGFilePart;
					XGPacket tPack = this.myRootObject.getChildByGuid(tPart.PacketGuid) as XGPacket;
					if(tPack != null)
					{
						this.mainWidget.ChangeObject(aObj, tPack);
					}
				}
				this.statusWidget.Update();
			}
			else
			{
				XGObject parentObj = this.myRootObject.getChildByGuid(aObj.ParentGuid);
				if (parentObj != null || aObj.ParentGuid == Guid.Empty)
				{
					this.AddObject(aObj);
					foreach (XGObject obj in this.myParentlessObjects.ToArray())
					{
						if (obj.ParentGuid == aObj.Guid)
						{
							this.myParentlessObjects.Remove(obj);
							this.AddObject(obj);
						}
					}
				}
				else
				{
					// just ask once on multiple parentless objects needing the same parent
					bool ask = true;
					foreach (XGObject tObj in this.myParentlessObjects.ToArray())
					{
						if (tObj.ParentGuid == aObj.ParentGuid)
						{
							ask = false;
							break;
						}
					}
					if (ask) { this.WriteData(TCPClientRequest.GetObject, aObj.ParentGuid, null); }
					this.myParentlessObjects.Add(aObj);
				}
			}
		}

		void myClient_ObjectRemoved(Guid aGuid)
		{
			XGObject remObj = this.myRootObject.getChildByGuid(aGuid);
			if (remObj != null)
			{
				XGHelper.Log("Removing " + remObj.GetType() + " - " + remObj.Name, LogLevel.Notice);

				try
				{
					if (remObj.GetType() == typeof(XGServer))
					{
						XGServer tServ = remObj as XGServer;
						this.myRootObject.removeServer(tServ);
					}

					else if (remObj.GetType() == typeof(XGChannel))
					{
						XGChannel tChan = remObj as XGChannel;
						tChan.Parent.removeChannel(tChan);
					}

					else if (remObj.GetType() == typeof(XGBot))
					{
						XGBot tBot = remObj as XGBot;
						tBot.Parent.removeBot(tBot);
					}

					else if (remObj.GetType() == typeof(XGPacket))
					{
						XGPacket tPack = remObj as XGPacket;
						tPack.Parent.removePacket(tPack);
					}

					else if (remObj.GetType() == typeof(XGFile))
					{
						XGFile tFile = remObj as XGFile;
						this.myRootObject.removeChild(tFile);
					}

					else if (remObj.GetType() == typeof(XGFilePart))
					{
						XGFilePart tPart = remObj as XGFilePart;
						tPart.Parent.removePart(tPart);
					}

					this.mainWidget.RemoveObject(remObj);
				}
				catch (Exception ex) { Console.WriteLine(ex.ToString()); }
			}
		}

		private void AddObject(XGObject aObj)
		{
			if (aObj.GetType() == typeof(XGServer))
			{
				this.myRootObject.addServer(aObj as XGServer);
				this.mainWidget.AddObject(aObj);
			}

			else if (aObj.GetType() == typeof(XGChannel))
			{
				XGServer tServ = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGServer;
				tServ.addChannel(aObj as XGChannel);
				this.mainWidget.AddObject(aObj, tServ);
			}

			else if (aObj.GetType() == typeof(XGBot))
			{
				XGChannel tChan = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGChannel;
				XGBot tBot = aObj as XGBot;
				tChan.addBot(tBot);
				this.mainWidget.AddObject(aObj);
			}

			else if (aObj.GetType() == typeof(XGPacket))
			{
				XGBot tBot = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGBot;
				XGPacket tPack = aObj as XGPacket;
				tBot.addPacket(tPack);
				this.mainWidget.AddObject(aObj);
			}

			else if (aObj.GetType() == typeof(XGFile))
			{
				this.myRootObject.addChild(aObj as XGFile);
				this.mainWidget.AddObject(aObj);
			}

			else if (aObj.GetType() == typeof(XGFilePart))
			{
				XGFile tFile = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGFile;
				XGFilePart tPart = aObj as XGFilePart;
				tFile.addPart(tPart);
				this.mainWidget.AddObject(aObj, tFile);

				XGPacket tPack = this.myRootObject.getChildByGuid(tPart.PacketGuid) as XGPacket;
				if(tPack != null)
				{
					this.mainWidget.ChangeObject(tPart, tPack);
					tPart.Packet = tPack;
				}
			}

			this.statusWidget.Update();
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
			if (!this.myCompleteObjects.Contains(aObj))
			{
				this.WriteData(TCPClientRequest.GetChildrenFromObject, aObj.Guid, null);
				this.myCompleteObjects.Add(aObj);
			}
		}

		private void ObjectFlippedEventHandler(XGObject aObj)
		{
			if (aObj != null)
			{
				if (!aObj.Enabled) { this.WriteData(TCPClientRequest.ActivateObject, aObj.Guid, null); }
				else { this.WriteData(TCPClientRequest.DeactivateObject, aObj.Guid, null); }
			}
		}

		private void GetEnabledPacketsEventHandler()
		{
			if (!this.myEnabledPacketsFetched)
			{
				if (this.myClient != null)
				{
					this.myEnabledPacketsFetched = true;
					this.WriteData(TCPClientRequest.GetActivePackets, Guid.Empty, null);
				}
			}
		}

		private void SearchPacketEventHandler(string aSearch)
		{
			if (!this.myCompleteSearches.Contains(aSearch))
			{
				if (this.myClient != null)
				{
					this.myCompleteSearches.Add(aSearch);
					this.WriteData(TCPClientRequest.SearchPacket, Guid.Empty, aSearch);
				}
			}
		}

		private void SearchPacketTimeEventHandler(string aSearch)
		{
			if (!this.myCompleteSearches.Contains(aSearch))
			{
				if (this.myClient != null)
				{
					this.myCompleteSearches.Add(aSearch);
					this.WriteData(TCPClientRequest.SearchPacketTime, Guid.Empty, aSearch);
				}
			}
		}

		private void ServerAddedEventHandler(string aData)
		{
			this.WriteData(TCPClientRequest.AddServer, Guid.Empty, aData);
		}

		private void ChannelAddedEventHandler(string aData, Guid aGuid)
		{
			this.WriteData(TCPClientRequest.AddChannel, aGuid, aData);
		}

		private void ServerRemovedEventHandler(Guid aGuid)
		{
			this.WriteData(TCPClientRequest.RemoveServer, aGuid, null);
		}

		private void ChannelRemovedEventHandler(Guid aGuid)
		{
			this.WriteData(TCPClientRequest.RemoveChannel, aGuid, null);
		}

		#endregion
	}
}
