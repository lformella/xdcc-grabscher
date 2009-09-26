using System;
using System.Collections.Generic;

namespace XG.Core
{
	[Serializable()]
	public class XGObject
	{
		[field: NonSerialized()]
		public event ObjectDelegate EnabledChangedEvent;

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

		private Guid guid;
		public Guid Guid
		{
			get { return this.guid; }
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
				// disable all children
				if (!value)
				{
					foreach (XGObject tObj in this.Children)
					{
						tObj.Enabled = value;
					}
				}
				if (this.enabled != value)
				{
					this.enabled = value;
					if (this.enabled)
					{
						this.lastModified = DateTime.Now;
					}
					if (this.EnabledChangedEvent != null)
					{
						this.EnabledChangedEvent(this);
					}
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
			set
			{
				if (this.lastModified != value)
				{
					this.lastModified = value;
					this.Modified = true;
				}
			}
		}

		private bool modified;
		public bool Modified
		{
			get { return this.modified; }
			set { this.modified = value; }
		}

		private List<XGObject> children;
		public XGObject[] Children
		{
			get
			{
				// wtf???
				// System.ArgumentException: Destination array was not long enough. Check destIndex and length, and the array's lower bounds
				try { return this.children.ToArray(); }
				catch(Exception) { return new XGObject[0]; }
			}
		}
		
		#region ALTER CHILDREN

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aObject">
		/// A <see cref="XGObject"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool addChild(XGObject aObject)
		{
			if(aObject != null)
			{
				if (!this.children.Contains(aObject))
				{
					XGObject tObj = this.getChildByGuid(aObject.Guid);
					if (tObj != null)
					{
						XGHelper.CloneObject(aObject, tObj, true);
						//this.children.Sort(XGHelper.CompareObjects);
					}
					else
					{
						this.children.Add(aObject);
						//this.children.Sort(XGHelper.CompareObjects);
						aObject.Parent = this;
						return true;
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="aObject">
		/// A <see cref="XGObject"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool removeChild(XGObject aObject)
		{
			if(aObject != null)
			{
				if (this.children.Contains(aObject))
				{
					this.children.Remove(aObject);
					return true;
				}
			}
			return false;
		}
		
		#endregion

		public XGObject getChildByGuid(Guid aGuid)
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
					tObj = o.getChildByGuid(aGuid);
					if (tObj != null) { break; }
				}
			}
			return tObj;
		}

		public XGObject getNextChild(XGObject aObject)
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

		protected void SetGuid(Guid aGuid)
		{
			this.guid = aGuid;
		}

		public XGObject()
		{
			this.name = "";
			this.children = new List<XGObject>();
			this.guid = Guid.NewGuid();
			this.connected = false;
			this.enabled = false;
		}

		public void Clone(XGObject aCopy, bool aFull)
		{
			this.parentGuid = aCopy.parentGuid;
			this.guid = aCopy.guid;
			this.name = aCopy.name;
			this.enabled = aCopy.enabled;
			this.lastModified = aCopy.lastModified;
			if (aFull)
			{
				this.connected = aCopy.connected;
			}
		}
	}
}
