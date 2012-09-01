// 
//  Servers.cs
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

namespace XG.Core
{
	[Serializable()]
	public class Servers : AObjects
	{
		public new IEnumerable<Server> All
		{
			get { return base.All.Cast<Server>(); }
		}

		public Server this[string name]
		{
			get
			{
				try
				{
					return All.First(serv => serv.Name == name.Trim().ToLower());
				}
				catch {}
				return null;
			}
		}

		public void Add(Server aServer)
		{
			base.Add(aServer);
		}
		public void Add(string aServer)
		{
			aServer = aServer.Trim().ToLower();
			if (this[aServer] == null)
			{
				Server tServer = new Server();
				tServer.Name = aServer;
				tServer.Port = 6667;
				tServer.Enabled = true;
				Add(tServer);
			}
		}

		public void Remove(Server aServer)
		{
			base.Remove(aServer);
		}
	}
}
