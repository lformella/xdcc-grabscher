using System;
using Gtk;
using XG.Client.GTK;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	[System.ComponentModel.ToolboxItem(true)]
	public class FileWidget : ViewWidget
	{
		public FileWidget()
		{
			this.CreateColumn("", rPixbuf, new Gtk.TreeCellDataFunc(RenderFileIcon), 70);
			this.CreateColumn("Name", rText, new Gtk.TreeCellDataFunc(RenderFileName), 0);
			this.CreateColumn("Speed", rTextRight, new Gtk.TreeCellDataFunc(RenderFileSpeed), 90);
			this.CreateColumn("Progress", rProgress, new Gtk.TreeCellDataFunc(RenderFileProgress), 250).Resizable = true;
			this.CreateColumn("Missing", rTextRight, new Gtk.TreeCellDataFunc(RenderFileSizeMissing), 60);
			this.CreateColumn("Size", rTextRight, new Gtk.TreeCellDataFunc(RenderFileSize), 60);
			this.CreateColumn("Time Left", rTextRight, new Gtk.TreeCellDataFunc(RenderFileTimeLeft), 90);
		}

		#region FILTER

		protected override bool FilterObjects(Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObject = null;
			try { tObject = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObject != null)
			{
				if(this.ShowOfflineObjects || tObject.GetType() == typeof(XGFilePart)) { return true; }
				else
				{
					foreach(XGFilePart tPart in tObject.Children)
					{
						if(tPart.PartState != FilePartState.Ready)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		#endregion

		#region SORT

		protected override int SortObjects(TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			XGObject tObject1 = null;
			try { tObject1 = (XGObject)model.GetValue(iter1, 0); }
			catch (Exception) {}
			XGObject tObject2 = null;
			try { tObject2 = (XGObject)model.GetValue(iter2, 0); }
			catch (Exception) {}

			if(tObject1.GetType() == typeof(XGFilePart) && tObject2.GetType() == typeof(XGFilePart))
			{
				XGFilePart tPart1 = tObject1 as XGFilePart;
				XGFilePart tPart2 = tObject2 as XGFilePart;
				switch(this.mySortColumn)
				{
					case 2:
						return tPart1.Speed.CompareTo(tPart2.Speed);
					case 3:
						return tPart1.StartSize.CompareTo(tPart2.StartSize);
					case 4:
						return tPart1.MissingSize.CompareTo(tPart2.MissingSize);
					case 5:
						return (tPart1.StopSize - tPart1.StartSize).CompareTo(tPart2.StopSize - tPart2.StartSize);
					case 6:
						return tPart1.TimeMissing.CompareTo(tPart2.TimeMissing);
				}
			}
			else if(tObject1.GetType() == typeof(XGFile) && tObject2.GetType() == typeof(XGFile))
			{
				XGFile tFile1 = tObject1 as XGFile;
				XGFile tFile2 = tObject2 as XGFile;
				switch(this.mySortColumn)
				{
					case 2:
						return this.GetSpeed(tFile1).CompareTo(this.GetSpeed(tFile2));
					case 3:
					case 4:
						return this.GetMissingSize(tFile1).CompareTo(this.GetMissingSize(tFile2));
					case 5:
						return tFile1.Size.CompareTo(tFile2.Size);
					case 6:
						return this.GetTimeMissing(tFile1).CompareTo(this.GetTimeMissing(tFile2));
				}
			}
			return XGHelper.CompareObjects(tObject1, tObject2);
		}

		#endregion

		#region RENDERER

		private void RenderFileIcon(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				Gtk.CellRendererPixbuf renderer = (cell as Gtk.CellRendererPixbuf);
				Gdk.Pixbuf pb = ImageLoaderGTK.Instance.pbPacket;
				if(tObj.GetType() == typeof(XGFile))
				{
					XGFile tFile = tObj as XGFile;
					double speed = this.GetSpeed(tFile);
					if(speed > 0)
					{
						if (speed < 1024 * 125) { pb = ImageLoaderGTK.Instance.pbPacketDL0; }
						else if (speed < 1024 * 250) { pb = ImageLoaderGTK.Instance.pbPacketDL1; }
						else if (speed < 1024 * 500) { pb = ImageLoaderGTK.Instance.pbPacketDL2; }
						else if (speed < 1024 * 750) { pb = ImageLoaderGTK.Instance.pbPacketDL3; }
						else if (speed < 1024 * 1000) { pb = ImageLoaderGTK.Instance.pbPacketDL4; }
						else { pb = ImageLoaderGTK.Instance.pbPacketDL5; }
					}
				}
				else if(tObj.GetType() == typeof(XGFilePart))
				{
					XGFilePart tPart = tObj as XGFilePart;
					if(tPart.PartState == FilePartState.Open)
					{
						if (tPart.Speed < 1024 * 125) { pb = ImageLoaderGTK.Instance.pbPacketDL0; }
						else if (tPart.Speed < 1024 * 250) { pb = ImageLoaderGTK.Instance.pbPacketDL1; }
						else if (tPart.Speed < 1024 * 500) { pb = ImageLoaderGTK.Instance.pbPacketDL2; }
						else if (tPart.Speed < 1024 * 750) { pb = ImageLoaderGTK.Instance.pbPacketDL3; }
						else if (tPart.Speed < 1024 * 1000) { pb = ImageLoaderGTK.Instance.pbPacketDL4; }
						else { pb = ImageLoaderGTK.Instance.pbPacketDL5; }
					}
					else if (tPart.PartState == FilePartState.Ready)
					{
						pb = tPart.IsChecked ? ImageLoaderGTK.Instance.pbPacketReady1 : ImageLoaderGTK.Instance.pbPacketReady0;
					}
					else if (tPart.PartState == FilePartState.Broken)
					{
						pb = ImageLoaderGTK.Instance.pbPacketBroken;
					}
				}
				renderer.Pixbuf = pb;
			}
		}

		private void RenderFileName(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				if(tObj.GetType() == typeof(XGFile))
				{
					renderer.Text = tObj.Name;
				}
				else if(tObj.GetType() == typeof(XGFilePart))
				{
					XGFilePart tPart = tObj as XGFilePart;
					if(tPart.PartState == FilePartState.Ready) { renderer.Text = ""; }
					else { renderer.Text = tPart.Packet != null ? (tPart.Packet.Parent != null ? tPart.Packet.Parent.Name :  "") :  ""; }
				}
			}
		}

		private void RenderFileSpeed(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				if(tObj.GetType() == typeof(XGFile))
				{
					XGFile tFile = tObj as XGFile;
					double speed = this.GetSpeed(tFile);
					renderer.Text = WidgetHelper.Speed2Human(speed);
				}
				else if(tObj.GetType() == typeof(XGFilePart))
				{
					XGFilePart tPart = tObj as XGFilePart;
					renderer.Text = WidgetHelper.Speed2Human(tPart.Speed);
				}
			}
		}

		private void RenderFileProgress(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				CellRendererPacketProgress renderer = (cell as CellRendererPacketProgress);
				renderer.Text = null;
				renderer.Object = tObj;
			}
		}

		private void RenderFileSize(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				if(tObj.GetType() == typeof(XGFile))
				{
					XGFile tFile = tObj as XGFile;
					renderer.Text = WidgetHelper.Size2Human(tFile.Size);
				}
				else if(tObj.GetType() == typeof(XGFilePart))
				{
					XGFilePart tPart = tObj as XGFilePart;
					renderer.Text = WidgetHelper.Size2Human(tPart.StopSize - tPart.StartSize);
				}
			}
		}

		private void RenderFileSizeMissing(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				if(tObj.GetType() == typeof(XGFile))
				{
					XGFile tFile = tObj as XGFile;
					Int64 size = this.GetMissingSize(tFile);
					renderer.Text = WidgetHelper.Size2Human(size);
				}
				else if(tObj.GetType() == typeof(XGFilePart))
				{
					XGFilePart tPart = tObj as XGFilePart;
					renderer.Text = WidgetHelper.Size2Human(tPart.MissingSize);
				}
			}
		}

		private void RenderFileTimeLeft(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				if(tObj.GetType() == typeof(XGFile))
				{
					XGFile tFile = tObj as XGFile;
					Int64 left = this.GetTimeMissing(tFile);
					renderer.Text = left != Int64.MaxValue ? WidgetHelper.Time2Human(left) : "";
				}
				else if(tObj.GetType() == typeof(XGFilePart))
				{
					XGFilePart tPart = tObj as XGFilePart;
					renderer.Text = tPart.TimeMissing != Int64.MaxValue ? WidgetHelper.Time2Human(tPart.TimeMissing) : "";
				}
			}
		}

		#endregion

		#region HELPER

		private double GetSpeed(XGFile aFile)
		{
			double speed = 0;
			foreach(XGFilePart tPart in aFile.Children)
			{
				speed += tPart.Speed;
			}
			return speed;
		}

		private Int64 GetMissingSize(XGFile aFile)
		{
			Int64 size = 0;
			foreach(XGFilePart tPart in aFile.Children)
			{
				size += tPart.MissingSize;
			}
			return size;
		}

		private Int64 GetTimeMissing(XGFile aFile)
		{
			Int64 left = 0;
			foreach(XGFilePart tPart in aFile.Children)
			{
				if(tPart.PartState != FilePartState.Ready)
				{
					left = tPart.TimeMissing > left ? tPart.TimeMissing : left;
				}
			}
			return left;
		}

		#endregion
	}
}
