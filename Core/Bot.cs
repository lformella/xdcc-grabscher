// 
//  Bot.cs
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

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace XG.Core
{
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	public class Bot : AObjects
	{
		#region ENUMS

		public enum States : byte
		{
			Idle,
			Active,
			Waiting
		}

		#endregion

		#region VARIABLES

		public new Channel Parent
		{
			get { return base.Parent as Channel; }
			set { base.Parent = value; }
		}

		[NonSerialized]
		Packet _currentQueuedPacket;

		public Packet CurrentQueuedPacket
		{
			get { return _currentQueuedPacket; }
		}

		States _state;

		[JsonProperty]
		[MySql]
		public States State
		{
			get { return _state; }
			set
			{
				SetProperty(ref _state, value);

				if (value == States.Waiting)
				{
					_currentQueuedPacket = OldestActivePacket();
				}
			}
		}

		[NonSerialized]
		IPAddress _ip = IPAddress.None;

		public IPAddress Ip
		{
			get { return _ip; }
			set { SetProperty(ref _ip, value); }
		}

		string _lastMessage = "";

		[JsonProperty]
		[MySql]
		public string LastMessage
		{
			get { return _lastMessage; }
			set
			{
				SetProperty(ref _lastMessage, value);
				_lastContact = DateTime.Now;
				_lastMessageTime = DateTime.Now;
			}
		}

		DateTime _lastMessageTime = DateTime.MinValue.ToUniversalTime();

		[JsonProperty]
		[MySql]
		public DateTime LastMessageTime
		{
			get { return _lastMessageTime; }
			set { SetProperty(ref _lastMessageTime, value); }
		}

		DateTime _lastContact = DateTime.MinValue.ToUniversalTime();

		[JsonProperty]
		[MySql]
		public DateTime LastContact
		{
			get { return _lastContact; }
			set { SetProperty(ref _lastContact, value); }
		}

		int _queuePosition;

		[JsonProperty]
		public int QueuePosition
		{
			get { return _queuePosition; }
			set { SetProperty(ref _queuePosition, value); }
		}

		int _queueTime;

		[JsonProperty]
		public int QueueTime
		{
			get { return _queueTime; }
			set { SetProperty(ref _queueTime, value); }
		}

		Int64 _infoSpeedMax;

		[JsonProperty]
		[MySql]
		public Int64 InfoSpeedMax
		{
			get { return _infoSpeedMax; }
			set { SetProperty(ref _infoSpeedMax, value); }
		}

		Int64 _infoSpeedCurrent;

		[JsonProperty]
		[MySql]
		public Int64 InfoSpeedCurrent
		{
			get { return _infoSpeedCurrent; }
			set { SetProperty(ref _infoSpeedCurrent, value); }
		}

		int _infoSlotTotal;

		[JsonProperty]
		[MySql]
		public int InfoSlotTotal
		{
			get { return _infoSlotTotal; }
			set { SetProperty(ref _infoSlotTotal, value); }
		}

		int _infoSlotCurrent;

		[JsonProperty]
		[MySql]
		public int InfoSlotCurrent
		{
			get { return _infoSlotCurrent; }
			set { SetProperty(ref _infoSlotCurrent, value); }
		}

		int _infoQueueTotal;

		[JsonProperty]
		[MySql]
		public int InfoQueueTotal
		{
			get { return _infoQueueTotal; }
			set { SetProperty(ref _infoQueueTotal, value); }
		}

		int _infoQueueCurrent;

		[JsonProperty]
		[MySql]
		public int InfoQueueCurrent
		{
			get { return _infoQueueCurrent; }
			set { SetProperty(ref _infoQueueCurrent, value); }
		}

		[JsonProperty]
		public Int64 Speed
		{
			get { return (from pack in Packets where pack.Part != null select pack.Part.Speed).Sum(); }
		}

		bool _hasNetworkProblems;

		[JsonProperty]
		public bool HasNetworkProblems
		{
			get { return _hasNetworkProblems; }
			set { SetProperty(ref _hasNetworkProblems, value); }
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Packet> Packets
		{
			get { return All.Cast<Packet>(); }
		}

		public Packet Packet(int aId)
		{
			try
			{
				return Packets.First(pack => pack.Id == aId);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public void AddPacket(Packet aPacket)
		{
			Add(aPacket);
		}

		public void RemovePacket(Packet aPacket)
		{
			Remove(aPacket);
		}

		public Packet OldestActivePacket()
		{
			try
			{
				return Packets.OrderBy(packet => packet.EnabledTime).First(pack => pack.Enabled);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		#endregion
	}
}
