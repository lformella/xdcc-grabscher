// 
//  FileSystem.cs
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

using log4net;

namespace XG.Server.Helper
{
	public class FileSystem
	{
		static readonly ILog _log = LogManager.GetLogger(typeof(FileSystem));

		/// <summary>
		/// Moves a file
		/// </summary>
		/// <param name="aNameOld">old filename</param>
		/// <param name="aNameNew">new filename</param>
		/// <returns>true if operation succeeded, false if it failed</returns>
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
					_log.Fatal("MoveFile('" + aNameOld + "', '" + aNameNew + "') ", ex);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Deletes a file
		/// </summary>
		/// <param name="aName">file to delete</param>
		/// <returns>true if operation succeeded, false if it failed</returns>
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
					_log.Fatal("DeleteFile(" + aName + ") ", ex);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Deletes a directory
		/// </summary>
		/// <param name="aName">directory to delete</param>
		/// <returns>true if operation succeeded, false if it failed</returns>
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
					_log.Fatal("DeleteDirectory(" + aName + ") ", ex);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Lists a directory
		/// </summary>
		/// <param name="aDir">directory to list</param>
		/// <returns>file list</returns>
		public static string[] ListDirectory(string aDir)
		{
			return ListDirectory(aDir, null);
		}

		/// <summary>
		/// Lists a directory with search pattern
		/// </summary>
		/// <param name="aDir">directory to list</param>
		/// <param name="aSearch">search pattern can be null to disable this</param>
		/// <returns>file list</returns>
		public static string[] ListDirectory(string aDir, string aSearch)
		{
			string[] files = new string[] { };
			try
			{
				files = aSearch == null ? Directory.GetFiles(aDir) : Directory.GetFiles(aDir, aSearch);
				Array.Sort(files);
			}
			catch (Exception ex)
			{
				_log.Fatal("ListDirectory('" + aDir + "', '" + aSearch + "') ", ex);
			}
			return files;
		}

		public static BinaryWriter OpenFileWritable(string aFile)
		{
			FileStream stream = File.Open(aFile, FileMode.Create, FileAccess.Write);
			return new BinaryWriter(stream);
		}

		public static BinaryReader OpenFileReadable(string aFile)
		{
			FileStream stream = File.Open(aFile, FileMode.Open, FileAccess.Read);
			return new BinaryReader(stream);
		}

		public static string ReadFile(string aFile)
		{
			string str = "";

			if(File.Exists(aFile))
			{
				try
				{
					StreamReader reader = new StreamReader(aFile);
					str = reader.ReadToEnd();
					reader.Close();
				}
				catch(Exception)
				{}
			}

			return str;
		}
	}
}
