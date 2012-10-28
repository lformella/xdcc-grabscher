// 
//  AWorkerLoop.cs
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

namespace XG.Server.Worker
{
	public abstract class ALoopWorker : AWorker
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public Int64 SecondsToSleep { get; set; }
		DateTime _last;
		bool _allowRun;

		#endregion

		#region FUNCTIONS

		public ALoopWorker() : base()
		{
			_last = DateTime.MinValue;
			_allowRun = true;
		}

		protected override void StartRun()
		{
			while (_allowRun)
			{
				if (_last.AddSeconds(SecondsToSleep) < DateTime.Now)
				{
					_last = DateTime.Now;

					try
					{
						LoopRun();
					}
					catch(Exception ex)
					{
						_log.Fatal("LoopRun()", ex);
					}
				}

				Thread.Sleep(1000);
			}
		}

		protected override void StopRun()
		{
			_allowRun = false;

			Thread.Sleep(2000);
		}

		protected abstract void LoopRun();

		#endregion
	}
}
