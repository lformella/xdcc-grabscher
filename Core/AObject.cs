// 
//  AObject.cs
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

using Newtonsoft.Json;

using System;
using System.Collections.Generic;

namespace XG.Core
{
	public delegate void ObjectDelegate(AObject aObj);

	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
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
		public event ObjectDelegate Changed;

		protected void FireChanged(AObject aObj)
		{
			if (Changed != null)
			{
				Changed(aObj);
			}
		}

		#endregion

		#region PROPERTIES

		protected bool SetProperty<T>(ref T field, T value)
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				_modified = true;
				return true;
			}
			return false;
		}

		Guid _parentGuid;

		[JsonProperty]
		[MySql]
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

		[JsonProperty]
		[MySql]
		public Guid Guid { get; set; }

		string _name;

		[JsonProperty]
		[MySql]
		public virtual string Name
		{
			get { return _name; }
			set { SetProperty(ref _name, value); }
		}

		bool _modified;

		public bool Commit()
		{
			if (_modified)
			{
				FireChanged(this);
				_modified = false;
				return true;
			}
			return false;
		}

		bool _connected;

		[JsonProperty]
		[MySql]
		public virtual bool Connected
		{
			get { return _connected; }
			set { SetProperty(ref _connected, value); }
		}

		bool _enabled;

		[JsonProperty]
		[MySql]
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
		}

		#endregion
	}
}
