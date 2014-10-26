// 
//  SnapshotHub.cs
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
using SharpRobin.Core;
using XG.Business.Model;
using XG.Extensions;
using XG.Plugin.Webserver.SignalR.Hub.Model;
using XG.Plugin.Webserver.SignalR.Hub.Model.Domain;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	public class SnapshotHub : AObjectHub
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
			GetClient(Context.ConnectionId).VisibleHubs.Add(typeof(SnapshotHub));
		}

		public void InVisible()
		{
			GetClient(Context.ConnectionId).VisibleHubs.Remove(typeof(SnapshotHub));
		}

		#endregion

		public IEnumerable<Flot> GetFlotSnapshot()
		{
			var tObjects = new List<Flot>();

			var snapshot = Business.Helper.Snapshots.GenerateSnapshot();
			for (int a = 1; a <= Snapshot.SnapshotCount; a++)
			{
				var value = (SnapshotValue)a;
				var obj = new Flot
				{
					Data = new[] {new[] {snapshot.Get(SnapshotValue.Timestamp), snapshot.Get(value)}},
					Type = a
				};

				tObjects.Add(obj);
			}

			return tObjects.ToArray();
		}

		public IEnumerable<Flot> GetSnapshots(int aDays)
		{
			var startTime = DateTime.Now.AddDays(aDays * -1);
			return GetFlotData(startTime, DateTime.Now);
		}

		IEnumerable<Flot> GetFlotData(DateTime aStart, DateTime aEnd)
		{
			var tObjects = new List<Flot>();

			FetchData data = Helper.RrdDb.createFetchRequest(ConsolFuns.CF_AVERAGE, aStart.ToTimestamp(), aEnd.ToTimestamp(), 1).fetchData();
			Int64[] times = data.getTimestamps();
			double[][] values = data.getValues();

			for (int a = 1; a <= Snapshot.SnapshotCount; a++)
			{
				var obj = new Flot();

				var list = new List<double[]>();
				for (int b = 0; b < times.Length; b++)
				{
					double[] current = { times[b] * 1000, values[a][b] };
					list.Add(current);
				}
				obj.Data = list.ToArray();
				obj.Type = a;

				tObjects.Add(obj);
			}

			return tObjects.ToArray();
		}
	}
}
