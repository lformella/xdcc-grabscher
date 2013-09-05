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

namespace XG.Core
{
	public delegate void ObjectsDelegate(AObjects aObjects, AObject aObject);

	[Serializable]
	public abstract class AObjects : AObject
	{
		#region EVENTS

		[field: NonSerialized]
		public event ObjectsDelegate OnAdded;

		protected void FireAdded(AObjects aObjects, AObject aObject)
		{
			if (OnAdded != null)
			{
				OnAdded(aObjects, aObject);
			}
		}

		[field: NonSerialized]
		public event ObjectsDelegate OnRemoved;

		protected void FireRemoved(AObjects aObjects, AObject aObject)
		{
			if (OnRemoved != null)
			{
				OnRemoved(aObjects, aObject);
			}
		}

		#endregion

		#region PROPERTIES

		readonly ICollection<AObject> _children;

		protected AObject[] All
		{
			get { return _children.ToArray(); }
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
					if (!_children.Contains(aObject))
					{
						if (WithGuid(aObject.Guid) == null && !DuplicateChildExists(aObject))
						{
							_children.Add(aObject);
							result = true;
						}
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
					FireAdded(this, aObject);
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
					if (_children.Contains(aObject))
					{
						_children.Remove(aObject);
						result = true;
					}
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
					FireRemoved(this, aObject);
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

		#endregion

		#region CONSTRUCTOR

		public AObjects(AObjects aObject = null, bool useHashset = true) : base(aObject)
		{
			if (useHashset)
			{
				_children = new HashSet<AObject>();
			}
			else
			{
				_children = new List<AObject>();
			}
		}

		#endregion
	}
}
