// 
//  AObjects.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Serialization;

namespace XG.Core
{
	[Serializable]
	public abstract class AObjects : AObject
	{
		#region EVENTS

		[field: NonSerialized]
		public event EventHandler<EventArgs<AObject, AObject>> OnAdded;

		protected void FireAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			if (OnAdded != null)
			{
				OnAdded(aSender, aEventArgs);
			}
		}

		[field: NonSerialized]
		public event EventHandler<EventArgs<AObject, AObject>> OnRemoved;

		protected void FireRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			if (OnRemoved != null)
			{
				OnRemoved(aSender, aEventArgs);
			}
		}

		#endregion

		#region PROPERTIES

		ICollection<AObject> _children;

		[field: NonSerialized]
		ConcurrentDictionary<Guid, AObject> _realChildren;

		protected ICollection<AObject> All
		{
			get { return _realChildren.Values; }
		}

		#endregion

		#region FUNCTIONS

		protected bool Add(AObject aObject)
		{
			bool result = false;
			if (aObject != null)
			{
				if (!_realChildren.ContainsKey(aObject.Guid) && !DuplicateChildExists(aObject))
				{
					result = _realChildren.TryAdd(aObject.Guid, aObject);
				}

				if (result)
				{
					aObject.Parent = this;

					aObject.OnEnabledChanged += FireEnabledChanged;
					aObject.OnChanged += FireChanged;

					var aObjects = aObject as AObjects;
					if (aObjects != null)
					{
						aObjects.OnAdded += FireAdded;
						aObjects.OnRemoved += FireRemoved;
					}
					FireAdded(this, new EventArgs<AObject, AObject>(this, aObject));
				}
			}
			return result;
		}

		protected bool Remove(AObject aObject)
		{
			bool result = false;
			if (aObject != null)
			{
				if (_realChildren.ContainsKey(aObject.Guid))
				{
					AObject obj = null;
					result = _realChildren.TryRemove(aObject.Guid, out obj);
				}

				if (result)
				{
					aObject.Parent = null;

					aObject.OnEnabledChanged -= FireEnabledChanged;
					aObject.OnChanged -= FireChanged;

					var aObjects = aObject as AObjects;
					if (aObjects != null)
					{
						aObjects.OnAdded -= FireAdded;
						aObjects.OnRemoved -= FireRemoved;
					}
					FireRemoved(this, new EventArgs<AObject, AObject>(this, aObject));
				}
			}
			return result;
		}

		public virtual AObject WithGuid(Guid aGuid)
		{
			if (aGuid == Guid.Empty)
			{
				return null;
			}
			if (Guid == aGuid)
			{
				return this;
			}

			AObject tObjectReturn = null;
			foreach (AObject tObject in All)
			{
				if (tObject.Guid == aGuid)
				{
					tObjectReturn = tObject;
					break;
				}
				var tObjects = tObject as AObjects;
				if (tObjects != null)
				{
					tObjectReturn = tObjects.WithGuid(aGuid);
					if (tObjectReturn != null)
					{
						break;
					}
				}
			}
			return tObjectReturn;
		}

		public virtual AObject Named(string aName)
		{
			try
			{
				return All.FirstOrDefault(obj => obj.Name.Trim().ToLower() == aName.Trim().ToLower());
			}
			catch (Exception)
			{
				return null;
			}
		}

		public abstract bool DuplicateChildExists(AObject aObject);

		[OnDeserialized]
		void OnDeserialized(StreamingContext context)
		{
			_realChildren = new ConcurrentDictionary<Guid, AObject>();
			if (_children == null)
			{
				return;
			}

			foreach (AObject tObject in _children)
			{
				if (tObject == null)
				{
					continue;
				}
				_realChildren.TryAdd(tObject.Guid, tObject);

				tObject.OnEnabledChanged += FireEnabledChanged;
				tObject.OnChanged += FireChanged;

				var tObjects = tObject as AObjects;
				if (tObjects != null)
				{
					tObjects.OnAdded += FireAdded;
					tObjects.OnRemoved += FireRemoved;
				}
			}
		}

		[OnSerializing]
		void OnSerializing(StreamingContext context)
		{
			_children = All.ToArray();
		}

		#endregion

		#region CONSTRUCTOR

		protected AObjects()
		{
			_realChildren = new ConcurrentDictionary<Guid, AObject>();
		}

		#endregion
	}
}
