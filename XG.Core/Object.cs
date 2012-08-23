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
using System.Linq;

namespace XG.Core
{
	[Serializable()]
	public class XGObject
	{
		#region EVENTS

		[field: NonSerialized()]
		public event ObjectDelegate EnabledChangedEvent;

		[field: NonSerialized()]
		public event ObjectDelegate ObjectChangedEvent;

		[field: NonSerialized()]
		public event ObjectObjectDelegate ChildAddedEvent;

		[field: NonSerialized()]
		public event ObjectObjectDelegate ChildRemovedEvent;

		#endregion

		#region VARIABLES

		private Guid parentGuid;
		public Guid ParentGuid
		{
			get { return parentGuid; }
		}

		private XGObject parent;
		public XGObject Parent
		{
			get { return this.parent; }
			set
			{
				if (this.parent != value)
				{
					this.parent = value;
					if (this.parent != null) { this.parentGuid = this.parent.Guid; }
					else { this.parentGuid = Guid.Empty; }
				}
			}
		}

		public Guid Guid
		{
			get;
			set;
		}

		private bool connected;
		public virtual bool Connected
		{
			get { return this.connected; }
			set
			{
				if (this.connected != value)
				{
					this.connected = value;
					this.Modified = true;
				}
			}
		}

		private bool enabled;
		public bool Enabled
		{
			get { return this.enabled; }
			set
			{
				if (this.enabled != value)
				{
					// disable all children
					if (!value)
					{
						foreach (XGObject tObj in this.Children)
						{
							tObj.Enabled = value;
						}
					}

					this.enabled = value;

					// just set the time if this object is enabled
					if (this.enabled)
					{
						this.lastModified = DateTime.Now;
					}
					this.FireEnabledChangedEvent(this);
				}
			}
		}

		private string name;
		public string Name
		{
			get { return this.name; }
			set
			{
				if (this.name != value)
				{
					this.name = value;
					this.Modified = true;
				}
			}
		}

		private DateTime lastModified = new DateTime(1, 1, 1);
		public DateTime LastModified
		{
			get { return this.lastModified; }
		}

		private bool modified;
		public bool Modified
		{
			get { return this.modified; }
			set { this.modified = value; }
		}

		public void Commit ()
		{
			if (this.modified)
			{
				this.FireObjectChangedEvent(this);
				this.modified = false;
			}
		}

		#endregion

		#region CHILDREN
		
		[field: NonSerialized()]
		private object objectLock = new object();
		protected object ObjectLock
		{
			get
			{
				if(this.objectLock == null)
				{
					this.objectLock = new object();
				}
				return this.objectLock;
			}
		}

		private List<XGObject> children;
		protected IEnumerable<XGObject> Children
		{
			get { return this.children.ToArray(); }
		}

		protected bool AddChild(XGObject aObject)
		{
			if (aObject != null)
			{
				lock(this.ObjectLock)
				{
					if (!this.children.Contains(aObject))
					{
						XGObject tObj = this.GetChildByGuid(aObject.Guid);
						if (tObj != null)
						{
							//XGHelper.CloneObject(aObject, tObj, true);
						}
						else
						{
							this.children.Add(aObject);
							aObject.Parent = this;
		
							// attach to child events
							aObject.EnabledChangedEvent += new ObjectDelegate(this.FireEnabledChangedEvent);
							aObject.ObjectChangedEvent += new ObjectDelegate(this.FireObjectChangedEvent);
							aObject.ChildAddedEvent += new ObjectObjectDelegate(this.FireChildAddedEvent);
							aObject.ChildRemovedEvent += new ObjectObjectDelegate(this.FireChildRemovedEvent);
	
							// and fire our own
							this.FireChildAddedEvent(this, aObject);
							aObject.Modified = false;
	
							return true;
						}
					}
				}
			}
			return false;
		}

		protected bool RemoveChild(XGObject aObject)
		{
			if (aObject != null)
			{
				lock(this.ObjectLock)
				{
					if (this.children.Contains(aObject))
					{
						this.children.Remove(aObject);
						aObject.Parent = null;
							
						// detach to child events
						aObject.EnabledChangedEvent -= new ObjectDelegate(this.FireEnabledChangedEvent);
						aObject.ObjectChangedEvent -= new ObjectDelegate(this.FireObjectChangedEvent);
						aObject.ChildAddedEvent -= new ObjectObjectDelegate(this.FireChildAddedEvent);
						aObject.ChildRemovedEvent -= new ObjectObjectDelegate(this.FireChildRemovedEvent);
	
						// and fire our own
						this.FireChildRemovedEvent(this, aObject);
						aObject.Modified = false;
	
						return true;
					}
				}
			}
			return false;
		}

		public void AttachCildEvents()
		{
			foreach (XGObject child in this.Children)
			{
				child.EnabledChangedEvent += new ObjectDelegate(this.FireEnabledChangedEvent);
				child.ObjectChangedEvent += new ObjectDelegate(this.FireObjectChangedEvent);
				child.ChildAddedEvent += new ObjectObjectDelegate(this.FireChildAddedEvent);
				child.ChildRemovedEvent += new ObjectObjectDelegate(this.FireChildRemovedEvent);

				child.AttachCildEvents();
			}
		}

		public XGObject GetChildByGuid(Guid aGuid)
		{
			if (aGuid == Guid.Empty) { return null; }
			if (this.Guid == aGuid) { return this; }

			XGObject tObj = null;
			foreach (XGObject o in this.Children)
			{
				if (o.Guid == aGuid)
				{
					tObj = o;
					break;
				}
				else
				{
					tObj = o.GetChildByGuid(aGuid);
					if (tObj != null) { break; }
				}
			}
			return tObj;
		}

		public XGObject GetNextChild(XGObject aObject)
		{
			if (this.children.Contains(aObject))
			{
				bool next = false;
				foreach (XGObject tObj in this.Children)
				{
					if (tObj == aObject) { next = true; }
					else if (next) { return tObj; }
				}
			}
			return null;
		}

		#endregion

		#region EVENTHANDLER

		private void FireEnabledChangedEvent(XGObject aObj)
		{
			if(this.EnabledChangedEvent != null)
			{
				this.EnabledChangedEvent(aObj);
			}
		}

		private void FireObjectChangedEvent(XGObject aObj)
		{
			if(this.ObjectChangedEvent != null)
			{
				this.ObjectChangedEvent(aObj);
			}
		}

		private void FireChildAddedEvent(XGObject aParent, XGObject aObj)
		{
			if(this.ChildAddedEvent != null)
			{
				this.ChildAddedEvent(aParent, aObj);
			}
		}

		private void FireChildRemovedEvent(XGObject aParent, XGObject aObj)
		{
			if(this.ChildRemovedEvent != null)
			{
				this.ChildRemovedEvent(aParent, aObj);
			}
		}

		#endregion

		#region CONSTRUCTOR

		public XGObject()
		{
			this.name = "";
			this.children = new List<XGObject>();
			this.Guid = Guid.NewGuid();
			this.connected = false;
			this.enabled = false;
		}

		#endregion
	}
}
