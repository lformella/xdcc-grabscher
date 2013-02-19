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

namespace XG.Core
{
	public delegate void ObjectDelegate(AObject aObj);
	public delegate void ObjectFieldsDelegate(AObject aObj, string[] aFields);

	[Serializable]
	public class AObject
	{
		#region EVENTS

		[field: NonSerialized]
		public event ObjectDelegate EnabledChanged;

		protected void FireEnabledChanged(AObject aObj)
		{
			if (EnabledChanged != null)
			{
				EnabledChanged(aObj);
			}
		}

		[field: NonSerialized]
		public event ObjectFieldsDelegate Changed;

		protected void FireChanged(AObject aObj, string[] aFields)
		{
			if (Changed != null)
			{
				Changed(aObj, aFields);
			}
		}

		#endregion

		#region PROPERTIES

		protected bool SetProperty<T>(ref T field, T value, string aName)
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
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
			get { return _parentGuid; }
		}

		AObject _parent;

		public virtual AObject Parent
		{
			get { return _parent; }
			set
			{
				if (_parent != value)
				{
					_parent = value;
					_parentGuid = _parent != null ? _parent.Guid : Guid.Empty;
				}
			}
		}

		public Guid Guid { get; set; }

		string _name;

		public virtual string Name
		{
			get { return _name; }
			set { SetProperty(ref _name, value, "Name"); }
		}

		List<string> _modifiedFields;

		public bool Commit()
		{
			if (_modifiedFields != null && _modifiedFields.Count > 0)
			{
				FireChanged(this, _modifiedFields.ToArray());
				_modifiedFields = new List<string>();
				return true;
			}
			return false;
		}

		bool _connected;

		public virtual bool Connected
		{
			get { return _connected; }
			set { SetProperty(ref _connected, value, "Connected"); }
		}

		bool _enabled;

		public virtual bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;

					if (_enabled)
					{
						_enabledTime = DateTime.Now;
					}
					FireEnabledChanged(this);
				}
			}
		}

		DateTime _enabledTime = DateTime.MinValue.ToUniversalTime();

		public DateTime EnabledTime
		{
			get { return _enabledTime; }
		}

		#endregion

		#region CONSTRUCTOR

		public AObject()
		{
			Guid = Guid.NewGuid();
			_name = "";
			_connected = false;
			_enabled = false;
			_modifiedFields = new List<string>();
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return GetType().Name + "|" + Name;
		}

		#endregion
	}
}
