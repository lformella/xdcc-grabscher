// 
//  Plugin.cs
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

using XG.Core;

using Nest;

namespace XG.Server.Plugin.General.ElasticSearch
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		ElasticClient _client;
		string _index = "xg";

		#endregion
		
		#region AWorker

		protected override void StartRun()
		{
			var setting = new ConnectionSettings(Settings.Instance.ElasticSearchHost, Settings.Instance.ElasticSearchPort);
			_client = new ElasticClient(setting);

			// reindex all
			foreach (Core.Server s in Servers.All)
			{
				Index(s);

				foreach (Channel c in s.Channels)
				{
					Index(c);

					foreach (Bot b in c.Bots)
					{
						Index(b);

						foreach (Packet p in b.Packets)
						{
							Index(p);
						}
					}
				}
			}
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded(AObject aParentObj, AObject aObj)
		{
			Index(aObj);

			// reindex all parents if a child is added
			if (aObj is Channel)
			{
				Index ((aObj as Channel).Parent);
			}
			if (aObj is Bot)
			{
				Index ((aObj as Bot).Parent);
				Index ((aObj as Bot).Parent.Parent);
			}
			if (aObj is Packet)
			{
				Index ((aObj as Packet).Parent);
				Index ((aObj as Packet).Parent.Parent);
				Index ((aObj as Packet).Parent.Parent.Parent);
			}
		}

		protected override void ObjectChanged(AObject aObj)
		{
			Index(aObj);

			// reindex all packets if a bot is changed
			if (aObj is Bot)
			{
				foreach (var p in (aObj as Bot).Packets)
				{
					Index(p);
				}
			}
		}

		protected override void ObjectRemoved(AObject aParentObj, AObject aObj)
		{
			Remove(aObj);

			// reindex parent object
			Index(aParentObj);

			// drop all children
			if (aObj is Core.Server)
			{
				foreach (var channel in (aObj as Core.Server).Channels)
				{
					Remove(channel);
					foreach (var bot in channel.Bots)
					{
						Remove(bot);
						foreach (var packet in bot.Packets)
						{
							Remove(packet);
						}
					}
				}
			}
			if (aObj is Channel)
			{
				foreach (var bot in (aObj as Channel).Bots)
				{
					Remove(bot);
					foreach (var packet in bot.Packets)
					{
						Remove(packet);
					}
				}
			}
			if (aObj is Bot)
			{
				foreach (var packet in (aObj as Bot).Packets)
				{
					Remove(packet);
				}
			}
		}

		protected override void SnapshotAdded (Snapshot aSnap)
		{
			var snap = new Object.Snapshot();

			var properties = snap.GetType().GetProperties();
			foreach (var prop in properties)
			{
				var snapVal = (SnapshotValue) Enum.Parse(typeof (SnapshotValue), prop.Name);
				prop.SetValue(snap, aSnap.Get(snapVal), null);
			}

			_client.Index(snap, _index, "snapshot", (int) snap.Timestamp);
		}

		#endregion

		#region FUNCTIONS

		void Index (AObject aObj)
		{
			Object.AObject myObj = null;

			if (aObj is Core.Server)
			{
				myObj = new Object.Server { Object = aObj as Core.Server };
			}
			else if (aObj is Channel)
			{
				myObj = new Object.Channel { Object = aObj as Channel };
			}
			else if (aObj is Bot)
			{
				myObj = new Object.Bot { Object = aObj as Bot };
			}
			else if (aObj is Packet)
			{
				myObj = new Object.Packet { Object = aObj as Packet };
			}

			if (myObj != null)
			{
				string type = myObj.GetType().Name.ToLower();
				_client.IndexAsync(myObj, _index, type, myObj.Guid.ToString());
			}
		}

		void Remove (AObject aObj)
		{
			if (aObj is Core.Server || aObj is Channel || aObj is Bot || aObj is Packet)
			{
				string type = aObj.GetType().Name.ToLower();
				_client.DeleteByIdAsync(_index, type, aObj.Guid.ToString());
			}
		}

		#endregion
	}
}
