//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Linq;

namespace XG.Core
{
	[Serializable()]
	public class XGServer : XGObject
	{
		#region EVENTS

		[field: NonSerialized()]
		public event ServerChannelDelegate ChannelAddedEvent;

		[field: NonSerialized()]
		public event ServerChannelDelegate ChannelRemovedEvent;

		#endregion

		#region VARIABLES

		public new RootObject Parent
		{
			get { return base.Parent as RootObject; }
			set { base.Parent = value; }
		}

		public new bool Connected
		{
			get { return base.Connected; }
			set
			{
				if (!value)
				{
					foreach (XGObject tObj in base.Children)
					{
						tObj.Connected = value;
					}
				}
				base.Connected = value;
			}
		}

		private int port = 0;
		public int Port
		{
			get { return this.port; }
			set
			{
				if (this.port != value)
				{
					this.port = value;
					this.Modified = true;
				}
			}
		}

		private SocketErrorCode errorCode = SocketErrorCode.None;
		public SocketErrorCode ErrorCode
		{
			get { return this.errorCode; }
			set
			{
				if (this.errorCode != value)
				{
					this.errorCode = value;
					this.Modified = true;
				}
			}
		}

		#endregion

		#region CHILDREN

		public IEnumerable<XGChannel> Channels
		{
			get { return base.Children.Cast<XGChannel>(); }
		}

		public XGChannel this[string name]
		{
			get
			{
				name = name.Trim().ToLower();
				if (!name.StartsWith("#")) { name = "#" + name; }
				try
				{
					return this.Channels.First(chan => chan.Name.Trim().ToLower() == name.Trim().ToLower());
				}
				catch {}
				return null;
			}
		}

		public XGBot GetBot(string aName)
		{
			XGBot tBot = null;
			foreach (XGChannel chan in base.Children)
			{
				tBot = chan[aName];
				if (tBot != null){ break; }
			}
			return tBot;
		}

		public void AddChannel(XGChannel aChannel)
		{
			if (base.AddChild(aChannel))
			{
				if (this.ChannelAddedEvent != null)
				{
					this.ChannelAddedEvent(this, aChannel);
				}
			}
		}

		public void AddChannel(string aChannel)
		{
			aChannel = aChannel.Trim().ToLower();
			if (!aChannel.StartsWith("#")) { aChannel = "#" + aChannel; }
			if (this[aChannel] == null)
			{
				XGChannel tChannel = new XGChannel();
				tChannel.Name = aChannel;
				tChannel.Enabled = this.Enabled;
				this.AddChannel(tChannel);
			}
		}

		public void RemoveChannel(XGChannel aChannel)
		{
			if (base.RemoveChild(aChannel))
			{
				if (this.ChannelRemovedEvent != null)
				{
					this.ChannelRemovedEvent(this, aChannel);
				}
			}
		}

		#endregion

		#region CONSTRUCTOR

		public XGServer() : base()
		{
		}

		public XGServer(RootObject parent) : this()
		{
			this.Parent = parent;
			this.Parent.AddServer(this);
		}

		public void Clone(XGServer aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
			this.port = aCopy.port;
		}

		#endregion
	}
}
