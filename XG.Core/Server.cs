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

		public new Servers Parent
		{
			get { return base.Parent as Servers; }
			set { base.Parent = value; }
		}

		int _port = 0;
		[DataMember]
		public int Port
		{
			get { return _port; }
			set
			{
				if (_port != value)
				{
					_port = value;
					Modified = true;
				}
			}
		}

		SocketErrorCode _errorCode = SocketErrorCode.None;
		[DataMember]
		public SocketErrorCode ErrorCode
		{
			get { return _errorCode; }
			set
			{
				if (_errorCode != value)
				{
					_errorCode = value;
					Modified = true;
				}
			}
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Channel> Channels
		{
			get { return base.All.Cast<Channel>(); }
		}

		public Channel this[string name]
		{
			get
			{
				return (Channel)base.Named(name);
			}
		}

		public Bot BotByName(string aName)
		{
			Bot tBot = null;
			foreach (Channel chan in base.All)
			{
				tBot = chan[aName];
				if (tBot != null) { break; }
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
			if (!aChannel.StartsWith("#")) { aChannel = "#" + aChannel; }
			if (this[aChannel] == null)
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
