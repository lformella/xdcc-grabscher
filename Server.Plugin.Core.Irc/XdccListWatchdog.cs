//
//  XdccListWatchdog.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
//
//  Copyright (c) 2013 Lars Formella
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using XG.Core;
using XG.Server.Worker;

namespace XG.Server.Plugin.Core.Irc
{
	public class XdccListWatchdog : ALoopWorker
	{
		#region VARIABLES

		public HashSet<IrcConnection> IrcConnections { get; set; }
		Dictionary<string, HashSet<string>> _askedUsers = new Dictionary<string, HashSet<string>>();

		#endregion

		#region AWorker

		protected override void LoopRun()
		{
			foreach (var connection in IrcConnections)
			{
				if (!connection.Server.Connected)
				{
					break;
				}

				if (!_askedUsers.ContainsKey(connection.Server.Name))
				{
					_askedUsers.Add(connection.Server.Name, new HashSet<string>());
				}
				HashSet<string> askedUsers = _askedUsers[connection.Server.Name];

				var channels = (from channel in connection.Server.Channels
				                where channel.Connected && channel.Bots.Count() == 0 && (DateTime.Now - channel.ConnectedTime).TotalSeconds > Settings.Instance.BotOfflineTime
				                select channel).ToArray();

				foreach (var channel in channels)
				{
					var ircChannel = connection.GetChannelInfo(channel.Name);

					if (ircChannel != null && !ircChannel.Mode.Contains("v"))
					{
						foreach (DictionaryEntry user in ircChannel.Voices)
						{
							string userName = (string)user.Key;
							if (!askedUsers.Contains(userName))
							{
								askedUsers.Add(userName);
								connection.RequestXdccHelp(userName);
							}
						}
					}
				}
			}
		}

		#endregion
	}
}

