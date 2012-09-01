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
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

using log4net;

using XG.Core;
using XG.Server.Helper;

namespace XG.Server.Plugin.Backend.File
{
	public class BackendPlugin : ABackendPlugin
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(BackendPlugin));

		BinaryFormatter _formatter = new BinaryFormatter();

		Thread _saveDataThread;

		bool _isSaveFile = false;
		object _saveFileLock = new object();
		object _saveSearchLock = new object();

		#endregion

		#region IServerBackendPlugin

		public override Servers LoadServers ()
		{
			Servers objectRepository = null;
			try
			{
				objectRepository = (Servers)Load(Settings.Instance.DataBinary);
				objectRepository.AttachChildEvents();
			}
			catch {}
			if (objectRepository == null)
			{
				objectRepository = new Servers();
			}
			return objectRepository;
		}

		public override Files LoadFiles ()
		{
			Files fileRepository = null;
			try
			{
				fileRepository = (Files)Load(Settings.Instance.FilesBinary);
				fileRepository.AttachChildEvents();
			}
			catch {}
			if (fileRepository == null)
			{
				fileRepository = new Files();
			}
			return fileRepository;
		}

		public override Objects LoadSearches ()
		{
			Objects objectRepository = null;
			try
			{
				objectRepository = (Objects)Load(Settings.Instance.SearchesBinary);
				objectRepository.AttachChildEvents();
			}
			catch {}
			if (objectRepository == null)
			{
				objectRepository = new Objects();
			}
			return objectRepository;
		}

		#endregion

		#region RUN STOP

		public override void Start ()
		{
			// start data saving routine
			_saveDataThread = new Thread(new ThreadStart(SaveDataLoop));
			_saveDataThread.Start();
		}

		public override void Stop ()
		{
			_saveDataThread.Abort();
		}
		
		#endregion

		#region EVENTS

		protected override void FileAdded (AObject aParentObj, AObject aObj)
		{
			SaveFileDataNow();
		}

		protected override void FileRemoved (AObject aParentObj, AObject aObj)
		{
			SaveFileDataNow();
		}

		protected override void FileChanged(AObject aObj)
		{
			if (aObj is XG.Core.File)
			{
				SaveFileDataNow();
			}
			else if (aObj is FilePart)
			{
				FilePart part = aObj as FilePart;
				// if this change is lost, the data might be corrupt, so save it NOW
				if (part.PartState != FilePartState.Open)
				{
					SaveFileDataNow();
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
			lock (_saveSearchLock)
			{
				Save(Searches, Settings.Instance.SearchesBinary);
			}
		}

		protected override void SearchRemoved(AObject aParent, AObject aObj)
		{
			lock (_saveSearchLock)
			{
				Save(Searches, Settings.Instance.SearchesBinary);
			}
		}

		#endregion

		#region SAVE + LOAD

		/// <summary>
		/// Serializes an object into a file
		/// </summary>
		/// <param name="aObj"></param>
		/// <param name="aFile"></param>
		void Save(object aObj, string aFile)
		{
			try
			{
				Stream streamWrite = System.IO.File.Create(aFile + ".new");
				_formatter.Serialize(streamWrite, aObj);
				streamWrite.Close();
				Filesystem.DeleteFile(aFile + ".bak");
				Filesystem.MoveFile(aFile, aFile + ".bak");
				System.IO.File.Move(aFile + ".new", aFile);
				_log.Debug("Save(" + aFile + ")");
			}
			catch (Exception ex)
			{
				_log.Fatal("Save(" + aFile + ")", ex);
			}
		}

		/// <summary>
		/// Deserializes an object from a file
		/// </summary>
		/// <param name="aFile">Name of the File</param>
		/// <returns>the object or null if the deserializing failed</returns>
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
					_log.Fatal("Load(" + aFile + ")" , ex);
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

		void SaveDataLoop()
		{
			DateTime timeIrc = DateTime.Now;
			DateTime timeStats = DateTime.Now;

			while (true)
			{
				// IRC Data
				if ((DateTime.Now - timeIrc).TotalMilliseconds > Settings.Instance.BackupDataTime)
				{
					timeIrc = DateTime.Now;

					Save(Servers, Settings.Instance.DataBinary);
				}

				// File Data
				if (_isSaveFile)
				{
					lock (_saveFileLock)
					{
						Save(Files, Settings.Instance.FilesBinary);
						_isSaveFile = false;
					}
				}

				// Statistics
				if ((DateTime.Now - timeStats).TotalMilliseconds > Settings.Instance.BackupStatisticTime)
				{
					timeStats = DateTime.Now;
					Statistic.Instance.Save();
				}

				Thread.Sleep((int)Settings.Instance.TimerSleepTime);
			}
		}

		/// <summary>
		/// Save the FileData right now 
		/// </summary>
		void SaveFileDataNow()
		{
			lock (_saveFileLock)
			{
				Save(Files, Settings.Instance.FilesBinary);
				_isSaveFile = false;
			}
		}

		#endregion
	}
}

