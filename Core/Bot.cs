// 
//  Bot.cs
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
using System.Net;
using System.Runtime.Serialization;

namespace XG.Core
{
	[Serializable]
	[DataContract]
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

		[DataMember]
		[MySql]
		public States State
		{
			get { return _state; }
			set
			{
				if (_state != value)
				{
					_state = value;
					Modified = true;
				}

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

		[DataMember]
		[MySql]
		public string LastMessage
		{
			get { return _lastMessage; }
			set
			{
				if (SetProperty(ref _lastMessage, value))
				{
					_lastContact = DateTime.Now;
				}
			}
		}

		DateTime _lastContact = DateTime.MinValue.ToUniversalTime();

		[DataMember]
		[MySql]
		public DateTime LastContact
		{
			get { return _lastContact; }
			set { SetProperty(ref _lastContact, value); }
		}

		int _queuePosition;

		[DataMember]
		public int QueuePosition
		{
			get { return _queuePosition; }
			set { SetProperty(ref _queuePosition, value); }
		}

		int _queueTime;

		[DataMember]
		public int QueueTime
		{
			get { return _queueTime; }
			set { SetProperty(ref _queueTime, value); }
		}

		Int64 _infoSpeedMax;

		[DataMember]
		[MySql]
		public Int64 InfoSpeedMax
		{
			get { return _infoSpeedMax; }
			set { SetProperty(ref _infoSpeedMax, value); }
		}

		Int64 _infoSpeedCurrent;

		[DataMember]
		[MySql]
		public Int64 InfoSpeedCurrent
		{
			get { return _infoSpeedCurrent; }
			set { SetProperty(ref _infoSpeedCurrent, value); }
		}

		int _infoSlotTotal;

		[DataMember]
		[MySql]
		public int InfoSlotTotal
		{
			get { return _infoSlotTotal; }
			set { SetProperty(ref _infoSlotTotal, value); }
		}

		int _infoSlotCurrent;

		[DataMember]
		[MySql]
		public int InfoSlotCurrent
		{
			get { return _infoSlotCurrent; }
			set { SetProperty(ref _infoSlotCurrent, value); }
		}

		int _infoQueueTotal;

		[DataMember]
		[MySql]
		public int InfoQueueTotal
		{
			get { return _infoQueueTotal; }
			set { SetProperty(ref _infoQueueTotal, value); }
		}

		int _infoQueueCurrent;

		[DataMember]
		[MySql]
		public int InfoQueueCurrent
		{
			get { return _infoQueueCurrent; }
			set { SetProperty(ref _infoQueueCurrent, value); }
		}

		[DataMember]
		public double Speed
		{
			get { return (from pack in Packets where pack.Part != null select pack.Part.Speed).Sum(); }
			private set { throw new NotSupportedException("You can not set this Property."); }
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
