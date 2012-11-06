// 
//  AWorker.cs
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
using System.Threading;
using System.Reflection;

using log4net;

using XG.Core;

namespace XG.Server.Worker
{
	public abstract class AWorker
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		Thread _thread;
		
		#endregion

		#region REPOSITORIES
		
		Core.Servers _servers;
		public Core.Servers Servers
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
					_servers.EnabledChanged -= new ObjectDelegate(ObjectEnabledChanged);
				}
				_servers = value;
				if(_servers != null)
				{
					_servers.Added += new ObjectsDelegate(ObjectAdded);
					_servers.Removed += new ObjectsDelegate(ObjectRemoved);
					_servers.Changed += new ObjectDelegate(ObjectChanged);
					_servers.EnabledChanged += new ObjectDelegate(ObjectEnabledChanged);
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
		
		Snapshots _snapshots;
		public Snapshots Snapshots
		{
			get
			{
				return _snapshots;
			}
			set
			{
				if(_snapshots != null)
				{
					_snapshots.Added -= new SnapshotDelegate(SnapshotAdded);
				}
				_snapshots = value;
				if(_snapshots != null)
				{
					_snapshots.Added += new SnapshotDelegate(SnapshotAdded);
				}
			}
		}
		
		#endregion
		
		#region REPOSITORY EVENTS
		
		protected virtual void ObjectAdded(AObject aParent, AObject aObj)
		{
		}
		
		protected virtual void ObjectRemoved(AObject aParent, AObject aObj)
		{
		}
		
		protected virtual void ObjectChanged(AObject aObj)
		{
		}
		
		protected virtual void ObjectEnabledChanged(AObject aObj)
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
		
		protected virtual void SnapshotAdded(Snapshot aSnap)
		{
		}
		
		#endregion

		#region FUNCTIONS

		public void Start()
		{
			try
			{
				_thread = new Thread(new ThreadStart(StartRun));
				_thread.Start();
			}
			catch (ThreadAbortException)
			{
				// this is ok
			}
			catch (Exception ex)
			{
				_log.Fatal("Start()", ex);
			}
		}

		protected virtual void StartRun()
		{
		}

		public void Stop()
		{
			try
			{
				StopRun();
				_thread.Abort();
			}
			catch (Exception ex)
			{
				_log.Fatal("Stop()", ex);
			}
		}
		
		protected virtual void StopRun()
		{
		}

		#endregion
	}
}
