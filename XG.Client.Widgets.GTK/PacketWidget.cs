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
using Gtk;
using XG.Client.GTK;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	[System.ComponentModel.ToolboxItem(true)]
	public class PacketWidget : PartableWidget
	{
		double myTimeODay = 8640000;
		double myTimeOWeek = 60480000;

		public PacketWidget()
		{
			this.CreateColumn("", rPixbuf, new Gtk.TreeCellDataFunc(RenderPacketIcon), 55);
			this.CreateColumn("", rTextRight, new Gtk.TreeCellDataFunc(RenderPacketId), 35);
			this.CreateColumn("Name", rProgress, new Gtk.TreeCellDataFunc(RenderPacketNameProgress), 0);
			this.CreateColumn("Speed", rTextRight, new Gtk.TreeCellDataFunc(RenderPacketSpeed), 90);
			this.CreateColumn("Missing", rTextRight, new Gtk.TreeCellDataFunc(RenderPacketSizeMissing), 60);
			this.CreateColumn("Size", rTextRight, new Gtk.TreeCellDataFunc(RenderPacketSize), 60);
			this.CreateColumn("Time Left", rTextRight, new Gtk.TreeCellDataFunc(RenderPacketTimeLeft), 90);
		}

		#region FILTER

		protected override bool FilterObjects(Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				if(!this.ShowOfflineObjects && tPack.Parent != null && !tPack.Parent.Connected)
				{
					return false;
				}
				else
				{
					switch (this.Filter)
					{
						case FilterType.Disable:
							try
							{
								if (this.myFilterObject == null || this.myFilterObject == tPack.Parent || this.myFilterObject == tPack.Parent.Parent || this.myFilterObject == tPack.Parent.Parent.Parent)
								{
									return true;
								}
							}
							catch (NullReferenceException) { return false; }
							break;
	
						case FilterType.Custom:
							string name = tPack.Name.ToLower();
							string[] list = this.myFilterObject as string[];
							for (int i = 0; i < list.Length; i++)
							{
								if (!name.Contains(list[i])) { return false; }
							}
							return true;
	
						case FilterType.Downloads:
							if (tPack.Connected) { return true; }
							break;
	
						case FilterType.EnabledPackets:
							if (tPack.Enabled) { return true; }
							break;
	
						case FilterType.ODay:
							if ((DateTime.Now - tPack.LastUpdated).TotalMilliseconds <= this.myTimeODay) { return true; }
							break;

						case FilterType.OWeek:
							if ((DateTime.Now - tPack.LastUpdated).TotalMilliseconds <= this.myTimeOWeek) { return true; }
							break;

						case FilterType.OpenSlots:
							try
							{
								if (tPack.Parent.Connected && tPack.Parent.InfoSlotTotal > 0 && tPack.Parent.InfoSlotCurrent > 0) { return true; }
							}
							catch (NullReferenceException) { return false; }
							break;
					}
				}
			}
			return false;
		}

		#endregion

		#region SORT

		protected override int SortObjects(TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			XGPacket tObject1 = null;
			try { tObject1 = (XGPacket)model.GetValue(iter1, 0); }
			catch (Exception) {}
			XGPacket tObject2 = null;
			try { tObject2 = (XGPacket)model.GetValue(iter2, 0); }
			catch (Exception) {}

			switch(this.mySortColumn)
			{
				case 2:
					return tObject1.Name.CompareTo(tObject2.Name);
				case 5:
					if(tObject1.RealSize > 0) { tObject1.RealSize.CompareTo(tObject2.RealSize > 0 ? tObject2.RealSize : tObject2.Size); }
					else { tObject1.Size.CompareTo(tObject2.RealSize > 0 ? tObject2.RealSize : tObject2.Size); }
					break;
				case 3:
				case 4:
				case 6:
					XGFilePart tPart1 = this.GetPartToObject(tObject1);
					XGFilePart tPart2 = this.GetPartToObject(tObject2);
					if (tPart1 == null)
					{
						if (tPart2 == null) { return 0; }
						else { return -1; }
					}
					else
					{
						if (tPart2 == null) { return 1; }
						else
						{
							switch(this.mySortColumn)
							{
								case 3:
									return tPart1.Speed.CompareTo(tPart2.Speed);
								case 4:
									return tPart1.MissingSize.CompareTo(tPart2.MissingSize);
								case 6:
									return tPart1.TimeMissing.CompareTo(tPart2.TimeMissing);
							}
						}
					}
					break;
			}
			return XGHelper.CompareObjects(tObject1, tObject2);
		}

		#endregion

		#region RENDERER

		private void RenderPacketIcon(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				Gtk.CellRendererPixbuf renderer = (cell as Gtk.CellRendererPixbuf);
				Gdk.Pixbuf pb = ImageLoaderGTK.Instance.pbPacketDisabled;
				if (tPack.Connected)
				{
					XGFilePart tPart = this.GetPartToObject(tPack);
					if (tPart != null)
					{
						if (tPart.Speed < 1024 * 50) { pb = ImageLoaderGTK.Instance.pbPacketDL0; }
						else if (tPart.Speed < 1024 * 100) { pb = ImageLoaderGTK.Instance.pbPacketDL1; }
						else if (tPart.Speed < 1024 * 150) { pb = ImageLoaderGTK.Instance.pbPacketDL2; }
						else if (tPart.Speed < 1024 * 200) { pb = ImageLoaderGTK.Instance.pbPacketDL3; }
						else if (tPart.Speed < 1024 * 250) { pb = ImageLoaderGTK.Instance.pbPacketDL4; }
						else { pb = ImageLoaderGTK.Instance.pbPacketDL5; }
					}
					else { pb = ImageLoaderGTK.Instance.pbPacketDL0; }
				}
				else if (tPack.Enabled)
				{
					if (tPack.Parent != null && tPack.Parent.getOldestActivePacket() == tPack)
					{
						pb = ImageLoaderGTK.Instance.pbPacketQueued;
					}
					else { pb = ImageLoaderGTK.Instance.pbPacketNew; }
				}
				renderer.Pixbuf = pb;
			}
		}

		private void RenderPacketId(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				renderer.Text = "" + tPack.Id;
			}
		}

		private void RenderPacketNameProgress(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				CellRendererPacketProgress renderer = (cell as CellRendererPacketProgress);
				XGFilePart tPart = this.GetPartToObject(tPack);
				if (tPack != null)
				{
					renderer.Object = tPart;
				}
				else { renderer.Object = null; }
				renderer.Text = tPack.Name;
			}
		}

		private void RenderPacketSpeed(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				XGFilePart tPart = this.GetPartToObject(tPack);
				if (tPart != null)
				{
					renderer.Text = WidgetHelper.Speed2Human(tPart.Speed);
				}
				else { renderer.Text = ""; }
			}
		}

		private void RenderPacketSize(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				renderer.Text = WidgetHelper.Size2Human(tPack.RealSize > 0 ? tPack.RealSize : tPack.Size);
			}
		}

		private void RenderPacketSizeMissing(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				XGFilePart tPart = this.GetPartToObject(tPack);
				if (tPart != null)
				{
					renderer.Text = WidgetHelper.Size2Human(tPart.MissingSize);
				}
				else { renderer.Text = ""; }
			}
		}

		private void RenderPacketTimeLeft(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGPacket tPack = null;
			try { tPack = (XGPacket)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tPack != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				XGFilePart tPart = this.GetPartToObject(tPack);
				if (tPart != null)
				{
					renderer.Text = WidgetHelper.Time2Human(tPart.TimeMissing);
				}
				else { renderer.Text = ""; }
			}
		}

		#endregion
	}
}
