// 
//  Server.cs
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
using System.Runtime.Serialization;

namespace XG.Core
{
	[Serializable]
	[DataContract]
	public class Server : AObjects
	{
		#region VARIABLES

		[DataMember]
		public override bool Connected
		{
			get { return base.Connected; }
			set
			{
				if (!value)
				{
					foreach (AObject obj in All)
					{
						obj.Connected = value;
					}
				}
				base.Connected = value;
			}
		}

		public new Servers Parent
		{
			get { return base.Parent as Servers; }
			set { base.Parent = value; }
		}

		int _port;

		[DataMember]
		[MySql]
		public int Port
		{
			get { return _port; }
			set { SetProperty(ref _port, value); }
		}

		SocketErrorCode _errorCode = SocketErrorCode.None;

		[DataMember]
		[MySql]
		public SocketErrorCode ErrorCode
		{
			get { return _errorCode; }
			set { SetProperty(ref _errorCode, value); }
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Channel> Channels
		{
			get { return base.All.Cast<Channel>(); }
		}

		public Channel Channel(string aName)
		{
			if (!aName.StartsWith("#"))
			{
				aName = "#" + aName;
			}
			return (Channel) base.Named(aName);
		}

		public Bot Bot(string aName)
		{
			Bot tBot = null;
			foreach (Channel chan in Channels)
			{
				tBot = chan.Bot(aName);
				if (tBot != null)
				{
					break;
				}
			}
			return tBot;
		}

		public void AddChannel(Channel aChannel)
		{
			base.Add(aChannel);
		}

		public void AddChannel(string aChannel)
		{
			aChannel = aChannel.Trim().ToLower();
			if (!aChannel.StartsWith("#"))
			{
				aChannel = "#" + aChannel;
			}
			if (Channel(aChannel) == null)
			{
				Channel tChannel = new Channel();
				tChannel.Name = aChannel;
				tChannel.Enabled = Enabled;
				AddChannel(tChannel);
			}
		}

		public void RemoveChannel(Channel aChannel)
		{
			base.Remove(aChannel);
		}

		#endregion
	}
}
