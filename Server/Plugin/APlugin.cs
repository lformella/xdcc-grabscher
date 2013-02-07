// 
//  APlugin.cs
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

using XG.Core;
using XG.Server.Worker;

namespace XG.Server.Plugin
{
	public abstract class APlugin : AWorker
	{
		#region SERVER

		public void AddServer(string aString)
		{
			Servers.Add(aString);
		}

		public void RemoveServer(Guid aGuid)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				Servers.Remove(tObj as Core.Server);
			}
		}

		#endregion

		#region CHANNEL

		public void AddChannel(Guid aGuid, string aString)
		{
			var tServ = Servers.WithGuid(aGuid) as Core.Server;
			if (tServ != null)
			{
				tServ.AddChannel(aString);
			}
		}

		public void RemoveChannel(Guid aGuid)
		{
			var tChan = Servers.WithGuid(aGuid) as Channel;
			if (tChan != null)
			{
				tChan.Parent.RemoveChannel(tChan);
			}
		}

		#endregion

		#region OBJECT

		public void ActivateObject(Guid aGuid)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = true;
				tObj.Commit();
			}
		}

		public void DeactivateObject(Guid aGuid)
		{
			AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = false;
				tObj.Commit();
			}
		}

		#endregion
	}
}
