// 
//  BotWatchdogWorker.cs
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
using System.Reflection;

using XG.Core;

using log4net;

namespace XG.Server.Worker
{
	public class BotWatchdogWorker : ALoopWorker
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#region AWorker

		protected override void LoopRun()
		{
			Bot[] tBots = (from server in Servers.All
			                where server.Connected
			                from channel in server.Channels
			                where channel.Connected
			                from bot in channel.Bots
			                where
				                !bot.Connected && (DateTime.Now - bot.LastContact).TotalSeconds > Settings.Instance.BotOfflineTime &&
				                bot.OldestActivePacket() == null
			                select bot).ToArray();

			int a = tBots.Count();
			foreach (Bot tBot in tBots)
			{
				tBot.Parent.RemoveBot(tBot);
			}
			if (a > 0)
			{
				Log.Info("RunBotWatchdog() removed " + a + " offline bot(s)");
			}

			// TODO scan for empty channels and send a "xdcc list" command to all the people in there
			// in some channels the bots are silent and have the same (no) rights like normal users
		}

		#endregion
	}
}
