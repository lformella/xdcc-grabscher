// 
//  Filesystem.cs
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
using System.IO;
using System.Reflection;

using log4net;

namespace XG.Server.Helper
{
	public class FileSystem : ANotificationSender
	{
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// 	Moves a file
		/// </summary>
		/// <param name="aNameOld"> old filename </param>
		/// <param name="aNameNew"> new filename </param>
		/// <returns> true if operation succeeded, false if it failed </returns>
		public static bool MoveFile(string aNameOld, string aNameNew)
		{
			if (File.Exists(aNameOld))
			{
				try
				{
					File.Move(aNameOld, aNameNew);
					return true;
				}
				catch (Exception ex)
				{
					Log.Fatal("MoveFile('" + aNameOld + "', '" + aNameNew + "') ", ex);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// 	Deletes a file
		/// </summary>
		/// <param name="aName"> file to delete </param>
		/// <returns> true if operation succeeded, false if it failed </returns>
		public static bool DeleteFile(string aName)
		{
			if (File.Exists(aName))
			{
				try
				{
					File.Delete(aName);
					return true;
				}
				catch (Exception ex)
				{
					Log.Fatal("DeleteFile(" + aName + ") ", ex);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// 	Deletes a directory
		/// </summary>
		/// <param name="aName"> directory to delete </param>
		/// <returns> true if operation succeeded, false if it failed </returns>
		public static bool DeleteDirectory(string aName)
		{
			if (Directory.Exists(aName))
			{
				try
				{
					Directory.Delete(aName, true);
					return true;
				}
				catch (Exception ex)
				{
					Log.Fatal("DeleteDirectory(" + aName + ") ", ex);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// 	Lists a directory
		/// </summary>
		/// <param name="aDir"> directory to list </param>
		/// <returns> file list </returns>
		public static string[] ListDirectory(string aDir)
		{
			return ListDirectory(aDir, null);
		}

		/// <summary>
		/// 	Lists a directory with search pattern
		/// </summary>
		/// <param name="aDir"> directory to list </param>
		/// <param name="aSearch"> search pattern can be null to disable this </param>
		/// <returns> file list </returns>
		public static string[] ListDirectory(string aDir, string aSearch)
		{
			var files = new string[] {};
			try
			{
				files = aSearch == null ? Directory.GetFiles(aDir) : Directory.GetFiles(aDir, aSearch);
				Array.Sort(files);
			}
			catch (Exception ex)
			{
				Log.Fatal("ListDirectory('" + aDir + "', '" + aSearch + "') ", ex);
			}
			return files;
		}

		public static string ReadFile(string aFile)
		{
			if (File.Exists(aFile))
			{
				try
				{
					using (var reader = new StreamReader(aFile))
					{
						string str = reader.ReadToEnd();
						reader.Close();
						return str;
					}
				}
				catch (Exception)
				{
					return "";
				}
			}

			return "";
		}
	}
}
