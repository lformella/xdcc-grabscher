// 
//  EventForwarder.cs
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
using System.Linq;
using Microsoft.AspNet.SignalR;
using XG.Model.Domain;
using XG.Model;
using System.Collections.Generic;
using XG.Plugin.Webserver.SignalR.Hub.Model;
using XG.Plugin.Webserver.SignalR.Hub;

namespace XG.Plugin.Webserver.SignalR
{
	public class EventForwarder : APlugin
	{
		#region REPOSITORY EVENTS

		protected override void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			bool reloadTable = false;
			if (aEventArgs.Value2 is Server || aEventArgs.Value2 is Channel)
			{
				reloadTable = true;
			}
			SendAdded(aEventArgs.Value2, reloadTable);
		}

		protected override void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			SendRemoved(aEventArgs.Value2);
		}

		protected override void ObjectChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			// if a bot changed dispatch just the packets (because we have no separate bot hub)
			if (aEventArgs.Value1 is Bot)
			{
				foreach (var pack in (aEventArgs.Value1 as Bot).Packets)
				{
					SendChanged(pack);
				}
			}
			else
			{
				SendChanged(aEventArgs.Value1);
			}
		}

		protected override void ObjectEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			SendChanged(aEventArgs.Value1);
		}

		protected override void FileAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			SendAdded(aEventArgs.Value2);
		}

		protected override void FileRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			SendRemoved(aEventArgs.Value2);
		}

		protected override void FileChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			// if a part changed dispatch just connected stuff (because we have no separate bot hub)
			if (aEventArgs.Value1 is FilePart)
			{
				var part = aEventArgs.Value1 as FilePart;
				SendChanged(part.Parent);

				if (part.Packet != null)
				{
					if (aEventArgs.Value2.Contains("Speed") || aEventArgs.Value2.Contains("CurrentSize") || aEventArgs.Value2.Contains("TimeMissing"))
					{
						SendChanged(part.Packet);
					}
				}
			}
			else
			{
				SendChanged(aEventArgs.Value1);
			}
		}

		protected override void SearchAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			SendAdded(aEventArgs.Value2);
		}

		protected override void SearchRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			SendRemoved(aEventArgs.Value2);
		}

		protected override void SearchChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			SendChanged(aEventArgs.Value1);
		}

		protected override void ApiKeyAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			SendAdded(aEventArgs.Value2);
		}

		protected override void ApiKeyRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			SendRemoved(aEventArgs.Value2);
		}

		protected override void ApiKeyChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			SendChanged(aEventArgs.Value1);
		}

		protected override void ApiKeyEnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			SendChanged(aEventArgs.Value1);
		}

		protected override void NotificationAdded(object aSender, EventArgs<Notification> aEventArgs)
		{
			SendAdded(aEventArgs.Value1);
		}

		#endregion

		#region HUB Sending

		void SendAdded(AObject aObject, bool reloadTable = false)
		{
			var hub = GetHubForObject(aObject);
			if (hub == null)
			{
				return;
			}

			var hubObject = Hub.Helper.XgObjectToHubObject(aObject);
			if (hubObject == null)
			{
				return;
			}

			var clients = GetClientsForObject(aObject);
			foreach (var client in clients)
			{
				lock (client.LoadedObjects)
				{
					if (client.LoadedObjects.Count < client.MaxObjects || client.MaxObjects == 0)
					{
						GlobalHost.ConnectionManager.GetHubContext(hub).Clients.Client(client.ConnectionId).OnAdded(hubObject);
						client.LoadedObjects.Add(hubObject.Guid);
					}
					else if (reloadTable)
					{
						GlobalHost.ConnectionManager.GetHubContext(hub).Clients.Client(client.ConnectionId).OnReloadTable();
					}
				}
			}
		}

		void SendRemoved(AObject aObject)
		{
			var hub = GetHubForObject(aObject);
			if (hub == null)
			{
				return;
			}

			var hubObject = Hub.Helper.XgObjectToHubObject(aObject);
			if (hubObject == null)
			{
				return;
			}

			var clients = GetClientsForObject(aObject);
			foreach (var client in clients)
			{
				lock (client.LoadedObjects)
				{
					if (client.LoadedObjects.Contains(hubObject.Guid))
					{
						GlobalHost.ConnectionManager.GetHubContext(hub).Clients.Client(client.ConnectionId).OnRemoved(hubObject);
						client.LoadedObjects.Remove(hubObject.Guid);
					}
				}
			}
		}

		void SendChanged(AObject aObject)
		{
			var hub = GetHubForObject(aObject);
			if (hub == null)
			{
				return;
			}

			var hubObject = Hub.Helper.XgObjectToHubObject(aObject);
			if (hubObject == null)
			{
				return;
			}

			var clients = GetClientsForObject(aObject);
			foreach (var client in clients)
			{
				lock (client.LoadedObjects)
				{
					if (client.LoadedObjects.Contains(hubObject.Guid))
					{
						GlobalHost.ConnectionManager.GetHubContext(hub).Clients.Client(client.ConnectionId).OnChanged(hubObject);
					}
				}
			}
		}

		HashSet<Client> GetClientsForObject(AObject aObject)
		{
			if (aObject is Server)
			{
				return ServerHub.ConnectedClients;
			}
			else if (aObject is Channel)
			{
				return ChannelHub.ConnectedClients;
			}
			else if (aObject is Packet)
			{
				return PacketHub.ConnectedClients;
			}
			else if (aObject is File)
			{
				return FileHub.ConnectedClients;
			}
			else if (aObject is Search)
			{
				return SearchHub.ConnectedClients;
			}
			else if (aObject is Notification)
			{
				return NotificationHub.ConnectedClients;
			}
			else if (aObject is ApiKey)
			{
				return ApiHub.ConnectedClients;
			}

			return new HashSet<Client>();
		}

		string GetHubForObject(AObject aObject)
		{
			if (aObject is Server)
			{
				return typeof(ServerHub).Name;
			}
			else if (aObject is Channel)
			{
				return typeof(ChannelHub).Name;
			}
			else if (aObject is Packet)
			{
				return typeof(PacketHub).Name;
			}
			else if (aObject is File)
			{
				return typeof(FileHub).Name;
			}
			else if (aObject is Search)
			{
				return typeof(SearchHub).Name;
			}
			else if (aObject is Notification)
			{
				return typeof(NotificationHub).Name;
			}
			else if (aObject is ApiKey)
			{
				return typeof(ApiHub).Name;
			}

			return null;
		}

		#endregion
	}
}
