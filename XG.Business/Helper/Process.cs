// 
//  Process.cs
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
using System.Diagnostics;
using System.Reflection;
using log4net;

namespace XG.Business.Helper
{
	public class Process
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public string Command { get; set; }
		public string Arguments { get; set; }
		public string Output { get; private set; }
		public string Error { get; private set; }

		#endregion

		#region RUN

		public bool Run()
		{
			bool result = true;
			
			Log.Info("Run(" + Command + ", " + Arguments + ")");
			try
			{
				var p = new System.Diagnostics.Process
				{
					StartInfo =
					{
						FileName = Command,
						Arguments = Arguments,
						CreateNoWindow = true,
						UseShellExecute = false,
						RedirectStandardInput = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					}
				};

				p.OutputDataReceived += OutputDataReceived;
				p.ErrorDataReceived += ErrorDataReceived;

				p.Start();

				p.BeginOutputReadLine();
				p.BeginErrorReadLine();

				p.StandardInput.Close();
				p.WaitForExit();

				p.OutputDataReceived -= OutputDataReceived;
				p.ErrorDataReceived -= ErrorDataReceived;

				p.Close();
			}
			catch (Exception ex)
			{
				Log.Fatal("Run(" + Command + ", " + Arguments + ")", ex);
				result = false;
			}
			finally
			{
				if (!String.IsNullOrWhiteSpace(Output))
				{
					Log.Info("Run(" + Command + ", " + Arguments + ") Output: " + Output);
				}
				if (!String.IsNullOrWhiteSpace(Error))
				{
					Log.Error("Run(" + Command + ", " + Arguments + ") Error: " + Error);
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
