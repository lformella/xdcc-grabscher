// 
//  AWorker.cs
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
using System.Reflection;
using System.Threading;
using log4net;
using Quartz;

namespace XG.Plugin
{
	public abstract class AWorker : ANotificationSender
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		bool _allowRunning;
		protected bool AllowRunning
		{
			get { return _allowRunning; }
		}

		public IScheduler Scheduler { get; set; }

		#endregion

		#region FUNCTIONS

		public void Start(string aName, bool aNewThread = true)
		{
			_allowRunning = true;
			try
			{
				if (aNewThread)
				{
					var thread = new Thread(StartRun);
					thread.Name = aName;
					thread.Start();
				}
				else
				{
					StartRun();
				}
			}
			catch (ThreadAbortException)
			{
				// this is ok
			}
			catch (Exception ex)
			{
				Log.Fatal("Start()", ex);
			}
		}

		protected virtual void StartRun() {}

		public void Stop()
		{
			_allowRunning = false;
			try
			{
				StopRun();
			}
			catch (Exception ex)
			{
				Log.Fatal("Stop()", ex);
			}
		}

		protected virtual void StopRun() {}

		#endregion
	}
}
