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
		
		[DataMember]
		public override bool Connected
		{
			get { return base.Connected; }
			set
			{
				if (base.Connected != value)
				{
					base.Connected = value;
				}
			}
		}

		[NonSerialized]
		Packet _currentQueuedPacket = null;
		public Packet CurrentQueuedPacket
		{
			get { return _currentQueuedPacket; }
		}

		Bot.States _state;
		[DataMember]
		public Bot.States State
		{
			get { return _state; }
			set
			{
				if (_state != value)
				{
					_state = value;
					Modified = true;
				}

				if(value == Bot.States.Waiting)
				{
					_currentQueuedPacket = OldestActivePacket();
				}
			}
		}

		[NonSerialized]
		IPAddress _ip = IPAddress.None;
		public IPAddress IP
		{
			get { return _ip; }
			set
			{
				if (_ip != value)
				{
					_ip = value;
					Modified = true;
				}
			}
		}

		string _lastMessage = "";
		[DataMember]
		public string LastMessage
		{
			get { return _lastMessage; }
			set
			{
				if (_lastMessage != value)
				{
					_lastMessage = value;
					_lastContact = DateTime.Now;
					Modified = true;
				}
			}
		}

		DateTime _lastContact = DateTime.MinValue;
		[DataMember]
		public DateTime LastContact
		{
			get { return _lastContact; }
			set
			{
				if (_lastContact != value)
				{
					_lastContact = value;
					Modified = true;
				}
			}
		}

		int _queuePosition = 0;
		[DataMember]
		public int QueuePosition
		{
			get { return _queuePosition; }
			set
			{
				if (_queuePosition != value)
				{
					_queuePosition = value;
					Modified = true;
				}
			}
		}

		int _queueTime = 0;
		[DataMember]
		public int QueueTime
		{
			get { return _queueTime; }
			set
			{
				if (_queueTime != value)
				{
					_queueTime = value;
					Modified = true;
				}
			}
		}

		double _infoSpeedMax = 0;
		[DataMember]
		public double InfoSpeedMax
		{
			get { return _infoSpeedMax; }
			set
			{
				if (_infoSpeedMax != value)
				{
					_infoSpeedMax = value;
					Modified = true;
				}
			}
		}
		double _infoSpeedCurrent = 0;
		[DataMember]
		public double InfoSpeedCurrent
		{
			get { return _infoSpeedCurrent; }
			set
			{
				if (_infoSpeedCurrent != value)
				{
					_infoSpeedCurrent = value;
					Modified = true;
				}
			}
		}

		int _infoSlotTotal = 0;
		[DataMember]
		public int InfoSlotTotal
		{
			get { return _infoSlotTotal; }
			set
			{
				if (_infoSlotTotal != value)
				{
					_infoSlotTotal = value;
					Modified = true;
				}
			}
		}
		int _infoSlotCurrent = 0;
		[DataMember]
		public int InfoSlotCurrent
		{
			get { return _infoSlotCurrent; }
			set
			{
				if (_infoSlotCurrent != value)
				{
					_infoSlotCurrent = value;
					Modified = true;
				}
			}
		}

		int _infoQueueTotal = 0;
		[DataMember]
		public int InfoQueueTotal
		{
			get { return _infoQueueTotal; }
			set
			{
				if (_infoQueueTotal != value)
				{
					_infoQueueTotal = value;
					Modified = true;
				}
			}
		}
		int _infoQueueCurrent = 0;
		[DataMember]
		public int InfoQueueCurrent
		{
			get { return _infoQueueCurrent; }
			set
			{
				if (_infoQueueCurrent != value)
				{
					_infoQueueCurrent = value;
					Modified = true;
				}
			}
		}
		
		[DataMember]
		public double Speed
		{
			get
			{
				return (from pack in Packets where pack.Part != null select pack.Part.Speed).Sum();
			}
			private set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Packet> Packets
		{
			get { return base.All.Cast<Packet>(); }
		}

		public Packet this[int id]
		{
			get
			{
				try
				{
					return Packets.First(pack => pack.Id == id);
				}
				catch {}
				return null;
			}
		}

		public void AddPacket(Packet aPacket)
		{
			base.Add(aPacket);
		}
		
		public void RemovePacket(Packet aPacket)
		{
			base.Remove(aPacket);
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
