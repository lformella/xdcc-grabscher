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
					else { renderer.Pixbuf = ImageLoaderGTK.Instance.pbServerDisabled; }
				}
				else if (tObj.GetType() == typeof(XGChannel))
				{
					if (tObj.Enabled)
					{
						if (tObj.Connected) { renderer.Pixbuf = ImageLoaderGTK.Instance.pbChannelConnected; }
						else { renderer.Pixbuf = ImageLoaderGTK.Instance.pbChannel; }
					}
					else { renderer.Pixbuf = ImageLoaderGTK.Instance.pbChannelDisabled; }
				}
			}
		}

		#endregion
	}
}
