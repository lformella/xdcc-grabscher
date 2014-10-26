// 
//  BotWatchdog.cs
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
using System.Reflection;
using System.Threading;
using Quartz;
using XG.Config.Properties;
using XG.Model.Domain;
using log4net;

namespace XG.Business.Job
{
	public class BotWatchdog : IJob
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public Servers Servers { get; set; }

		public void Execute (IJobExecutionContext context)
		{
			Thread.CurrentThread.Priority = ThreadPriority.Lowest;

			Bot[] tBots = (from server in Servers.All
			                where server.Connected
			                from channel in server.Channels
			                where channel.Connected
			                from bot in channel.Bots
			                where
			                    !bot.Connected && (DateTime.Now - bot.LastContact).TotalSeconds > Settings.Default.BotOfflineTime &&
			                    bot.OldestActivePacket() == null
			                select bot).ToArray();

			int a = tBots.Length;
			foreach (Bot tBot in tBots)
			{
				tBot.Parent.RemoveBot(tBot);
			}
			if (a > 0)
			{
				Log.Info("Execute() removed " + a + " offline bot(s)");
			}
		}
	}
}
