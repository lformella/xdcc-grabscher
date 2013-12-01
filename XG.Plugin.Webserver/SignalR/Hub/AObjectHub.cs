// 
//  AObjectHub.cs
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
using System.Threading.Tasks;
using System.Collections.Generic;
using XG.Model.Domain;
using XG.Plugin.Webserver.SignalR.Hub.Model;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	[HubAuthorize]
	public abstract class AObjectHub : Microsoft.AspNet.SignalR.Hub
	{
		public override Task OnConnected()
		{
			AddClient(new Client { ConnectionId = Context.ConnectionId, LoadedObjects = new HashSet<Guid>(), MaxObjects = 0 });
			return base.OnConnected();
		}

		protected abstract void AddClient(Client aClient);

		public override Task OnDisconnected()
		{
			RemoveClient(Context.ConnectionId);
			return base.OnDisconnected();
		}

		protected abstract void RemoveClient(string connectionId);

		protected abstract Client GetClient(string connectionId);

		protected void UpdateLoadedClientObjects(string connectionId, HashSet<Guid> aGuids, int aMaxObjects = 0)
		{
			var client = GetClient(connectionId);
			if (client != null)
			{
				client.LoadedObjects = aGuids;
				client.MaxObjects = aMaxObjects;
			}
		}

		public void Enable(Guid aGuid)
		{
			AObject tObj = Helper.Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = true;
			}
		}

		public void Disable(Guid aGuid)
		{
			AObject tObj = Helper.Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = false;
			}
		}

		protected IEnumerable<T> FilterAndLoadObjects<T>(IEnumerable<AObject> aObjects, int aCount, int aPage, string aSortBy, string aSort, out int aLength)
		{
			aPage--;
			var objects = Helper.XgObjectsToHubObjects(aObjects).Cast<T>();

			if (string.IsNullOrWhiteSpace(aSortBy))
			{
				aSortBy = "Name";
			}
			var prop = typeof(T).GetProperty(aSortBy);
			if (aSort == "desc")
			{
				objects = objects.OrderByDescending(o => prop.GetValue(o, null));
			}
			else
			{
				objects = objects.OrderBy(o => prop.GetValue(o, null));
			}

			aLength = objects.Count();
			return objects.Skip(aPage * aCount).Take(aCount).ToArray();
		}
	}
}
