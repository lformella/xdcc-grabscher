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
using System.Linq;
using Db4objects.Db4o;
using Db4objects.Db4o.Activation;
using Db4objects.Db4o.Collections;
using XG.Extensions;

namespace XG.Model.Domain
{
	public abstract class AObjects : AObject
	{
		#region EVENTS

		[Transient]
		public event EventHandler<EventArgs<AObject, AObject>> OnAdded = delegate {};

		protected void FireAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			OnAdded(aSender, aEventArgs);
		}

		[Transient]
		public event EventHandler<EventArgs<AObject, AObject>> OnRemoved = delegate {};

		protected void FireRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			OnRemoved(aSender, aEventArgs);
		}

		#endregion

		#region PROPERTIES

		ICollection<AObject> _children = new ArrayList4<AObject>();

		public AObject[] Children
		{
			get
			{
				Activate(ActivationPurpose.Read);
				return new HashSet<AObject>(_children).ToArray(); 
			}
		}

		#endregion

		#region FUNCTIONS

		protected bool Add(AObject aObject)
		{
			bool result = false;
			if (aObject != null)
			{
				lock (_children)
				{
					if (!DuplicateChildExists(aObject))
					{
						Activate(ActivationPurpose.Write);
						_children.Add(aObject);
						result = true;
					}
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
				lock (_children)
				{
					Activate(ActivationPurpose.Write);
					result = _children.Remove(aObject);
				}

				if (result)
				{
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

		public AObject WithGuid(Guid aGuid)
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
			foreach (AObject tObject in Children)
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

		public AObject Named(string aName)
		{
			try
			{
				return Children.FirstOrDefault(obj => obj.Name.Trim().ToLower() == aName.Trim().ToLower());
			}
			catch (Exception)
			{
				return null;
			}
		}

		protected abstract bool DuplicateChildExists(AObject aObject);

		#endregion

		#region DB4O

		public new void ObjectOnActivate(IObjectContainer container)
		{
			base.ObjectOnActivate(container);

			OnAdded = delegate {};
			OnRemoved = delegate {};

			foreach (var child in Children)
			{
				if (child == null)
				{
					_children.Remove(child);
					continue;
				}
				child.OnEnabledChanged += FireEnabledChanged;
				child.OnChanged += FireChanged;

				var children = child as AObjects;
				if (children != null)
				{
					children.OnAdded += FireAdded;
					children.OnRemoved += FireRemoved;
				}
			}
		}

		#endregion
	}
}
