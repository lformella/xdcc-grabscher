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
using Db4objects.Db4o;

namespace XG.Model.Domain
{
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

		[Transient]
		Packet _currentQueuedPacket;

		public Packet CurrentQueuedPacket
		{
			get { return _currentQueuedPacket; }
		}

		[Transient]
		States _state;

		public States State
		{
			get { return GetProperty(ref _state); }
			set
			{
				SetProperty(ref _state, value, "State");

				if (value == States.Waiting)
				{
					_currentQueuedPacket = OldestActivePacket();
				}
			}
		}

		[Transient]
		IPAddress _ip = IPAddress.None;

		public IPAddress IP
		{
			get { return _ip; }
			set { _ip =  value; }
		}

		string _lastMessage = "";

		public string LastMessage
		{
			get { return GetProperty(ref _lastMessage); }
			set
			{
				SetProperty(ref _lastMessage, value, "LastMessage");
				LastContact = DateTime.Now;
				LastMessageTime = DateTime.Now;
			}
		}

		DateTime _lastMessageTime = DateTime.MinValue.ToUniversalTime();

		public DateTime LastMessageTime
		{
			get { return GetProperty(ref _lastMessageTime); }
			set { SetProperty(ref _lastMessageTime, value, "LastMessageTime"); }
		}

		DateTime _lastContact = DateTime.MinValue.ToUniversalTime();

		public DateTime LastContact
		{
			get { return GetProperty(ref _lastContact); }
			set { SetProperty(ref _lastContact, value, "LastContact"); }
		}

		[Transient]
		int _queuePosition;

		public int QueuePosition
		{
			get { return GetProperty(ref _queuePosition); }
			set { SetProperty(ref _queuePosition, value, "QueuePosition"); }
		}

		[Transient]
		int _queueTime;

		public int QueueTime
		{
			get { return GetProperty(ref _queueTime); }
			set { SetProperty(ref _queueTime, value, "QueueTime"); }
		}

		Int64 _infoSpeedMax;

		public Int64 InfoSpeedMax
		{
			get { return GetProperty(ref _infoSpeedMax); }
			set { SetProperty(ref _infoSpeedMax, value, "InfoSpeedMax"); }
		}

		Int64 _infoSpeedCurrent;

		public Int64 InfoSpeedCurrent
		{
			get { return GetProperty(ref _infoSpeedCurrent); }
			set { SetProperty(ref _infoSpeedCurrent, value, "InfoSpeedCurrent"); }
		}

		int _infoSlotTotal;

		public int InfoSlotTotal
		{
			get { return GetProperty(ref _infoSlotTotal); }
			set { SetProperty(ref _infoSlotTotal, value, "InfoSlotTotal"); }
		}

		int _infoSlotCurrent;

		public int InfoSlotCurrent
		{
			get { return GetProperty(ref _infoSlotCurrent); }
			set { SetProperty(ref _infoSlotCurrent, value, "InfoSlotCurrent"); }
		}

		int _infoQueueTotal;

		public int InfoQueueTotal
		{
			get { return GetProperty(ref _infoQueueTotal); }
			set { SetProperty(ref _infoQueueTotal, value, "InfoQueueTotal"); }
		}

		int _infoQueueCurrent;

		public int InfoQueueCurrent
		{
			get { return GetProperty(ref _infoQueueCurrent); }
			set { SetProperty(ref _infoQueueCurrent, value, "InfoQueueCurrent"); }
		}

		public Int64 Speed
		{
			get { return (from pack in Packets where pack.File != null select pack.File.Speed).Sum(); }
		}

		[Transient]
		bool _hasNetworkProblems;

		public bool HasNetworkProblems
		{
			get { return GetProperty(ref _hasNetworkProblems); }
			set { SetProperty(ref _hasNetworkProblems, value, "HasNetworkProblems"); }
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Packet> Packets
		{
			get { return Children.Cast<Packet>(); }
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

		protected override bool DuplicateChildExists(AObject aObject)
		{
			return Packet((aObject as Packet).Id) != null;
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return base.ToString() + (IP != null ? "|" + IP : "");
		}

		#endregion
	}
}
