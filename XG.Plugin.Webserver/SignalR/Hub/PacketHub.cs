// 
//  PacketHub.cs
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
using System.Collections.Generic;
using XG.Model;
using XG.Plugin.Webserver.SignalR.Hub.Model;
using System.Threading.Tasks;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	public class PacketHub : AObjectHub
	{
		#region Client Handling

		public static readonly HashSet<Client> ConnectedClients = new HashSet<Client>();

		protected override void AddClient(Client aClient)
		{
			aClient.MaxObjects = 20;
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

		public Model.Domain.Result LoadByGuid(Guid aGuid, bool aShowOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			Search search;
			if (aGuid == Search.SearchEnabled)
			{
				search = new Search { Guid = Search.SearchEnabled };
			}
			else if (aGuid == Search.SearchDownloads)
			{
				search = new Search { Guid = Search.SearchDownloads };
			}
			else
			{
				search = Helper.Searches.All.SingleOrDefault(s => s.Guid == aGuid);
			}
			if (search != null)
			{
				return LoadBySearch(search, aShowOfflineBots, aCount, aPage, aSortBy, aSort);
			}
			return new Model.Domain.Result { Total = 0, Results = new List<Packet>() };
		}

		public Model.Domain.Result LoadByName(string aSearch, bool aShowOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			return LoadBySearch(new Search { Name = aSearch }, aShowOfflineBots, aCount, aPage, aSortBy, aSort);
		}

		Model.Domain.Result LoadBySearch(Search aSearch, bool aShowOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			if (aSearch.Name.Contains("*"))
			{
				return LoadByWildcardSearch(aSearch, aShowOfflineBots, aCount, aPage, aSortBy, aSort);
			}
			else
			{
				return LoadByNormalSearch(aSearch, aShowOfflineBots, aCount, aPage, aSortBy, aSort);
			}
		}

		public Model.Domain.Result LoadByParentGuid(Guid aParentGuid, bool aShowOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			var packets = (from server in Helper.Servers.All from channel in server.Channels from bot in channel.Bots where bot.Guid == aParentGuid from packet in bot.Packets select packet);
			int length;
			var objects = Helper.FilterAndLoadObjects<Model.Domain.Packet>(packets, aCount, aPage, aSortBy, aSort, out length);
			UpdateLoadedClientObjects(Context.ConnectionId, new HashSet<Guid>(objects.Select(o => o.Guid)), aCount);
			return new Model.Domain.Result { Total = length, Results = objects };
		}

		Model.Domain.Result LoadByNormalSearch(Search aSearch, bool aShowOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			var packets = (from server in Helper.Servers.All from channel in server.Channels from bot in channel.Bots where (aShowOfflineBots || bot.Connected) from packet in bot.Packets where aSearch.IsVisible(packet) select packet);
			int length;
			var objects = Helper.FilterAndLoadObjects<Model.Domain.Packet>(packets, aCount, aPage, aSortBy, aSort, out length);
			UpdateLoadedClientObjects(Context.ConnectionId, new HashSet<Guid>(objects.Select(o => o.Guid)), aCount);
			objects.ToList().ForEach(p => p.GroupBy = aSearch.Name);
			return new Model.Domain.Result { Total = length, Results = objects };
		}

		Model.Domain.Result LoadByWildcardSearch(Search aSearch, bool aShowOfflineBots, int aCount, int aPage, string aSortBy, string aSort)
		{
			int start = 1;
			int end = aSearch.Name.Contains("**") ? 99 : 9;
			int length = 0;
			HashSet<object> objects = new HashSet<object>();
			for (int a = start; a <= end; a++)
			{
				var result = LoadByNormalSearch(new Search { Name = aSearch.Name.Replace("**", "" + a.ToString("D2")).Replace("*", "" + a) }, aShowOfflineBots, 0, 1, aSortBy, aSort);
				if (result.Total > 0)
				{
					length += result.Total;
					objects.UnionWith(result.Results);
				}
			}
			var currentObjects = objects.Skip((aPage - 1) * aCount).Take(aCount).ToArray();
			return new Model.Domain.Result { Total = length, Results = currentObjects };
		}
	}
}
