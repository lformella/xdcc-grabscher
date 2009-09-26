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
	public class BotWidget : PartableWidget
	{
		double myTimeODay = 8640000;
		double myTimeOWeek = 60480000;

		public BotWidget()
		{
			this.CreateColumn("", rPixbuf, new Gtk.TreeCellDataFunc(RenderBotIcon), 55);
			this.CreateColumn("Name", rText, new Gtk.TreeCellDataFunc(RenderObjectName), 0);
			this.CreateColumn("Speed", rTextRight, new Gtk.TreeCellDataFunc(RenderBotSpeed), 90);
			this.CreateColumn("Queued", rTextRight, new Gtk.TreeCellDataFunc(RenderBotQueue), 105);
			this.CreateColumn("Info Speed", rTextRight, new Gtk.TreeCellDataFunc(RenderBotInfoSpeed), 110);
			this.CreateColumn("Info Slot", rTextRight, new Gtk.TreeCellDataFunc(RenderBotInfoSlot), 80);
			this.CreateColumn("Info Queue", rTextRight, new Gtk.TreeCellDataFunc(RenderBotInfoQueue), 90);
			this.CreateColumn("Last Contact", rTextRight, new Gtk.TreeCellDataFunc(RenderBotLastContact), 100);
			
			//Tooltips tt = new Tooltips();
			//tt.SetTip(null, "", "");
		}

		#region FILTER

		protected override bool FilterObjects(Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				if(base.FilterObjects(model, iter))
				{
					switch (this.Filter)
					{
						case FilterType.Disable:
							if (this.myFilterObject == null || this.myFilterObject == tBot.Parent || this.myFilterObject == tBot.Parent.Parent)
							{
								return true;
							}
							break;
	
						case FilterType.Custom:
							foreach (XGPacket tPack in tBot.Children)
							{
								string name = tPack.Name.ToLower();
								bool add = true;
								string[] list = this.myFilterObject as string[];
								for (int i = 0; i < list.Length; i++)
								{
									if (!name.Contains(list[i]))
									{
										add = false;
										break;
									}
								}
								if (add) { return true; }
							}
							break;
	
						case FilterType.Downloads:
							if (tBot.BotState == BotState.Active) { return true; }
							break;
	
						case FilterType.EnabledPackets:
							foreach (XGPacket tPack in tBot.Children)
							{
								if (tPack.Enabled) { return true; }
							}
							break;
	
						case FilterType.ODay:
							foreach (XGPacket tPack in tBot.Children)
							{
								if ((DateTime.Now - tPack.LastUpdated).TotalMilliseconds <= this.myTimeODay) { return true; }
							}
							break;

						case FilterType.OWeek:
							foreach (XGPacket tPack in tBot.Children)
							{
								if ((DateTime.Now - tPack.LastUpdated).TotalMilliseconds <= this.myTimeOWeek) { return true; }
							}
							break;
	
						case FilterType.OpenSlots:
							if (tBot.Connected && tBot.InfoSlotTotal > 0 && tBot.InfoSlotCurrent > 0) { return true; }
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
			XGBot tObject1 = null;
			try { tObject1 = (XGBot)model.GetValue(iter1, 0); }
			catch (Exception) {}
			XGBot tObject2 = null;
			try { tObject2 = (XGBot)model.GetValue(iter2, 0); }
			catch (Exception) {}

			switch(this.mySortColumn)
			{
				case 2:
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
						else { return tPart1.Speed.CompareTo(tPart2.Speed); }
					}
				case 3:
					return tObject1.QueuePosition.CompareTo(tObject2.QueuePosition);
				case 4:
					return tObject1.InfoSpeedCurrent.CompareTo(tObject2.InfoSpeedCurrent);
				case 5:
					return tObject1.InfoSlotCurrent.CompareTo(tObject2.InfoSlotCurrent);
				case 6:
					return tObject1.InfoQueueCurrent.CompareTo(tObject2.InfoQueueCurrent);
				case 7:
					return tObject1.LastContact.CompareTo(tObject2.LastContact);
			}
			return XGHelper.CompareObjects(tObject1, tObject2);
		}

		#endregion

		#region RENDERER

		private void RenderBotIcon(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				Gtk.CellRendererPixbuf renderer = (cell as Gtk.CellRendererPixbuf);
				Gdk.Pixbuf pb = ImageLoaderGTK.Instance.pbBot;
				if (!tBot.Connected) { pb = ImageLoaderGTK.Instance.pbBotOff; }
				else if (tBot.BotState == BotState.Active)
				{
					XGFilePart tPart = this.GetPartToObject(tBot);
					if (tPart != null)
					{
						if (tPart.Speed < 1024 * 125) { pb = ImageLoaderGTK.Instance.pbBotDL0; }
						else if (tPart.Speed < 1024 * 250) { pb = ImageLoaderGTK.Instance.pbBotDL1; }
						else if (tPart.Speed < 1024 * 500) { pb = ImageLoaderGTK.Instance.pbBotDL2; }
						else if (tPart.Speed < 1024 * 750) { pb = ImageLoaderGTK.Instance.pbBotDL3; }
						else if (tPart.Speed < 1024 * 1000) { pb = ImageLoaderGTK.Instance.pbBotDL4; }
						else { pb = ImageLoaderGTK.Instance.pbBotDL5; }
					}
					else { pb = ImageLoaderGTK.Instance.pbBotDL0; }
				}
				else if (tBot.BotState == BotState.Waiting) { pb = ImageLoaderGTK.Instance.pbBotQueued; }
				else if (tBot.InfoSlotTotal > 0 && tBot.InfoSlotCurrent > 0) { pb = ImageLoaderGTK.Instance.pbBotFree; }
				else if (tBot.InfoSlotTotal > 0 && tBot.InfoSlotCurrent == 0) { pb = ImageLoaderGTK.Instance.pbBotFull; }
				renderer.Pixbuf = pb;
			}
		}

		private void RenderBotSpeed(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				XGFilePart tPart = this.GetPartToObject(tBot);
				if (tPart != null)
				{
					renderer.Text = WidgetHelper.Speed2Human(tPart.Speed);
				}
				else { renderer.Text = ""; }
			}
		}
		private void RenderBotQueue(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				if(tBot.BotState == BotState.Waiting)
				{
					renderer.Text = tBot.QueuePosition > 0 ? tBot.QueuePosition + (tBot.QueueTime > 0 ? " (" + WidgetHelper.Time2Human(tBot.QueueTime) + ")" : "") : "";
				}
				else { renderer.Text = ""; }
			}
		}

		private void RenderBotInfoSpeed(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				renderer.Text = tBot.InfoSpeedCurrent + " / " + tBot.InfoSpeedMax;
			}
		}

		private void RenderBotInfoSlot(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				renderer.Text = tBot.InfoSlotCurrent + " / " + tBot.InfoSlotTotal;
			}
		}

		private void RenderBotInfoQueue(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				renderer.Text = tBot.InfoQueueCurrent + " / " + tBot.InfoQueueTotal;
			}
		}

		private void RenderBotLastContact(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGBot tBot = null;
			try { tBot = (XGBot)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tBot != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				renderer.Text = tBot.LastContact.ToShortTimeString();
			}
		}

		#endregion
	}
}
