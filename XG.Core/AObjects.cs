// 
//  AObjects.cs
//  
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// 

using System;
using System.Collections.Generic;
using System.Linq;

namespace XG.Core
{
	public delegate void ObjectsDelegate(AObjects aObjects, AObject aObject);
	
	[Serializable()]
	public class AObjects : AObject
	{
		#region EVENTS

		[field: NonSerialized()]
		public event ObjectsDelegate Added;

		protected void FireAdded(AObjects aObjects, AObject aObject)
		{
			if(Added != null)
			{
				Added(aObjects, aObject);
			}
		}

		[field: NonSerialized()]
		public event ObjectsDelegate Removed;

		protected void FireRemoved(AObjects aObjects, AObject aObject)
		{
			if(Removed != null)
			{
				Removed(aObjects, aObject);
			}
		}

		#endregion

		#region PROPERTIES
		
		[field: NonSerialized()]
		object _objectLock = new object();
		protected object ObjectLock
		{
			get
			{
				if(_objectLock == null)
				{
					_objectLock = new object();
				}
				return _objectLock;
			}
		}

		List<AObject> _children;
		protected IEnumerable<AObject> All
		{
			get { return _children.ToArray(); }
		}

		public override bool Connected
		{
			get { return base.Connected; }
			set
			{
				if (!value)
				{
					foreach (AObject obj in All)
					{
						obj.Connected = value;
					}
				}
				base.Connected = value;
			}
		}

		#endregion

		#region FUNCTIONS

		protected bool Add(AObject aObject)
		{
			if (aObject != null)
			{
				lock(ObjectLock)
				{
					if (!_children.Contains(aObject))
					{
						AObject tObj = ByGuid(aObject.Guid);
						if (tObj == null)
						{
							_children.Add(aObject);
							aObject.Parent = this;

							aObject.EnabledChanged += new ObjectDelegate(FireEnabledChanged);
							aObject.Changed += new ObjectDelegate(FireChanged);

							if(aObject is AObjects)
							{
								AObjects aObjects = (AObjects)aObject;

								aObjects.Added += new ObjectsDelegate(FireAdded);
								aObjects.Removed += new ObjectsDelegate(FireRemoved);
							}
							FireAdded(this, aObject);
							aObject.Modified = false;
	
							return true;
						}
					}
				}
			}
			return false;
		}

		protected bool Remove(AObject aObject)
		{
			if (aObject != null)
			{
				lock(ObjectLock)
				{
					if (_children.Contains(aObject))
					{
						_children.Remove(aObject);
						aObject.Parent = null;

						aObject.EnabledChanged -= new ObjectDelegate(FireEnabledChanged);
						aObject.Changed -= new ObjectDelegate(FireChanged);

						if(aObject is AObjects)
						{
							AObjects aObjects = (AObjects)aObject;

							aObjects.Added -= new ObjectsDelegate(FireAdded);
							aObjects.Removed -= new ObjectsDelegate(FireRemoved);
						}
						FireRemoved(this, aObject);
						aObject.Modified = false;
	
						return true;
					}
				}
			}
			return false;
		}

		public void AttachChildEvents()
		{
			foreach (AObject tObject in All)
			{
				tObject.EnabledChanged += new ObjectDelegate(FireEnabledChanged);
				tObject.Changed += new ObjectDelegate(FireChanged);
				
				if(tObject is AObjects)
				{
					AObjects tObjects = (AObjects)tObject;

					tObjects.Added += new ObjectsDelegate(FireAdded);
					tObjects.Removed += new ObjectsDelegate(FireRemoved);
	
					tObjects.AttachChildEvents();
				}
			}
		}

		public virtual AObject ByGuid(Guid aGuid)
		{
			if (aGuid == Guid.Empty) { return null; }
			if (Guid == aGuid) { return this; }

			AObject tObjectReturn = null;
			foreach (AObject tObject in All)
			{
				if (tObject.Guid == aGuid)
				{
					tObjectReturn = tObject;
					break;
				}
				else if(tObject is AObjects)
				{
					AObjects tObjects = (AObjects)tObject;

					tObjectReturn = tObjects.ByGuid(aGuid);
					if (tObjectReturn != null) { break; }
				}
			}
			return tObjectReturn;
		}

		public virtual AObject ByName(string aName)
		{
			AObject tObject = null;
			try
			{
				tObject = All.First(obj => obj.Name.Trim().ToLower() == aName.Trim().ToLower());
			}
			catch {}
			return tObject;
		}

		public AObject Next(AObject aObject)
		{
			if (_children.Contains(aObject))
			{
				bool next = false;
				foreach (AObject tObj in All)
				{
					if (tObj == aObject) { next = true; }
					else if (next) { return tObj; }
				}
			}
			return null;
		}

		#endregion

		#region CONSTRUCTOR

		public AObjects()
		{
			_children = new List<AObject>();
		}

		#endregion
	}
}

