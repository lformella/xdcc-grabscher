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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace XG.Core
{
	[Serializable]
	public class Bot : AObjects
	{
		#region ENUMS

		public enum States : byte
		{
			Idle,
			Active,
			Waiting
		}

		public enum Rights : byte
		{
			None,
			Voice,
			HalfOp,
			Op
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

		public States State
		{
			get { return _state; }
			set
			{
				SetProperty(ref _state, value, "State");

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
			set { SetProperty(ref _ip, value, "Ip"); }
		}

		string _lastMessage = "";

		public string LastMessage
		{
			get { return _lastMessage; }
			set
			{
				SetProperty(ref _lastMessage, value, "LastMessage");
				_lastContact = DateTime.Now;
				_lastMessageTime = DateTime.Now;
			}
		}

		DateTime _lastMessageTime = DateTime.MinValue.ToUniversalTime();

		public DateTime LastMessageTime
		{
			get { return _lastMessageTime; }
			set { SetProperty(ref _lastMessageTime, value, "LastMessageTime"); }
		}

		DateTime _lastContact = DateTime.MinValue.ToUniversalTime();

		public DateTime LastContact
		{
			get { return _lastContact; }
			set { SetProperty(ref _lastContact, value, "LastContact"); }
		}

		int _queuePosition;

		public int QueuePosition
		{
			get { return _queuePosition; }
			set { SetProperty(ref _queuePosition, value, "QueuePosition"); }
		}

		int _queueTime;

		public int QueueTime
		{
			get { return _queueTime; }
			set { SetProperty(ref _queueTime, value, "QueueTime"); }
		}

		Int64 _infoSpeedMax;

		public Int64 InfoSpeedMax
		{
			get { return _infoSpeedMax; }
			set { SetProperty(ref _infoSpeedMax, value, "InfoSpeedMax"); }
		}

		Int64 _infoSpeedCurrent;

		public Int64 InfoSpeedCurrent
		{
			get { return _infoSpeedCurrent; }
			set { SetProperty(ref _infoSpeedCurrent, value, "InfoSpeedCurrent"); }
		}

		int _infoSlotTotal;

		public int InfoSlotTotal
		{
			get { return _infoSlotTotal; }
			set { SetProperty(ref _infoSlotTotal, value, "InfoSlotTotal"); }
		}

		int _infoSlotCurrent;

		public int InfoSlotCurrent
		{
			get { return _infoSlotCurrent; }
			set { SetProperty(ref _infoSlotCurrent, value, "InfoSlotCurrent"); }
		}

		int _infoQueueTotal;

		public int InfoQueueTotal
		{
			get { return _infoQueueTotal; }
			set { SetProperty(ref _infoQueueTotal, value, "InfoQueueTotal"); }
		}

		int _infoQueueCurrent;

		public int InfoQueueCurrent
		{
			get { return _infoQueueCurrent; }
			set { SetProperty(ref _infoQueueCurrent, value, "InfoQueueCurrent"); }
		}

		public Int64 Speed
		{
			get { return (from pack in Packets where pack.Part != null select pack.Part.Speed).Sum(); }
		}

		bool _hasNetworkProblems;

		public bool HasNetworkProblems
		{
			get { return _hasNetworkProblems; }
			set { SetProperty(ref _hasNetworkProblems, value, "HasNetworkProblems"); }
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
				return Packets.FirstOrDefault(pack => pack.Id == aId);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public bool AddPacket(Packet aPacket)
		{
			return Add(aPacket);
		}

		public bool RemovePacket(Packet aPacket)
		{
			return Remove(aPacket);
		}

		public Packet OldestActivePacket()
		{
			try
			{
				return Packets.OrderBy(packet => packet.EnabledTime).FirstOrDefault(pack => pack.Enabled);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		public override bool DuplicateChildExists(AObject aObject)
		{
			return Packet((aObject as Packet).Id) != null;
		}

		#endregion

		#region CONSTRUCTOR

		public Bot(Bot aObject = null, bool useHashset = true) : base(aObject, useHashset)
		{
			if (aObject != null)
			{
				_state = aObject._state;
				_lastMessage = aObject._lastMessage;
				_lastMessageTime = aObject._lastMessageTime;
				_lastContact = aObject._lastContact;
				_queuePosition = aObject._queuePosition;
				_queueTime = aObject._queueTime;
				_infoSpeedMax = aObject._infoSpeedMax;
				_infoSpeedCurrent = aObject._infoSpeedCurrent;
				_infoSlotTotal = aObject._infoSlotTotal;
				_infoSlotCurrent = aObject._infoSlotCurrent;
				_infoQueueTotal = aObject._infoQueueTotal;
				_infoQueueCurrent = aObject._infoQueueCurrent;
				_hasNetworkProblems = aObject._hasNetworkProblems;
			}
		}

		#endregion
	}
}
