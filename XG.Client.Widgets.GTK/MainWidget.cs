using System;
using System.Collections.Generic;
using Gtk;
using XG.Client.GTK;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class MainWidget : Gtk.Bin
	{
		#region EVENTS

		public event EmptyDelegate GetEnabledPacketsEvent;
		public event DataTextDelegate SearchPacketEvent;
		public event DataTextDelegate SearchPacketTimeEvent;
		public event ObjectDelegate ObjectFlippedEvent;
		public event ObjectDelegate GetChildrenEvent;

		public event DataTextDelegate ServerAddedEvent;
		public event StringGuidDelegate ChannelAddedEvent;

		public event GuidDelegate ServerRemovedEvent;
		public event GuidDelegate ChannelRemovedEvent;

		#endregion

		public bool Connected
		{
			set
			{
				this.hpanedIrc.Sensitive = value;
				this.vboxFile.Sensitive = value;

				if(!value)
				{
					this.serverchannelWidget.Clear();
					this.botWidget.Clear();
					this.packetWidget.Clear();
					this.fileWidget.Clear();
				}
			}
		}

		public MainWidget()
		{
			this.Build();

			#region evil hacks, dont try that at home kids!
			(((this.btnSearch.Children[0] as Gtk.Alignment).Child as Gtk.HBox).Children[0] as Gtk.Image).Pixbuf = ImageLoaderGTK.Instance.pbSearch;
			(((this.btnAddChannel.Children[0] as Gtk.Alignment).Child as Gtk.HBox).Children[0] as Gtk.Image).Pixbuf = ImageLoaderGTK.Instance.pbChannel;
			(((this.btnAddServer.Children[0] as Gtk.Alignment).Child as Gtk.HBox).Children[0] as Gtk.Image).Pixbuf = ImageLoaderGTK.Instance.pbServer;
			(((this.btnRemoveObject.Children[0] as Gtk.Alignment).Child as Gtk.HBox).Children[0] as Gtk.Image).Pixbuf = ImageLoaderGTK.Instance.pbRemove;
			#endregion

			#region EVENTS

			this.serverchannelWidget.ObjectClickedEvent += new ObjectDelegate(serverChannelWidget_ObjectClickedEventHandler);
			this.serverchannelWidget.ObjectDoubleClickedEvent += new ObjectDelegate(serverChannelWidget_ObjectDoubleClickedEventHandler);

			this.botWidget.ObjectClickedEvent += new ObjectDelegate(botWidget_ObjectClickedEventHandler);
			this.botWidget.ObjectDoubleClickedEvent += new ObjectDelegate(botWidget_ObjectDoubleClickedEventHandler);

			this.packetWidget.ObjectClickedEvent += new ObjectDelegate(packetWidget_ObjectClickedEventHandler);
			this.packetWidget.ObjectDoubleClickedEvent += new ObjectDelegate(packetWidget_ObjectDoubleClickedEventHandler);

			this.fileWidget.ObjectClickedEvent += new ObjectDelegate(fileWidget_ObjectClickedEventHandler);
			this.fileWidget.ObjectDoubleClickedEvent += new ObjectDelegate(fileWidget_ObjectDoubleClickedEventHandler);

			this.searchWidget.FilterActivated += new FilterDelegate(searchWidget_FilterActivatedEventHandler);

			#endregion
		}

		#region DATA HANDLING

		public void AddObject(XGObject aObj)
		{
			this.AddObject(aObj, null);
		}
		public void AddObject(XGObject aObj, XGObject aParent)
		{
			if (aObj.GetType() == typeof(XGServer))
			{
				this.serverchannelWidget.AddObject(aObj);
			}

			else if (aObj.GetType() == typeof(XGChannel))
			{
				this.serverchannelWidget.AddObject(aObj, aParent);
			}

			else if (aObj.GetType() == typeof(XGBot))
			{
				this.botWidget.AddObject(aObj);
			}

			else if (aObj.GetType() == typeof(XGPacket))
			{
				this.packetWidget.AddObject(aObj);
				this.botWidget.Refilter();
			}

			else if (aObj.GetType() == typeof(XGFile))
			{
				this.fileWidget.AddObject(aObj);
			}

			else if (aObj.GetType() == typeof(XGFilePart))
			{
				this.fileWidget.AddObject(aObj, aParent);
			}
		}

		public void ChangeObject(XGObject aObj, XGObject aParent)
		{
			if (aObj.GetType() == typeof(XGServer) || aObj.GetType() == typeof(XGChannel))
			{
				this.serverchannelWidget.ChangeObject(aObj);
			}
			else if (aObj.GetType() == typeof(XGBot))
			{
				this.botWidget.ChangeObject(aObj);
				this.packetWidget.Refilter();
			}
			else if (aObj.GetType() == typeof(XGPacket))
			{
				this.packetWidget.ChangeObject(aObj);
				this.botWidget.Refilter();
			}
			else if (aObj.GetType() == typeof(XGFile) || aObj.GetType() == typeof(XGFilePart))
			{
				if (aObj.GetType() == typeof(XGFilePart))
				{
					XGPacket tPacket = aParent as XGPacket;
					this.packetWidget.UpdatePart(aObj as XGFilePart, tPacket);
					this.botWidget.UpdatePart(aObj as XGFilePart, tPacket.Parent);
				}

				this.fileWidget.ChangeObject(aObj);
			}
		}

		public void RemoveObject(XGObject aObj)
		{
			try
			{
				if (aObj.GetType() == typeof(XGServer) || aObj.GetType() == typeof(XGChannel))
				{
					this.serverchannelWidget.RemoveObject(aObj);
				}

				else if (aObj.GetType() == typeof(XGBot))
				{
					this.botWidget.RemoveObject(aObj);
					foreach(XGPacket tPack in aObj.Children)
					{
						this.packetWidget.RemoveObject(tPack);
					}
				}

				else if (aObj.GetType() == typeof(XGPacket))
				{
					this.packetWidget.RemoveObject(aObj);
					this.botWidget.Refilter();
				}

				else if (aObj.GetType() == typeof(XGFile))
				{
					this.fileWidget.RemoveObject(aObj);
				}

				else if (aObj.GetType() == typeof(XGFilePart))
				{
					this.fileWidget.RemoveObject(aObj);
					this.packetWidget.RemovePart(aObj as XGFilePart);
					this.botWidget.RemovePart(aObj as XGFilePart);
				}
			}
			catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		}

		public void ObjectBlockStart()
		{
			this.botWidget.ObjectBlockStart();
			this.packetWidget.ObjectBlockStart();
			this.fileWidget.ObjectBlockStart();
		}

		public void ObjectBlockStop()
		{
			this.botWidget.ObjectBlockStop();
			this.packetWidget.ObjectBlockStop();
			this.fileWidget.ObjectBlockStop();
		}

		#endregion

		#region EVENTHANDLER

		protected virtual void btnSearch_Clicked(object sender, System.EventArgs e)
		{
			this.searchWidget.AddSearch(this.entrySearch.Text);
		}
		protected virtual void entrySearch_Activated(object sender, System.EventArgs e)
		{
			this.btnSearch_Clicked(sender, e);
		}

		protected virtual void btnAddServer_Clicked(object sender, System.EventArgs e)
		{
			TextDialog d = new TextDialog("Please insert a Server name!");
			d.Run();
			if (d.Result == DialogResult.OK && this.ServerAddedEvent != null)
			{
				this.ServerAddedEvent(d.GetInput());
			}
			d.Destroy();
		}

		protected virtual void btnAddChannel_Clicked(object sender, System.EventArgs e)
		{
			TextDialog d = new TextDialog("Please insert a Channel name!");
			d.Run();
			if (d.Result == DialogResult.OK)
			{
				XGObject tObj = this.serverchannelWidget.LastSelectedObject;
				if (tObj != null && this.ChannelAddedEvent != null)
				{
					this.ChannelAddedEvent(d.GetInput(), tObj.Guid);
				}
			}
			d.Destroy();
		}

		protected virtual void btnRemoveObject_Clicked(object sender, System.EventArgs e)
		{
			XGObject tObj = this.serverchannelWidget.LastSelectedObject;
			if (tObj != null)
			{
				if (tObj.GetType() == typeof(XGServer) && this.ServerRemovedEvent != null)
				{
					this.ServerRemovedEvent(tObj.Guid);
				}
				else if (tObj.GetType() == typeof(XGChannel) && this.ChannelRemovedEvent != null)
				{
					this.ChannelRemovedEvent(tObj.Guid);
				}
			}
		}

		#endregion

		#region EVENTHANDLER VIEWS

		void serverChannelWidget_ObjectClickedEventHandler(XGObject aObj)
		{
			if(this.botWidget.Filter == FilterType.Disable) { this.botWidget.SetFilter(FilterType.Disable, aObj); }
			if(this.packetWidget.Filter == FilterType.Disable) { this.packetWidget.SetFilter(FilterType.Disable, aObj); }

			if (aObj.GetType() == typeof(XGServer))
			{
				this.btnAddChannel.Sensitive = true;
				/*foreach (XGChannel tChan in (tObj as XGServer).Children)
				{
					if (!this.myCompleteObjects.Contains(tChan))
					{
						this.WriteData(TCPClientRequest.GetChildrenFromObject, tChan.Guid, null);
						this.myCompleteObjects.Add(tChan);
					}
				}*/
			}
			else if (aObj.GetType() == typeof(XGChannel))
			{
				this.btnAddChannel.Sensitive = false;

				if(this.GetChildrenEvent != null) { this.GetChildrenEvent(aObj); }
			}
		}
		void serverChannelWidget_ObjectDoubleClickedEventHandler(XGObject aObj)
		{
			if(this.ObjectFlippedEvent != null) { this.ObjectFlippedEvent(aObj); }
		}

		void botWidget_ObjectClickedEventHandler(XGObject aObj)
		{
			this.serverchannelWidget.SelectObject(aObj.Parent);
			if(this.packetWidget.Filter == FilterType.Disable) { this.packetWidget.SetFilter(FilterType.Disable, aObj); }

			this.GetChildrenEvent(aObj);
		}
		void botWidget_ObjectDoubleClickedEventHandler(XGObject aObj)
		{
		}

		void packetWidget_ObjectClickedEventHandler(XGObject aObj)
		{
			if(aObj.Parent != null)
			{
				this.botWidget.SelectObject(aObj.Parent);
				if(aObj.Parent.Parent != null)
				{
					this.serverchannelWidget.SelectObject(aObj.Parent.Parent);
				}
			}
		}
		void packetWidget_ObjectDoubleClickedEventHandler(XGObject aObj)
		{
			if(this.ObjectFlippedEvent != null) { this.ObjectFlippedEvent(aObj); }
		}

		void fileWidget_ObjectClickedEventHandler(XGObject aObj)
		{
		}
		void fileWidget_ObjectDoubleClickedEventHandler(XGObject aObj)
		{
		}

		void searchWidget_FilterActivatedEventHandler(FilterType aFilter, object aObject)
		{
			if (aFilter == FilterType.EnabledPackets)
			{
				this.GetEnabledPacketsEvent();
			}

			if (aFilter == FilterType.Custom)
			{
				string name = (aObject as string);
				this.SearchPacketEvent(name);
				aObject = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			}

			if (aFilter == FilterType.ODay)
			{
				this.SearchPacketTimeEvent("0-86400000");
			}

			if (aFilter == FilterType.OWeek)
			{
				this.SearchPacketTimeEvent("0-604800000");
			}

			this.botWidget.SetFilter(aFilter, aObject);
			this.packetWidget.SetFilter(aFilter, aObject);
		}

		protected virtual void chkOfflineBots_Toggled (object sender, System.EventArgs e)
		{
			this.botWidget.ShowOfflineObjects = this.chkOfflineBots.Active;
			this.packetWidget.ShowOfflineObjects = this.chkOfflineBots.Active;
		}

		protected virtual void chkFilesReady_Toggled (object sender, System.EventArgs e)
		{
			this.fileWidget.ShowOfflineObjects = this.chkFilesReady.Active;
		}

		#endregion
	}
}
