// 
//  ServerHub.cs
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
using XG.Plugin.Webserver.SignalR.Hub;
using XG.Plugin.Webserver.SignalR.Hub.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using XG.Model.Domain;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	public class ServerHub : AObjectHub
	{
		#region Client Handling

		public static readonly HashSet<Client> ConnectedClients = new HashSet<Client>();

		protected override void AddClient(Client aClient)
		{
			aClient.MaxObjects = 10;
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
			GetClient(Context.ConnectionId).VisibleHubs.Add(typeof(ServerHub));
		}

		public void InVisible()
		{
			GetClient(Context.ConnectionId).VisibleHubs.Remove(typeof(ServerHub));
		}

		#endregion

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

		public void Add(string aName)
		{
			string serverString = aName;
			int port = 6667;
			if (serverString.Contains(":"))
			{
				string[] serverArray = serverString.Split(':');
				serverString = serverArray[0];
				port = int.Parse(serverArray[1]);
			}

			Helper.Servers.Add(serverString, port);
		}

		public void Remove(Guid aGuid)
		{
			AObject tObj = Helper.Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				Helper.Servers.Remove(tObj as Server);
			}
		}

		public Model.Domain.Result Load(int aCount, int aPage, string aSortBy, string aSort)
		{
			int length;
			var objects = Helper.FilterAndLoadObjects<Model.Domain.Server>(Helper.Servers.All, aCount, aPage, aSortBy, aSort, out length);
			UpdateLoadedClientObjects(Context.ConnectionId, new HashSet<Guid>(objects.Select(o => o.Guid)), aCount);
			return new Model.Domain.Result { Total = length, Results = objects };
		}
	}
}
