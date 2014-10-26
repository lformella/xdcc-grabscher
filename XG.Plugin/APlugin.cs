// 
//  ADataWorker.cs
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

using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin
{
	public abstract class APlugin : AWorker
	{
		#region REPOSITORIES

		Servers _servers;

		public Servers Servers
		{
			get { return _servers; }
			set
			{
				if (_servers != null)
				{
					_servers.OnAdded -= ObjectAdded;
					_servers.OnRemoved -= ObjectRemoved;
					_servers.OnChanged -= ObjectChanged;
					_servers.OnEnabledChanged -= ObjectEnabledChanged;
				}
				_servers = value;
				if (_servers != null)
				{
					_servers.OnAdded += ObjectAdded;
					_servers.OnRemoved += ObjectRemoved;
					_servers.OnChanged += ObjectChanged;
					_servers.OnEnabledChanged += ObjectEnabledChanged;
				}
			}
		}

		Files _files;

		public Files Files
		{
			get { return _files; }
			set
			{
				if (_files != null)
				{
					_files.OnAdded -= FileAdded;
					_files.OnRemoved -= FileRemoved;
					_files.OnChanged -= FileChanged;
				}
				_files = value;
				if (_files != null)
				{
					_files.OnAdded += FileAdded;
					_files.OnRemoved += FileRemoved;
					_files.OnChanged += FileChanged;
				}
			}
		}

		Searches _searches;

		public Searches Searches
		{
			get { return _searches; }
			set
			{
				if (_searches != null)
				{
					_searches.OnAdded -= SearchAdded;
					_searches.OnRemoved -= SearchRemoved;
					_searches.OnChanged -= SearchChanged;
				}
				_searches = value;
				if (_searches != null)
				{
					_searches.OnAdded += SearchAdded;
					_searches.OnRemoved += SearchRemoved;
					_searches.OnChanged += SearchChanged;
				}
			}
		}

		ApiKeys _apiKeys;

		public ApiKeys ApiKeys
		{
			get { return _apiKeys; }
			set
			{
				if (_apiKeys != null)
				{
					_apiKeys.OnAdded -= ApiKeyAdded;
					_apiKeys.OnRemoved -= ApiKeyRemoved;
					_apiKeys.OnChanged -= ApiKeyChanged;
					_apiKeys.OnEnabledChanged -= ApiKeyEnabledChanged;
				}
				_apiKeys = value;
				if (_apiKeys != null)
				{
					_apiKeys.OnAdded += ApiKeyAdded;
					_apiKeys.OnRemoved += ApiKeyRemoved;
					_apiKeys.OnChanged += ApiKeyChanged;
					_apiKeys.OnEnabledChanged += ApiKeyEnabledChanged;
				}
			}
		}

		Notifications _notifications;

		public Notifications Notifications
		{
			get { return _notifications; }
			set
			{
				if (_notifications != null)
				{
					_notifications.OnAdded -= (aSender, aEventArgs) => NotificationAdded(aSender, new EventArgs<Notification>((Notification)aEventArgs.Value2));
				}
				_notifications = value;
				if (_notifications != null)
				{
					_notifications.OnAdded += (aSender, aEventArgs) => NotificationAdded(aSender, new EventArgs<Notification>((Notification)aEventArgs.Value2));
				}
			}
		}

		#endregion

		#region REPOSITORY EVENTS

		protected virtual void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void ObjectChanged(object aSender, EventArgs<AObject, string[]> aEventArgs) {}

		protected virtual void ObjectEnabledChanged(object aSender, EventArgs<AObject> aEventArgs) {}

		protected virtual void FileAdded(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void FileRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void FileChanged(object aSender, EventArgs<AObject, string[]> aEventArgs) {}

		protected virtual void SearchAdded(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void SearchRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void ApiKeyChanged(object aSender, EventArgs<AObject, string[]> aEventArgs) {}

		protected virtual void ApiKeyAdded(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void ApiKeyRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs) {}

		protected virtual void ApiKeyEnabledChanged(object aSender, EventArgs<AObject> aEventArgs) {}

		protected virtual void SearchChanged(object aSender, EventArgs<AObject, string[]> aEventArgs) {}

		protected virtual void NotificationAdded(object aSender, EventArgs<Notification> aEventArgs) {}

		#endregion
	}
}
