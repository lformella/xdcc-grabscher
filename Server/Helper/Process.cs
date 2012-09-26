// 
//  Process.cs
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
using System.Diagnostics;

using log4net;

namespace XG.Server.Helper
{
	public class Process
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(Process));

		public string Command { get; set; }
		public string Arguments { get; set; }
		public string Output { get; private set; }
		public string Error { get; private set; }

		#endregion

		#region RUN

		public bool Run ()
		{
			bool result = true;

			try
			{
				System.Diagnostics.Process p = new System.Diagnostics.Process();
				p.StartInfo.FileName = Command;
				p.StartInfo.Arguments = Arguments;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardInput = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;

				p.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
				p.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

				p.Start();

				p.BeginOutputReadLine();
				p.BeginErrorReadLine();

				p.StandardInput.Close();
				p.WaitForExit();

				p.OutputDataReceived -= new DataReceivedEventHandler(OutputDataReceived);
				p.ErrorDataReceived -= new DataReceivedEventHandler(ErrorDataReceived);

				p.Close();
			}
			catch (Exception ex)
			{
				_log.Fatal("Run(" + Command + ", " + Arguments + ")", ex);
				result = false;
			}
			finally
			{
				_log.Info("Run(" + Command + ", " + Arguments + ") Output: " + Output);
				_log.Info("Run(" + Command + ", " + Arguments + ") Error: " + Error);

				if (!string.IsNullOrEmpty(Error))
				{
					result = false;
				}
			}

			return result;
		}

		void OutputDataReceived(object aSendingProcess, DataReceivedEventArgs aOutline)
		{
			if (!String.IsNullOrEmpty(aOutline.Data))
			{
				Output += Environment.NewLine + aOutline.Data;
			}
		}

		void ErrorDataReceived(object aSendingProcess, DataReceivedEventArgs aOutline)
		{
			if (!String.IsNullOrEmpty(aOutline.Data))
			{
				Error += Environment.NewLine + aOutline.Data;
			}
		}

		#endregion
	}
}
