// 
//  AObject.cs
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
using Db4objects.Db4o;
using Db4objects.Db4o.Activation;
using Db4objects.Db4o.Ext;
using Db4objects.Db4o.TA;
using XG.Extensions;

namespace XG.Model.Domain
{
	public abstract class AObject : IActivatable, IObjectCallbacks
	{
		#region EVENTS

		[Transient]
		public event EventHandler<EventArgs<AObject>> OnEnabledChanged = delegate {};

		protected void FireEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			OnEnabledChanged(aSender, aEventArgs);
		}

		[Transient]
		public event EventHandler<EventArgs<AObject, string[]>> OnChanged = delegate {};

		protected void FireChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			OnChanged(aSender, aEventArgs);
		}

		#endregion

		#region PROPERTIES

		protected T GetProperty<T>(ref T field)
		{
			Activate(ActivationPurpose.Read);
			return field;
		}

		protected bool SetProperty<T>(ref T field, T value, string aName)
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				Activate(ActivationPurpose.Write);
				if (_modifiedFields == null)
				{
					_modifiedFields = new List<string>();
				}
				_modifiedFields.Add(aName);
				field = value;
				return true;
			}
			return false;
		}

		Guid _parentGuid;

		public Guid ParentGuid
		{
			get { return GetProperty(ref _parentGuid); }
		}

		AObject _parent;

		public AObject Parent
		{
			get { return GetProperty(ref _parent); }
			set
			{
				if (_parent != value)
				{
					Activate(ActivationPurpose.Write);
					_parent = value;
					_parentGuid = _parent != null ? _parent.Guid : Guid.Empty;
				}
			}
		}

		Guid _guid;

		public Guid Guid
		{
			get
			{
				Activate(ActivationPurpose.Read);
				return _guid;
			}
			set
			{
				Activate(ActivationPurpose.Write);
				_guid = value;
			}
		}

		string _name;

		public virtual string Name
		{
			get { return GetProperty(ref _name); }
			set { SetProperty(ref _name, value, "Name"); }
		}

		[Transient]
		List<string> _modifiedFields;

		public bool Commit()
		{
			try
			{
				if (_modifiedFields != null && _modifiedFields.Count > 0)
				{
					FireChanged(this, new EventArgs<AObject, string[]>(this, _modifiedFields.ToArray()));
					_modifiedFields = new List<string>();
					return true;
				}
			}
			catch(Exception)
			{
				return false;
			}
			return false;
		}

		[Transient]
		bool _connected;

		public virtual bool Connected
		{
			get { return GetProperty(ref _connected); }
			set
			{
				SetProperty(ref _connected, value, "Connected");

				if (_connected)
				{
					Activate(ActivationPurpose.Write);
					_connectedTime = DateTime.Now;
				}
			}
		}

		DateTime _connectedTime = DateTime.MinValue.ToUniversalTime();

		public DateTime ConnectedTime
		{
			get { return GetProperty(ref _connectedTime); }
		}

		bool _enabled;

		public bool Enabled
		{
			get { return GetProperty(ref _enabled); }
			set
			{
				if (_enabled != value)
				{
					Activate(ActivationPurpose.Write);
					_enabled = value;

					if (_enabled)
					{
						_enabledTime = DateTime.Now;
					}
					FireEnabledChanged(this, new EventArgs<AObject>(this));
				}
			}
		}

		DateTime _enabledTime = DateTime.MinValue.ToUniversalTime();

		public DateTime EnabledTime
		{
			get { return GetProperty(ref _enabledTime); }
		}

		#endregion

		#region CONSTRUCTOR

		protected AObject()
		{
			Guid = Guid.NewGuid();
			Name = "";
			Connected = false;
			Enabled = false;
			_modifiedFields = new List<string>();
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return GetType().Name + "|" + Name;
		}

		public override int GetHashCode()
		{
			return Guid.GetHashCode();
		}

		#endregion

		#region DB4O

		[Transient]
		IActivator _activator;

		public void Activate(ActivationPurpose purpose)
		{
			if (_activator != null)
			{
				_activator.Activate(purpose);
			}
		}

		public void Bind(IActivator activator)
		{
			if (_activator == activator)
			{
				return;
			}
			if (activator != null && null != _activator)
			{
				throw new InvalidOperationException();
			}
			_activator = activator;
		}

		public bool ObjectCanActivate(IObjectContainer container)
		{
			return true;
		}

		public bool ObjectCanDeactivate(IObjectContainer container)
		{
			return true;
		}

		public bool ObjectCanDelete(IObjectContainer container)
		{
			return true;
		}

		public bool ObjectCanNew(IObjectContainer container)
		{
			return true;
		}

		public bool ObjectCanUpdate(IObjectContainer container)
		{
			return true;
		}

		public void ObjectOnActivate(IObjectContainer container)
		{
			OnChanged = delegate {};
			OnEnabledChanged = delegate {};
		}

		public void ObjectOnDeactivate(IObjectContainer container) {}

		public void ObjectOnDelete(IObjectContainer container) {}

		public void ObjectOnNew(IObjectContainer container) {}

		public void ObjectOnUpdate(IObjectContainer container) {}

		#endregion
	}
}
