// 
//  SearchHub.cs
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
using XG.Model.Domain;
using XG.Plugin.Webserver.SignalR.Hub;
using XG.Plugin.Webserver.SignalR.Hub.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	public class SearchHub : AObjectHub
	{
		#region Client Handling

		public static readonly HashSet<Client> ConnectedClients = new HashSet<Client>();

		protected override void AddClient(Client aClient)
		{
			aClient.MaxObjects = 0;
			ConnectedClients.Add(aClient);
		}

		protected override void RemoveClient(string connectionId)
		{
			var client = GetClient(connectionId);
			if (client != null)
			{
				ConnectedClients.Remove(client);
			}
		}

		protected override Client GetClient(string connectionId)
		{
			return (from client in ConnectedClients where client.ConnectionId == connectionId select client).SingleOrDefault();
		}

		public void Visible()
		{
			GetClient(Context.ConnectionId).VisibleHubs.Add(typeof(SearchHub));
		}

		public void InVisible()
		{
			GetClient(Context.ConnectionId).VisibleHubs.Remove(typeof(SearchHub));
		}

		#endregion

		public void Add(string aSearch, Int64 aSize)
		{
			var obj = Helper.Searches.WithParameters(aSearch, aSize);
			if (obj == null)
			{
				obj = new XG.Model.Domain.Search { Name = aSearch, Size = aSize };
				Helper.Searches.Add(obj);
			}
		}

		public void Remove(Guid aGuid)
		{
			var search = Helper.Searches.WithGuid(aGuid);
			if (search != null)
			{
				Helper.Searches.Remove(search);
			}
		}

		public void GetAll()
		{
			UpdateLoadedClientObjects(Context.ConnectionId, new HashSet<Guid>(Helper.Searches.All.Select(o => o.Guid)));
			foreach (var search in Helper.Searches.All)
			{
				Clients.Caller.OnAdded(Helper.XgObjectToHubObject(search));
			}
		}
	}
}
