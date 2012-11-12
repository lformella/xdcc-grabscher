// 
//  BackendPlugin.cs
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

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly BinaryFormatter _formatter = new BinaryFormatter();
		bool _isSaveFile;
		readonly object _saveObjectsLock = new object();
		readonly object _saveFilesLock = new object();
		readonly object _saveSearchesLock = new object();
		readonly object _saveSnapshotsLock = new object();

		string _dataBinary = "xg.bin";
		string _filesBinary = "xgfiles.bin";
		string _searchesBinary = "xgsearches.bin";
		string _snapshotsBinary = "xgsnapshots.bin";

		int BackupDataTime = 900000;

		#endregion

		#region ABackendPlugin

		public override Core.Servers LoadServers()
		{
			Core.Servers _servers = null;
			try
			{
				_servers = (Core.Servers) Load(Settings.Instance.AppDataPath + _dataBinary);
				_servers.AttachChildEvents();
			}
			catch {}
			if (_servers == null)
			{
				_servers = new Core.Servers();
			}
			return _servers;
		}

		public override Files LoadFiles()
		{
			Files _files = null;
			try
			{
				_files = (Files) Load(Settings.Instance.AppDataPath + _filesBinary);
				_files.AttachChildEvents();
			}
			catch {}
			if (_files == null)
			{
				_files = new Files();
			}
			return _files;
		}

		public override Objects LoadSearches()
		{
			Objects _searches = null;
			try
			{
				_searches = (Objects) Load(Settings.Instance.AppDataPath + _searchesBinary);
				_searches.AttachChildEvents();
			}
			catch {}
			if (_searches == null)
			{
				_searches = new Objects();
			}
			return _searches;
		}

		public override Snapshots LoadStatistics()
		{
			Snapshots _snapshots = null;
			try
			{
				_snapshots = (Snapshots) Load(Settings.Instance.AppDataPath + _snapshotsBinary);
			}
			catch {}
			if (_snapshots == null)
			{
				_snapshots = new Snapshots();
			}
			return _snapshots;
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			DateTime timeIrc = DateTime.Now;
			DateTime timeStats = DateTime.Now;

			while (true)
			{
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

				Thread.Sleep(Settings.Instance.RunLoopTime * 1000);
			}
		}

		protected override void StopRun()
		{
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
				FilePart part = aObj as FilePart;
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
				_log.Debug("Save(" + aFile + ")");
			}
			catch (Exception ex)
			{
				_log.Fatal("Save(" + aFile + ")", ex);
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
					_log.Debug("Load(" + aFile + ")");
				}
				catch (Exception ex)
				{
					_log.Fatal("Load(" + aFile + ")", ex);
					// try to load the backup
					try
					{
						Stream streamRead = System.IO.File.OpenRead(aFile + ".bak");
						obj = _formatter.Deserialize(streamRead);
						streamRead.Close();
						_log.Debug("Load(" + aFile + ".bak)");
					}
					catch (Exception)
					{
						_log.Fatal("Load(" + aFile + ".bak)", ex);
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
				return Save(Files, Settings.Instance.AppDataPath + _filesBinary);
			}
		}

		bool SaveObjects()
		{
			lock (_saveObjectsLock)
			{
				return Save(Servers, Settings.Instance.AppDataPath + _dataBinary);
			}
		}

		bool SaveSearches()
		{
			lock (_saveSearchesLock)
			{
				return Save(Searches, Settings.Instance.AppDataPath + _searchesBinary);
			}
		}

		bool SaveSnapshots()
		{
			lock (_saveSnapshotsLock)
			{
				return Save(Snapshots, Settings.Instance.AppDataPath + _snapshotsBinary);
			}
		}

		#endregion
	}
}
