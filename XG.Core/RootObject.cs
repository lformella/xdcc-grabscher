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
