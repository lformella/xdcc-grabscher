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

using Gtk;
using XG.Client.GTK;

namespace XG.Client.Widgets.GTK
{
	public delegate void FilterDelegate(FilterType aFilter, object aObject);

	[System.ComponentModel.ToolboxItem(true)]
	public class SearchWidget : ViewWidget
	{
		private TreeStore mySpecialStore;
		public event FilterDelegate FilterActivated;

		public SearchWidget()
		{
			this.View.AppendColumn("", rPixbuf, "pixbuf", 0);
			this.View.AppendColumn("", rText, "text", 1);

			this.mySpecialStore = new Gtk.TreeStore(typeof(Gdk.Pixbuf), typeof(string));
			this.View.Model = this.mySpecialStore;
			this.mySpecialStore.AppendValues(ImageLoaderGTK.Instance.pbODay, "ODay Packets");
			this.mySpecialStore.AppendValues(ImageLoaderGTK.Instance.pbOWeek, "OWeek Packets");
			this.mySpecialStore.AppendValues(ImageLoaderGTK.Instance.pbBlind, "Show All");
			this.mySpecialStore.AppendValues(ImageLoaderGTK.Instance.pbBotDL0, "Downloads");
			this.mySpecialStore.AppendValues(ImageLoaderGTK.Instance.pbSearchSlots, "Open Slots");
			this.mySpecialStore.AppendValues(ImageLoaderGTK.Instance.pbOk, "Enabled Packets");
		}

		public void AddSearch(string aSearch)
		{
			Gtk.Application.Invoke(delegate
			{
				this.mySpecialStore.AppendValues(ImageLoaderGTK.Instance.pbSearch, aSearch);
			});
		}

		#region EVENTHANDLER VIEW

		protected override void treeViewCursorChanged(object o, System.EventArgs e)
		{
			TreeSelection selection = (o as TreeView).Selection;
			TreeModel model;
			TreeIter iter;
			if (selection.GetSelected(out model, out iter))
			{
				Gdk.Pixbuf pb = (Gdk.Pixbuf)model.GetValue(iter, 0);
				string name = (string)model.GetValue(iter, 1);

				if (pb == ImageLoaderGTK.Instance.pbBlind)
				{
					this.FilterActivated(FilterType.Disable, null);
				}
				else
				{

					if (pb == ImageLoaderGTK.Instance.pbODay)
					{
						this.FilterActivated(FilterType.ODay, null);
					}
					else if (pb == ImageLoaderGTK.Instance.pbOWeek)
					{
						this.FilterActivated(FilterType.OWeek, null);
					}
					else if (pb == ImageLoaderGTK.Instance.pbBotDL0)
					{
						this.FilterActivated(FilterType.Downloads, null);
					}
					else if (pb == ImageLoaderGTK.Instance.pbSearchSlots)
					{
						this.FilterActivated(FilterType.OpenSlots, null);
					}
					else if (pb == ImageLoaderGTK.Instance.pbOk)
					{
						this.FilterActivated(FilterType.EnabledPackets, null);
					}
					else if (pb == ImageLoaderGTK.Instance.pbSearch)
					{
						this.FilterActivated(FilterType.Custom, name);
					}
				}
			}
		}

		protected override void treeViewRowActivated(object o, Gtk.RowActivatedArgs args)
		{
			TreeSelection selection = (o as TreeView).Selection;
			TreeModel model;
			TreeIter iter;
			if (selection.GetSelected(out model, out iter))
			{
				Gdk.Pixbuf pb = (Gdk.Pixbuf)model.GetValue(iter, 0);
				if (pb == ImageLoaderGTK.Instance.pbSearch)
				{
					this.mySpecialStore.Remove(ref iter);
					this.FilterActivated(FilterType.Disable, null);
				}
			}
		}

		#endregion
	}
}
