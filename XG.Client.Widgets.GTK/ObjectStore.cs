using Gtk;
using XG.Core;

namespace XG.Client.Widgets.GTK 
{
   public class ObjectStore
   {
      private bool tree;
      private ListStore myListStore;
      private TreeStore myTreeStore;

      public TreeModel Model
      {
         get
         {
            if(this.tree) { return this.myTreeStore; }
            else { return this.myListStore; }
         }
      }

      public bool IsTree
      {
         get { return this.tree; }
      }

      public ObjectStore(bool aTree)
      {
         if(this.tree) { this.myTreeStore = new TreeStore(typeof(XGObject)); }
         else { this.myListStore = new ListStore(typeof(XGObject)); }
      }

      public TreeIter AppendValues(XGObject aObject)
      {
         if(this.tree) { return this.myTreeStore.AppendValues(aObject); }
         else { return this.myListStore.AppendValues(aObject); }
      }

      public TreeIter AppendValues(TreeIter aIter, XGObject aObject)
      {
         if(this.tree) { return this.myTreeStore.AppendValues(aIter, aObject); }
         else { return this.myListStore.AppendValues(aIter, aObject); }
      }

      public bool Remove(ref TreeIter aIter)
      {
         if(this.tree) { return this.myTreeStore.Remove(ref aIter); }
         else { return this.myListStore.Remove(ref aIter); }
      }

      public void Clear()
      {
         if(this.tree) { this.myTreeStore.Clear(); }
         else { this.myListStore.Clear(); }
      }
   }
}
