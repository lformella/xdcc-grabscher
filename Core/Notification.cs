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

namespace XG.Core
{
	[Serializable]
	public class Notification : AObject
	{
		#region ENUMS

		public enum Types
		{
			None = 0,
			PacketCompleted = 1,
			PacketIncompleted = 2,
			PacketBroken = 3,

			PacketRequested = 4,
			PacketRemoved = 5,

			FileCompleted = 6,
			FileSizeMismatch = 7,
			FileBuildFailed = 8,

			ServerConnected = 9,
			ServerConnectFailed = 10,

			ChannelJoined = 11,
			ChannelJoinFailed = 12,
			ChannelBanned = 13,
			ChannelParted = 14,
			ChannelKicked = 15,

			BotConnected = 16,
			BotConnectFailed = 17,
			BotSubmittedWrongPort = 18
		}

		#endregion

		#region VARIABLES

		Types _type;

		public Types Type
		{
			get { return _type; }
			set { SetProperty(ref _type, value, "Type"); }
		}

		AObject _object;

		public AObject Object
		{
			get { return _object; }
			set { SetProperty(ref _object, value, "Object"); }
		}

		#endregion

		#region CONSTRUCTOR

		public Notification(Notification.Types aType, AObject aObject)
		{
			_type = aType;
			_object = aObject;
		}

		#endregion
	}
}
