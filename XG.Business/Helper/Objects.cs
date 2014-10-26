// 
//  Objects.cs
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
using XG.Business.Model;
using XG.Model;
using XG.Model.Domain;
using System.Collections.Generic;

namespace XG.Business.Helper
{
	public static class Objects
	{
		public static void CheckAndRemoveDuplicates(Servers aServers)
		{
			IEnumerable<Server> servers = (from server in aServers.All select server).ToArray();
			foreach(var obj in servers.GroupBy(obj => obj.Name).Where(list => list.Count() > 1).Select(list => list.Skip(1)).SelectMany(list => list))
			{
				obj.Parent.Remove(obj);
			}

			IEnumerable<Channel> channels = (from server in servers from channel in server.Channels select channel).ToArray();
			foreach(var obj in channels.GroupBy(obj => obj.Parent.Name + "/" + obj.Name).Where(list => list.Count() > 1).Select(list => list.Skip(1)).SelectMany(list => list))
			{
				obj.Parent.RemoveChannel(obj);
			}

			IEnumerable<Bot> bots = (from channel in channels from bot in channel.Bots select bot).ToArray();
			foreach(var obj in bots.GroupBy(obj => obj.Parent.Parent.Name + "/" + obj.Parent.Name + "/" + obj.Name).Where(list => list.Count() > 1).Select(list => list.Skip(1)).SelectMany(list => list))
			{
				obj.Parent.RemoveBot(obj);
			}

			IEnumerable<Packet> packets = (from bot in bots from packet in bot.Packets select packet).ToArray();
			foreach(var obj in packets.GroupBy(obj => obj.Parent.Parent.Parent.Name + "/" + obj.Parent.Parent.Name + "/" + obj.Parent.Name + "/" + obj.Id).Where(list => list.Count() > 1).Select(list => list.Skip(1)).SelectMany(list => list))
			{
				obj.Parent.RemovePacket(obj);
			}
		}
	}
}
