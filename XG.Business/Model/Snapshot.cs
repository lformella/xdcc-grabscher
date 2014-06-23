// 
//  Snapshot.cs
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

using System.Collections.Generic;

namespace XG.Business.Model
{
	public enum SnapshotValue
	{
		Timestamp = 0,

		Speed = 1,

		Servers = 2,
		ServersEnabled = 21,
		ServersDisabled = 22,
		ServersConnected = 3,
		ServersDisconnected = 4,

		Channels = 5,
		ChannelsEnabled = 23,
		ChannelsDisabled = 24,
		ChannelsConnected = 6,
		ChannelsDisconnected = 7,

		Bots = 8,
		BotsConnected = 9,
		BotsDisconnected = 10,
		BotsFreeSlots = 11,
		BotsFreeQueue = 12,
		BotsAverageCurrentSpeed = 19,
		BotsAverageMaxSpeed = 20,

		Packets = 13,
		PacketsConnected = 14,
		PacketsDisconnected = 15,
		PacketsSize = 16,
		PacketsSizeDownloading = 17,
		PacketsSizeNotDownloading = 18,
		PacketsSizeConnected = 25,
		PacketsSizeDisconnected = 26,

		FileSize = 30,
		FileSizeDownloaded = 27,
		FileSizeMissing = 28,
		FileTimeMissing = 29
	}

	public class Snapshot
	{
		readonly Dictionary<SnapshotValue, double> _dic;
		public static readonly int SnapshotCount = 29;

		public Snapshot()
		{
			_dic = new Dictionary<SnapshotValue, double>();

			// fill with null values
			for (int a = 0; a < Snapshot.SnapshotCount; a++)
			{
				Set((SnapshotValue) a, 0);
			}
		}

		public void Set(SnapshotValue aType, double aValue)
		{
			if (_dic.ContainsKey(aType))
			{
				_dic[aType] = aValue;
			}
			else
			{
				_dic.Add(aType, aValue);
			}
		}

		public double Get(SnapshotValue aType)
		{
			return _dic.ContainsKey(aType) ? _dic[aType] : 0;
		}
	}
}
