// 
//  Server.cs
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
using Newtonsoft.Json;

namespace XG.Plugin.ElasticSearch.Object
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Server : AObject
	{
		[JsonIgnore]
		public new Model.Domain.Server Object
		{
			get
			{
				return (Model.Domain.Server)base.Object;
			}
			set
			{
				base.Object = value;
			}
		}

		#region VARIABLES
		
		public int Port
		{
			get { return Object.Port; }
		}

		public Model.Domain.SocketErrorCode ErrorCode
		{
			get { return Object.ErrorCode; }
		}

		public int ChannelCount
		{
			get { return Object.Channels.Count(); }
		}

		public int ChannelCountConnected
		{
			get { return (from channel in Object.Channels where channel.Connected select channel).Count(); }
		}

		public int BotCount
		{
			get { return (from channel in Object.Channels from bot in channel.Bots select bot).Count(); }
		}

		public int BotCountConnected
		{
			get { return (from channel in Object.Channels from bot in channel.Bots where bot.Connected select bot).Count(); }
		}

		public int PacketCount
		{
			get { return (from channel in Object.Channels from bot in channel.Bots from packet in bot.Packets select packet).Count(); }
		}

		public int PacketCountConnected
		{
			get { return (from channel in Object.Channels from bot in channel.Bots where bot.Connected from packet in bot.Packets select packet).Count(); }
		}
		
		public Int64 PacketSize
		{
			get { return (from channel in Object.Channels from bot in channel.Bots from packet in bot.Packets select packet.Size).Sum(); }
		}
		
		public Int64 PacketSizeConnected
		{
			get { return (from channel in Object.Channels from bot in channel.Bots where bot.Connected from packet in bot.Packets select packet.Size).Sum(); }
		}

		public string IrcLink
		{
			get { return "irc://" + Object.Name + ":" + Object.Port + "/"; }
		}

		#endregion
	}
}
