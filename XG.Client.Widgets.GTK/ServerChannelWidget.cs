using System;
using XG.Client.GTK;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	[System.ComponentModel.ToolboxItem(true)]
	public class ServerChannelWidget : ViewWidget
	{
		public ServerChannelWidget()
		{
			this.CreateColumn("", rPixbuf, new Gtk.TreeCellDataFunc(RenderObjectIcon), 70);
			this.CreateColumn("", rText, new Gtk.TreeCellDataFunc(RenderObjectName), 0);
		}

		#region RENDERER

		private void RenderObjectIcon(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) {}
			if (tObj != null)
			{
				Gtk.CellRendererPixbuf renderer = (cell as Gtk.CellRendererPixbuf);
				if (tObj.GetType() == typeof(XGServer))
				{
					if (tObj.Enabled)
					{
						if (tObj.Connected) { renderer.Pixbuf = ImageLoaderGTK.Instance.pbServerConnected; }
						else { renderer.Pixbuf = ImageLoaderGTK.Instance.pbServer; }
					}
					else { renderer.Pixbuf = ImageLoaderGTK.Instance.pbServerDisconnected; }
				}
				else if (tObj.GetType() == typeof(XGChannel))
				{
					if (tObj.Enabled)
					{
						if (tObj.Connected) { renderer.Pixbuf = ImageLoaderGTK.Instance.pbChannelConnected; }
						else { renderer.Pixbuf = ImageLoaderGTK.Instance.pbChannel; }
					}
					else { renderer.Pixbuf = ImageLoaderGTK.Instance.pbChannelDisconnected; }
				}
			}
		}

		#endregion
	}
}
