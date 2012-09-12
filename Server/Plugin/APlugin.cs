// 
//  APlugin.cs
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

using XG.Core;

namespace XG.Server.Plugin
{
	public abstract class APlugin
	{
		#region VARIABLES

		XG.Core.Servers _servers;
		public XG.Core.Servers Servers
		{
			get
			{
				return _servers;
			}
			set
			{
				if(_servers != null)
				{
					_servers.Added -= new ObjectsDelegate(ObjectAdded);
					_servers.Removed -= new ObjectsDelegate(ObjectRemoved);
					_servers.Changed -= new ObjectDelegate(ObjectChanged);
				}
				_servers = value;
				if(_servers != null)
				{
					_servers.Added += new ObjectsDelegate(ObjectAdded);
					_servers.Removed += new ObjectsDelegate(ObjectRemoved);
					_servers.Changed += new ObjectDelegate(ObjectChanged);
				}
			}
		}

		Files _files;
		public Files Files
		{
			get
			{
				return _files;
			}
			set
			{
				if(_files != null)
				{
					_files.Added -= new ObjectsDelegate(FileAdded);
					_files.Removed -= new ObjectsDelegate(FileRemoved);
					_files.Changed -= new ObjectDelegate(FileChanged);
				}
				_files = value;
				if(_files != null)
				{
					_files.Added += new ObjectsDelegate(FileAdded);
					_files.Removed += new ObjectsDelegate(FileRemoved);
					_files.Changed += new ObjectDelegate(FileChanged);
				}
			}
		}

		Objects _searches;
		public Objects Searches
		{
			get
			{
				return _searches;
			}
			set
			{
				if(_searches != null)
				{
					_searches.Added -= new ObjectsDelegate(SearchAdded);
					_searches.Removed -= new ObjectsDelegate(SearchRemoved);
					_searches.Changed -= new ObjectDelegate(SearchChanged);
				}
				_searches = value;
				if(_searches != null)
				{
					_searches.Added += new ObjectsDelegate(SearchAdded);
					_searches.Removed += new ObjectsDelegate(SearchRemoved);
					_searches.Changed += new ObjectDelegate(SearchChanged);
				}
			}
		}

		#endregion

		#region EVENTHANDLER

		protected virtual void ObjectAdded(AObject aParent, AObject aObj)
		{
		}

		protected virtual void ObjectRemoved(AObject aParent, AObject aObj)
		{
		}

		protected virtual void ObjectChanged(AObject aObj)
		{
		}

		protected virtual void FileAdded(AObject aParent, AObject aObj)
		{
		}

		protected virtual void FileRemoved(AObject aParent, AObject aObj)
		{
		}

		protected virtual void FileChanged(AObject aObj)
		{
		}

		protected virtual void SearchAdded(AObject aParent, AObject aObj)
		{
		}

		protected virtual void SearchRemoved(AObject aParent, AObject aObj)
		{
		}

		protected virtual void SearchChanged(AObject aObj)
		{
		}

		#endregion

		#region FUNCTIONS

		public virtual void Start()
		{
		}

		public virtual void Stop()
		{
		}

		#endregion
		
		#region SERVER

		public void AddServer(string aString)
		{
			Servers.Add(aString);
		}

		public void RemoveServer(Guid aGuid)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				Servers.Remove(tObj as XG.Core.Server);
			}
		}

		#endregion

		#region CHANNEL

		public void AddChannel(Guid aGuid, string aString)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				(tObj as XG.Core.Server).AddChannel(aString);
			}
		}

		public void RemoveChannel(Guid aGuid)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				Channel tChan = tObj as Channel;
				tChan.Parent.RemoveChannel(tChan);
			}
		}

		#endregion

		#region OBJECT

		public void ActivateObject(Guid aGuid)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = true;
				tObj.Commit();
			}
		}

		public void DeactivateObject(Guid aGuid)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = false;
				tObj.Commit();
			}
		}

		#endregion
	}
}
