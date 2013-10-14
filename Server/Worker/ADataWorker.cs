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

using System;
using System.Linq;

using XG.Core;

namespace XG.Server.Worker
{
	public abstract class ADataWorker : AWorker
	{
		#region REPOSITORIES

		Core.Servers _servers;

		public Core.Servers Servers
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

		protected virtual void SearchChanged(object aSender, EventArgs<AObject, string[]> aEventArgs) {}

		protected virtual void NotificationAdded(object aSender, EventArgs<Notification> aEventArgs) {}

		#endregion

		#region FUNCTIONS

		public Snapshot CollectSnapshot()
		{
			Core.Server[] servers = (from server in Servers.All select server).ToArray();
			Channel[] channels = (from server in servers from channel in server.Channels select channel).ToArray();
			Bot[] bots = (from channel in channels from bot in channel.Bots select bot).ToArray();
			Packet[] packets = (from bot in bots from packet in bot.Packets select packet).ToArray();
			Core.File[] files = (from file in Files.All select file).ToArray();

			var snapshot = new Snapshot();

			snapshot.Set(SnapshotValue.Timestamp, DateTime.Now.ToTimestamp());

			snapshot.Set(SnapshotValue.Speed, (from file in files from part in file.Parts select part.Speed).Sum());

			snapshot.Set(SnapshotValue.Servers, (from server in servers select server).Count());
			snapshot.Set(SnapshotValue.ServersEnabled, (from server in servers where server.Enabled select server).Count());
			snapshot.Set(SnapshotValue.ServersDisabled, (from server in servers where !server.Enabled select server).Count());
			snapshot.Set(SnapshotValue.ServersConnected, (from server in servers where server.Connected select server).Count());
			snapshot.Set(SnapshotValue.ServersDisconnected, (from server in servers where !server.Connected select server).Count());

			snapshot.Set(SnapshotValue.Channels, (from channel in channels select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsEnabled, (from channel in channels where channel.Parent.Enabled && channel.Enabled select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsDisabled, (from channel in channels where !channel.Parent.Enabled || !channel.Enabled select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsConnected, (from channel in channels where channel.Connected select channel).Count());
			snapshot.Set(SnapshotValue.ChannelsDisconnected, (from channel in channels where !channel.Connected select channel).Count());

			snapshot.Set(SnapshotValue.Bots, (from bot in bots select bot).Count());
			snapshot.Set(SnapshotValue.BotsConnected, (from bot in bots where bot.Connected select bot).Count());
			snapshot.Set(SnapshotValue.BotsDisconnected, (from bot in bots where !bot.Connected select bot).Count());
			snapshot.Set(SnapshotValue.BotsFreeSlots, (from bot in bots where bot.InfoSlotCurrent > 0 select bot).Count());
			snapshot.Set(SnapshotValue.BotsFreeQueue, (from bot in bots where bot.InfoQueueCurrent > 0 select bot).Count());
			try
			{
				snapshot.Set(SnapshotValue.BotsAverageCurrentSpeed, ((from bot in bots select bot.InfoSpeedCurrent).Sum() / (from bot in bots where bot.InfoSpeedCurrent > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				snapshot.Set(SnapshotValue.BotsAverageCurrentSpeed, 0);
			}
			try
			{
				snapshot.Set(SnapshotValue.BotsAverageMaxSpeed, ((from bot in bots select bot.InfoSpeedMax).Sum() / (from bot in bots where bot.InfoSpeedMax > 0 select bot).Count()));
			}
			catch (DivideByZeroException)
			{
				snapshot.Set(SnapshotValue.BotsAverageMaxSpeed, 0);
			}

			snapshot.Set(SnapshotValue.Packets, (from packet in packets select packet).Count());
			snapshot.Set(SnapshotValue.PacketsConnected, (from packet in packets where packet.Connected select packet).Count());
			snapshot.Set(SnapshotValue.PacketsDisconnected, (from packet in packets where !packet.Connected select packet).Count());
			snapshot.Set(SnapshotValue.PacketsSize, (from packet in packets select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeDownloading, (from packet in packets where packet.Connected select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeNotDownloading, (from packet in packets where !packet.Connected select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeConnected, (from packet in packets where packet.Parent.Connected select packet.Size).Sum());
			snapshot.Set(SnapshotValue.PacketsSizeDisconnected, (from packet in packets where !packet.Parent.Connected select packet.Size).Sum());

			snapshot.Set(SnapshotValue.FileSizeDownloaded, (from file in files from part in file.Parts select part.DownloadedSize).Sum());
			snapshot.Set(SnapshotValue.FileSizeMissing, (from file in files from part in file.Parts select part.MissingSize).Sum());
			try
			{
				snapshot.Set(SnapshotValue.FileTimeMissing, (from file in files from part in file.Parts select part.TimeMissing).Max());
			}
			catch (Exception)
			{
				snapshot.Set(SnapshotValue.FileTimeMissing, 0);
			}

			return snapshot;
		}

		#endregion
	}
}
