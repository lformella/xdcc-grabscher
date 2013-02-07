// 
//  BackendPlugin.cs
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
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

using XG.Core;
using XG.Server.Helper;

using log4net;

namespace XG.Server.Plugin.Backend.File
{
	public class BackendPlugin : ABackendPlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly BinaryFormatter _formatter = new BinaryFormatter();

		bool _isSaveFile;

		readonly object _saveObjectsLock = new object();
		readonly object _saveFilesLock = new object();
		readonly object _saveSearchesLock = new object();
		readonly object _saveSnapshotsLock = new object();

		const string DataBinary = "xg.bin";
		const string FilesBinary = "xgfiles.bin";
		const string SearchesBinary = "xgsearches.bin";
		const string SnapshotsBinary = "xgsnapshots.bin";

		const int BackupDataTime = 900000;

		bool _allowRunning = true;

		#endregion

		#region ABackendPlugin

		public override Core.Servers LoadServers()
		{
			try
			{
				var servers = (Core.Servers) Load(Settings.Instance.AppDataPath + DataBinary);
				if (servers != null)
				{
					servers.AttachChildEvents();
					return servers;
				}
			}
			catch (Exception)
			{
				// skip all errors
			}
			return new Core.Servers();
		}

		public override Files LoadFiles()
		{
			try
			{
				var files = (Files) Load(Settings.Instance.AppDataPath + FilesBinary);
				if (files != null)
				{
					files.AttachChildEvents();
					return files;
				}
			}
			catch (Exception)
			{
				// skip all errors
			}
			return new Files();
		}

		public override Objects LoadSearches()
		{
			try
			{
				var searches = (Objects) Load(Settings.Instance.AppDataPath + SearchesBinary);
				if (searches != null)
				{
					searches.AttachChildEvents();
					return searches;
				}
			}
			catch (Exception)
			{
				// skip all errors
			}
			return new Objects();
		}

		public override Snapshots LoadStatistics()
		{
			try
			{
				var snapshots = (Snapshots) Load(Settings.Instance.AppDataPath + SnapshotsBinary);
				if (snapshots != null)
				{
					return snapshots;
				}
			}
			catch (Exception)
			{
				// skip all errors
			}
			return new Snapshots();
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			DateTime timeIrc = DateTime.Now;
			DateTime timeStats = DateTime.Now;

			DateTime _last = DateTime.Now;
			while (_allowRunning)
			{
				if (_last.AddSeconds(Settings.Instance.RunLoopTime) < DateTime.Now)
				{
					_last = DateTime.Now;

					// Objects
					if ((DateTime.Now - timeIrc).TotalSeconds > BackupDataTime)
					{
						timeIrc = DateTime.Now;

						SaveObjects();
					}

					// Files
					if (_isSaveFile)
					{
						SaveFiles();
					}

					// Statistics
					if ((DateTime.Now - timeStats).TotalSeconds > Settings.Instance.BackupStatisticTime)
					{
						timeStats = DateTime.Now;
						Statistic.Instance.Save();
					}
				}

				Thread.Sleep(500);
			}
		}

		protected override void StopRun()
		{
			_allowRunning = false;

			// sync all to disk
			SaveFiles();
			SaveObjects();
			SaveSearches();
			SaveSnapshots();
		}

		#endregion

		#region EVENTHANDLER

		protected override void FileAdded(AObject aParentObj, AObject aObj)
		{
			SaveFiles();
		}

		protected override void FileRemoved(AObject aParentObj, AObject aObj)
		{
			SaveFiles();
		}

		protected override void FileChanged(AObject aObj)
		{
			if (aObj is Core.File)
			{
				SaveFiles();
			}
			else if (aObj is FilePart)
			{
				var part = aObj as FilePart;
				// if this change is lost, the data might be corrupt, so save it now
				if (part.State != FilePart.States.Open)
				{
					SaveFiles();
				}
					// the data saving can be scheduled
				else
				{
					_isSaveFile = true;
				}
			}
		}

		protected override void SearchAdded(AObject aParent, AObject aObj)
		{
			SaveSearches();
		}

		protected override void SearchRemoved(AObject aParent, AObject aObj)
		{
			SaveSearches();
		}

		protected override void ObjectAdded(AObject aParent, AObject aObj)
		{
			if (aObj is Core.Server || aObj is Channel)
			{
				SaveObjects();
			}
		}

		protected override void ObjectRemoved(AObject aParent, AObject aObj)
		{
			if (aObj is Core.Server || aObj is Channel)
			{
				SaveObjects();
			}
		}

		protected override void ObjectEnabledChanged(AObject aObj)
		{
			if (aObj is Core.Server || aObj is Channel || aObj is Packet)
			{
				SaveObjects();
			}
		}

		protected override void SnapshotAdded(Snapshot aSnap)
		{
			SaveSnapshots();
		}

		#endregion

		#region SAVE + LOAD

		/// <summary>
		/// 	Serializes an object into a file
		/// </summary>
		/// <param name="aObj"> </param>
		/// <param name="aFile"> </param>
		bool Save(object aObj, string aFile)
		{
			try
			{
				Stream streamWrite = System.IO.File.Create(aFile + ".new");
				_formatter.Serialize(streamWrite, aObj);
				streamWrite.Close();
				FileSystem.DeleteFile(aFile + ".bak");
				FileSystem.MoveFile(aFile, aFile + ".bak");
				FileSystem.MoveFile(aFile + ".new", aFile);
				Log.Debug("Save(" + aFile + ")");
			}
			catch (Exception ex)
			{
				Log.Fatal("Save(" + aFile + ")", ex);
				return false;
			}
			return true;
		}

		/// <summary>
		/// 	Deserializes an object from a file
		/// </summary>
		/// <param name="aFile"> Name of the File </param>
		/// <returns> the object or null if the deserializing failed </returns>
		object Load(string aFile)
		{
			object obj = null;
			if (System.IO.File.Exists(aFile))
			{
				try
				{
					Stream streamRead = System.IO.File.OpenRead(aFile);
					obj = _formatter.Deserialize(streamRead);
					streamRead.Close();
					Log.Debug("Load(" + aFile + ")");
				}
				catch (Exception ex)
				{
					Log.Fatal("Load(" + aFile + ")", ex);
					// try to load the backup
					try
					{
						Stream streamRead = System.IO.File.OpenRead(aFile + ".bak");
						obj = _formatter.Deserialize(streamRead);
						streamRead.Close();
						Log.Debug("Load(" + aFile + ".bak)");
					}
					catch (Exception)
					{
						Log.Fatal("Load(" + aFile + ".bak)", ex);
					}
				}
			}
			return obj;
		}

		bool SaveFiles()
		{
			lock (_saveFilesLock)
			{
				_isSaveFile = false;
				return Save(Files, Settings.Instance.AppDataPath + FilesBinary);
			}
		}

		bool SaveObjects()
		{
			lock (_saveObjectsLock)
			{
				return Save(Servers, Settings.Instance.AppDataPath + DataBinary);
			}
		}

		bool SaveSearches()
		{
			lock (_saveSearchesLock)
			{
				return Save(Searches, Settings.Instance.AppDataPath + SearchesBinary);
			}
		}

		bool SaveSnapshots()
		{
			lock (_saveSnapshotsLock)
			{
				return Save(Snapshots, Settings.Instance.AppDataPath + SnapshotsBinary);
			}
		}

		#endregion
	}
}
