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
using GLib;
using Gtk;
using XG.Core;

namespace XG.Client.Widgets.GTK
{
	[Flags]
	public enum FilterType : short
	{
		Disable,
		Downloads,
		ODay,
		OWeek,
		OpenSlots,
		EnabledPackets,
		Custom
	}

	public partial class ViewWidget : Gtk.Bin
	{
		private Dictionary<XGObject, Gtk.TreeIter> myDictObjectIter;

		private TreeStore myObjectStore;
		private TreeModelFilter myObjectFilter;

		private int myColumnCounter = 0;
		protected CellRendererPixbuf rPixbuf;
		protected CellRendererPacketProgress rProgress;
		protected CellRendererText rText;
		protected CellRendererText rTextRight;

		private bool myObjectBlockMode = false;

		#region SORTABLES

		protected TreeModelSort myObjectSorter;
		protected int mySortColumn = 0;
		protected SortType mySortType = SortType.Ascending;

		#endregion

		#region EVENTS

		public event ObjectDelegate ObjectClickedEvent;
		public event ObjectDelegate ObjectDoubleClickedEvent;

		#endregion

		#region GET / SET

		private bool showOfflineObjects = true;
		public bool ShowOfflineObjects
		{
			get { return showOfflineObjects; }
			set
			{
				if (this.showOfflineObjects != value)
				{
					this.showOfflineObjects = value;
					this.Refilter();
				}
			}
		}

		private FilterType myFilter;
		public FilterType Filter
		{
			get { return this.myFilter; }
		}
		protected object myFilterObject;

		protected TreeView View
		{
			get { return this.treeView; }
		}

		private XGObject lastSelectedObject;
		public XGObject LastSelectedObject
		{
			get { return this.lastSelectedObject; }
		}

		#endregion

		#region INIT

		public ViewWidget()
		{
			this.Build();

			rPixbuf = new CellRendererPixbuf();
			rProgress = new CellRendererPacketProgress();
			rText = new CellRendererText();
			rTextRight = new CellRendererText();
			rTextRight.Xalign = 1.0f;

			this.myDictObjectIter = new Dictionary<XGObject, TreeIter>();
			this.myObjectStore = new TreeStore(typeof(XGObject));

			this.myObjectFilter = new Gtk.TreeModelFilter(this.myObjectStore, null);
			this.myObjectFilter.VisibleFunc = new Gtk.TreeModelFilterVisibleFunc(FilterObjects);
			this.myObjectSorter = new TreeModelSort(this.myObjectFilter);
			this.myObjectSorter.DefaultSortFunc = SortObjects;
			this.myObjectSorter.SetSortColumnId(0, SortType.Ascending);

			this.treeView.Model = this.myObjectSorter;

			GLib.ExceptionManager.UnhandledException += delegate(UnhandledExceptionArgs args)
			{
				Console.WriteLine(args.ExceptionObject.ToString());
			};
		}

		protected TreeViewColumn CreateColumn(string aName, CellRenderer aRenderer, TreeCellDataFunc aFunc, int aWidth)
		{
			TreeViewColumn col = this.View.AppendColumn("" + aName + "", aRenderer, aFunc);
			if (aWidth == 0) { col.Expand = true; }
			else { col.FixedWidth = aWidth; }
			col.Sizing = TreeViewColumnSizing.Fixed;
			if (myColumnCounter > 0)
			{
				col.Clicked += new EventHandler(ColumnClicked);
				col.SortColumnId = myColumnCounter;
				this.myObjectSorter.SetSortFunc(myColumnCounter, SortObjects);
			}
			myColumnCounter++;
			return col;
		}

		protected void ColumnClicked(object sender, EventArgs args)
		{
			TreeViewColumn col;

			if (sender is Gtk.TreeViewColumn)
			{
				foreach (TreeViewColumn tCol in this.View.Columns)
				{
					tCol.SortIndicator = false;
				}

				col = (sender as Gtk.TreeViewColumn);
				if (col.SortColumnId == this.mySortColumn)
				{
					if (this.mySortType == SortType.Ascending) { this.mySortType = SortType.Descending; }
					else { this.mySortType = SortType.Ascending; }
				}
				else
				{
					this.mySortColumn = col.SortColumnId;
					this.mySortType = SortType.Ascending;
				}
				col.SortIndicator = true;

				this.myObjectSorter.SetSortColumnId(this.mySortColumn, this.mySortType);
				this.myObjectSorter.ChangeSortColumn();
			}
		}

		#endregion

		#region FILTER

		protected virtual bool FilterObjects(Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObject = null;
			try { tObject = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tObject != null)
			{
				if (this.showOfflineObjects || tObject.Connected)
				{
					return true;
				}
			}
			return false;
		}

		public void Refilter()
		{
			if (!this.myObjectBlockMode)
			{
				Gtk.Application.Invoke(delegate
				{
					//this.treeView.QueueDraw();
					this.myObjectFilter.Refilter();
				});
			}
		}

		public void SetFilter(FilterType aFilter, object aObject)
		{
			this.myFilter = aFilter;
			this.myFilterObject = aObject;
			this.Refilter();
		}

		#endregion

		#region SORT

		protected virtual int SortObjects(TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			XGObject tObject1 = null;
			try { tObject1 = (XGObject)model.GetValue(iter1, 0); }
			catch (Exception) { }
			XGObject tObject2 = null;
			try { tObject2 = (XGObject)model.GetValue(iter2, 0); }
			catch (Exception) { }
			return XGHelper.CompareObjects(tObject1, tObject2);
		}

		#endregion

		#region RENDERER

		protected void RenderObjectName(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			XGObject tObj = null;
			try { tObj = (XGObject)model.GetValue(iter, 0); }
			catch (Exception) { }
			if (tObj != null)
			{
				Gtk.CellRendererText renderer = (cell as Gtk.CellRendererText);
				renderer.Text = tObj.Name + (tObj.Children.Length > 0 ? " (" + tObj.Children.Length + ")" : "");
				//if(tObj.Enabled) { renderer.Sensitive = false; }
				//else { renderer.Sensitive = false; }
			}
		}

		#endregion

		#region ADD / CHANGE / REMOVE / SELECT / CLEAR

		public void AddObject(XGObject aObject)
		{
			this.AddObject(aObject, null);
		}
		public void AddObject(XGObject aObject, XGObject aParent)
		{
			Gtk.Application.Invoke(delegate
			{
				TreeIter ti;
				if (aParent == null || !this.myDictObjectIter.ContainsKey(aParent)) { ti = this.myObjectStore.AppendValues(aObject); }
				else { ti = this.myObjectStore.AppendValues(this.myDictObjectIter[aParent], aObject); }
				this.myDictObjectIter.Add(aObject, ti);
			});
			this.Refilter();
		}

		public void ObjectBlockStart()
		{
			this.myObjectBlockMode = true;
		}

		public void ObjectBlockStop()
		{
			this.myObjectBlockMode = false;
			this.Refilter();
		}

		public void ChangeObject(XGObject aObject)
		{
			this.Refilter();
		}

		public void RemoveObject(XGObject aObject)
		{
			TreeIter ti = this.myDictObjectIter[aObject];
			this.myDictObjectIter.Remove(aObject);
			Gtk.Application.Invoke(delegate { this.myObjectStore.Remove(ref ti); });
			this.Refilter();
		}

		public void SelectObject(XGObject aObject)
		{
			try
			{
				TreeIter ti = this.myDictObjectIter[aObject];
				TreePath tp = this.myObjectStore.GetPath(ti);
				tp = this.myObjectFilter.ConvertChildPathToPath(tp);
				this.treeView.Selection.SelectPath(tp);
				/*TreeIter ti = this.myDictObjectIter[aObject.Parent];
				this.treeView.Selection.SelectIter(ti);*/
			}
			catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		}

		public void Clear()
		{
			try
			{
				this.myDictObjectIter.Clear();
				this.myFilter = FilterType.Disable;
				this.myFilterObject = null;
				this.myObjectStore.Clear();
			}
			catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		}

		#endregion

		#region EVENTHANDLER VIEW

		protected virtual void treeViewCursorChanged(object o, System.EventArgs e)
		{
			TreeSelection selection = (o as TreeView).Selection;
			TreeModel model;
			TreeIter iter;
			if (selection.GetSelected(out model, out iter))
			{
				XGObject tObj = (XGObject)model.GetValue(iter, 0);
				if (tObj != null)
				{
					this.lastSelectedObject = tObj;
					if (this.ObjectClickedEvent != null) { this.ObjectClickedEvent(tObj); }
				}
			}
		}

		protected virtual void treeViewRowActivated(object o, Gtk.RowActivatedArgs args)
		{
			TreeSelection selection = (o as TreeView).Selection;
			TreeModel model;
			TreeIter iter;
			if (selection.GetSelected(out model, out iter))
			{
				XGObject tObj = (XGObject)model.GetValue(iter, 0);
				if (tObj != null)
				{
					this.lastSelectedObject = tObj;
					if (this.ObjectDoubleClickedEvent != null) { this.ObjectDoubleClickedEvent(tObj); }
				}
			}
		}

		#endregion
	}
}
