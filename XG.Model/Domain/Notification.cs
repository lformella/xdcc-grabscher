// 
//  Notification.cs
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

namespace XG.Model.Domain
{
	public class Notification : AObject
	{
		#region ENUMS

		public enum Types
		{
			None = 0,
			PacketCompleted = 1,
			PacketIncomplete = 2,
			PacketBroken = 3,
			PacketFileMismatch = 7,
			PacketNameDifferent = 19,

			PacketRequested = 4,
			PacketRemoved = 5,

			FileCompleted = 6,
			FileFinishFailed = 8,

			ServerConnected = 9,
			ServerConnectFailed = 10,

			ChannelJoined = 11,
			ChannelJoinFailed = 12,
			ChannelBanned = 13,
			ChannelParted = 14,
			ChannelKicked = 15,

			BotConnected = 16,
			BotConnectFailed = 17,
			BotSubmittedWrongData = 18
		}

		#endregion

		#region VARIABLES

		Types _type;

		public Types Type
		{
			get { return _type; }
			set { SetProperty(ref _type, value, "Type"); }
		}

		AObject _object1;

		public AObject Object1
		{
			get { return _object1; }
			set { SetProperty(ref _object1, value, "Object1"); }
		}

		AObject _object2;

		public AObject Object2
		{
			get { return _object2; }
			set { SetProperty(ref _object2, value, "Object2"); }
		}

		readonly DateTime _time;

		public DateTime Time
		{
			get { return _time; }
		}

		#endregion

		#region CONSTRUCTOR

		public Notification(Notification.Types aType, AObject aObject1, AObject aObject2 = null)
		{
			_type = aType;
			_object1 = aObject1;
			_object2 = aObject2;
			_time = DateTime.Now;
		}

		#endregion
	}
}
