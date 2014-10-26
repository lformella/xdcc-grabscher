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
			AddClient(
				new Client
				{
					ConnectionId = Context.ConnectionId,
					LoadedObjects = new HashSet<Guid>(),
					MaxObjects = 0,
					VisibleHubs = new List<Type>()
				}
			);
			return base.OnConnected();
		}

		protected abstract void AddClient(Client aClient);

		public override Task OnDisconnected(bool stopCalled)
		{
			RemoveClient(Context.ConnectionId);
			return base.OnDisconnected(stopCalled);
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
	}
}
