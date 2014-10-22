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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.SignalR;
using XG.Extensions;
using XG.Model.Domain;
using log4net;
using XG.Plugin.Webserver.SignalR.Hub;
using XG.Plugin.Webserver.SignalR.Hub.Model;

namespace XG.Plugin.Webserver.SignalR
{
	public class EventForwarder : APlugin
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
			if (aEventArgs.Value1 is Bot)
			{
				// if a bot changed maybee dispatch all packets or just the first because we have no separate bot hub
				if (aEventArgs.Value2.Contains("State") || aEventArgs.Value2.Contains("QueuePosition") || aEventArgs.Value2.Contains("QueueTime"))
				{
					foreach (var pack in (aEventArgs.Value1 as Bot).Packets)
					{
						SendChanged(pack);
					}
				}
				else
				{
					var pack = (aEventArgs.Value1 as Bot).Packets.FirstOrDefault();
					if (pack != null)
					{
						SendChanged(pack);
					}
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
			// if a file changed dispatch connected stuff too
			if (aEventArgs.Value1 is File)
			{
				var file = aEventArgs.Value1 as File;

				if (file.Packet != null)
				{
					if (aEventArgs.Value2.Contains("Speed") || aEventArgs.Value2.Contains("CurrentSize") || aEventArgs.Value2.Contains("TimeMissing"))
					{
						SendChanged(file.Packet);
					}
				}
			}
			SendChanged(aEventArgs.Value1);
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
					if ((client.LoadedObjects.Count < client.MaxObjects || client.MaxObjects == 0) && hub != typeof(PacketHub))
					{
						Log.Debug("SendAdded() " + aObject);
						GlobalHost.ConnectionManager.GetHubContext(hub.Name).Clients.Client(client.ConnectionId).OnAdded(hubObject);
						client.LoadedObjects.Add(hubObject.Guid);
					}
					else if (reloadTable)
					{
						Log.Debug("SendChanged() RELOAD");
						GlobalHost.ConnectionManager.GetHubContext(hub.Name).Clients.Client(client.ConnectionId).OnReloadTable();
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
					if (!client.LoadedObjects.Contains(hubObject.Guid))
					{
						continue;
					}

					Log.Debug("SendRemoved()" + aObject);
					GlobalHost.ConnectionManager.GetHubContext(hub.Name).Clients.Client(client.ConnectionId).OnRemoved(hubObject);
					client.LoadedObjects.Remove(hubObject.Guid);
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
					if (!client.LoadedObjects.Contains(hubObject.Guid))
					{
						continue;
					}
					if (!client.VisibleHubs.Contains(hub))
					{
						continue;
					}

					Log.Debug("SendChanged()" + aObject);
					GlobalHost.ConnectionManager.GetHubContext(hub.Name).Clients.Client(client.ConnectionId).OnChanged(hubObject);
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
			else if (aObject is XG.Model.Domain.Search)
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

		Type GetHubForObject(AObject aObject)
		{
			if (aObject is Server)
			{
				return typeof(ServerHub);
			}
			else if (aObject is Channel)
			{
				return typeof(ChannelHub);
			}
			else if (aObject is Packet)
			{
				return typeof(PacketHub);
			}
			else if (aObject is File)
			{
				return typeof(FileHub);
			}
			else if (aObject is XG.Model.Domain.Search)
			{
				return typeof(SearchHub);
			}
			else if (aObject is Notification)
			{
				return typeof(NotificationHub);
			}
			else if (aObject is ApiKey)
			{
				return typeof(ApiHub);
			}

			return null;
		}

		#endregion
	}
}
