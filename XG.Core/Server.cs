using System;

namespace XG.Core
{
	[Serializable()]
	public class XGServer : XGObject
	{
		[field: NonSerialized()]
		public event ServerChannelDelegate ChannelAddedEvent;
		[field: NonSerialized()]
		public event ServerChannelDelegate ChannelRemovedEvent;

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
			get { return port; }
			set
			{
				if (port != value)
				{
					port = value;
					this.Modified = true;
				}
			}
		}

		public XGChannel this[string name]
		{
			get
			{
				name = name.ToLower();
				foreach (XGChannel chan in base.Children)
				{
					if (chan.Name.ToLower() == name) { return chan; }
				}
				return null;
			}
		}

		public XGBot getBot(string aName)
		{
			XGBot tBot = null;
			foreach (XGChannel chan in base.Children)
			{
				tBot = chan[aName];
				if (tBot != null) { return tBot; }
			}
			return tBot;
		}

		public void addChannel(XGChannel aChannel)
		{
			if (base.addChild(aChannel))
			{
				if (this.ChannelAddedEvent != null)
				{
					this.ChannelAddedEvent(this, aChannel);
				}
			}
		}
		public void addChannel(string aChannel)
		{
			if (!aChannel.StartsWith("#"))
			{
				aChannel = "#" + aChannel;
			}
			if (this[aChannel] == null)
			{
				XGChannel tChannel = new XGChannel();
				tChannel.Name = aChannel.Trim().ToLower();
				tChannel.Enabled = true;
				this.addChannel(tChannel);
			}
		}

		public void removeChannel(XGChannel aChannel)
		{
			if (base.removeChild(aChannel))
			{
				if (this.ChannelRemovedEvent != null)
				{
					this.ChannelRemovedEvent(this, aChannel);
				}
			}
		}
		public void removeChannel(string aChannel)
		{
			XGChannel tChan = this[aChannel];
			if (tChan != null)
			{
				this.removeChannel(tChan);
			}
		}

		public XGServer()
			: base()
		{
		}
		public XGServer(RootObject parent)
			: this()
		{
			this.Parent = parent;
			this.Parent.addServer(this);
		}

		public void Clone(XGServer aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
			this.port = aCopy.port;
		}
	}
}