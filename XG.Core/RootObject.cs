using System;

namespace XG.Core
{
	[Serializable()]
	public class RootObject : XGObject
	{
		[field: NonSerialized()]
		public event RootServerDelegate ServerAddedEvent;
		[field: NonSerialized()]
		public event RootServerDelegate ServerRemovedEvent;

		public XGServer this[string name]
		{
			get
			{
				foreach (XGServer serv in base.Children)
				{
					if (serv.Name == name)
					{
						return serv;
					}
				}
				return null;
			}
		}

		public void addServer(XGServer aServer)
		{
			if (base.addChild(aServer))
			{
				if (this.ServerAddedEvent != null)
				{
					this.ServerAddedEvent(this, aServer);
				}
			}
		}
		public void addServer(string aServer)
		{
			if (this[aServer] == null)
			{
				XGServer tServer = new XGServer();
				tServer.Name = aServer.Trim().ToLower();
				tServer.Port = 6667;
				tServer.Enabled = true;
				this.addServer(tServer);
			}
		}

		public void removeServer(XGServer aServer)
		{
			if (base.removeChild(aServer))
			{
				if (this.ServerRemovedEvent != null)
				{
					this.ServerRemovedEvent(this, aServer);
				}
			}
		}
		public void removeServer(string aServer)
		{
			XGServer tServ = this[aServer];
			if (tServ != null)
			{
				this.removeServer(tServ);
			}
		}

		public new void SetGuid(Guid aGuid)
		{
			base.SetGuid(aGuid);
		}

		public RootObject()
			: base()
		{
		}

		public void Clone(RootObject aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
		}
	}
}
