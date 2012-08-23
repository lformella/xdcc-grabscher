//
//  Copyright (C) 2012 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using log4net;
using XG.Core;
using XG.Server.Plugin;

namespace XG.Server.Plugin.Backend.File
{
	public class BackendPlugin : AServerBackendPlugin
	{
		#region VARIABLES

		private static readonly ILog log = LogManager.GetLogger(typeof(BackendPlugin));

		private BinaryFormatter formatter = new BinaryFormatter();

		private Thread saveDataThread;

		private bool isSaveFile = false;
		private object saveFileLock = new object();
		private object saveSearchLock = new object();

		#endregion

		#region IServerBackendPlugin

		public override XG.Core.Repository.Object GetObjectRepository ()
		{
			try
			{
				this.ObjectRepository = (XG.Core.Repository.Object)this.Load(Settings.Instance.DataBinary);
				this.ObjectRepository.AttachCildEvents();
			}
			catch {}
			if (this.ObjectRepository == null)
			{
				this.ObjectRepository = new XG.Core.Repository.Object();
			}
			return this.ObjectRepository;
		}

		public override XG.Core.Repository.File GetFileRepository ()
		{
			try
			{
				this.FileRepository = (XG.Core.Repository.File)this.Load(Settings.Instance.FilesBinary);
				this.FileRepository.AttachCildEvents();
			}
			catch {}
			if (this.FileRepository == null)
			{
				this.FileRepository = new XG.Core.Repository.File();
			}
			return this.FileRepository;
		}

		public override List<string> GetSearchRepository ()
		{
			List<string> searches = null;
			try
			{
				searches = (List<string>)this.Load(Settings.Instance.SearchesBinary);
			}
			catch {}
			if (searches == null)
			{
				searches = new List<string>();
			}
			return searches;
		}

		#endregion

		#region RUN STOP

		public override void Start ()
		{
			// start data saving routine
			this.saveDataThread = new Thread(new ThreadStart(SaveDataLoop));
			this.saveDataThread.Start();

			this.Parent.SearchAddedEvent += new DataTextDelegate (Parent_SearchAddedEvent);
			this.Parent.SearchRemovedEvent += new DataTextDelegate (Parent_SearchRemovedEvent);
		}

		public override void Stop ()
		{
			this.Parent.SearchAddedEvent -= new DataTextDelegate (Parent_SearchAddedEvent);
			this.Parent.SearchRemovedEvent -= new DataTextDelegate (Parent_SearchRemovedEvent);

			this.saveDataThread.Abort();
		}
		
		#endregion

		#region EVENTS

		protected override void ObjectRepository_ObjectAddedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void ObjectRepository_ObjectRemovedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void ObjectRepository_ObjectChangedEventHandler (XGObject aObj)
		{
		}

		protected override void FileRepository_ObjectAddedEventHandler (XGObject aParentObj, XGObject aObj)
		{
			this.SaveFileDataNow();
		}

		protected override void FileRepository_ObjectRemovedEventHandler (XGObject aParentObj, XGObject aObj)
		{
			this.SaveFileDataNow();
		}

		protected override void FileRepository_ObjectChangedEventHandler(XGObject aObj)
		{
			if (aObj.GetType() == typeof(XGFile))
			{
				this.SaveFileDataNow();
			}
			else if (aObj.GetType() == typeof(XGFilePart))
			{
				XGFilePart part = aObj as XGFilePart;
				// if this change is lost, the data might be corrupt, so save it NOW
				if (part.PartState != FilePartState.Open)
				{
					this.SaveFileDataNow();
				}
				// the data saving can be scheduled
				else
				{
					this.isSaveFile = true;
				}
			}
		}

		private void Parent_SearchAddedEvent(string aSearch)
		{
			lock (this.saveSearchLock)
			{
				this.Save(this.Parent.Searches, Settings.Instance.SearchesBinary);
			}
		}

		private void Parent_SearchRemovedEvent(string aSearch)
		{
			lock (this.saveSearchLock)
			{
				this.Save(this.Parent.Searches, Settings.Instance.SearchesBinary);
			}
		}

		#endregion

		#region SAVE + LOAD

		/// <summary>
		/// Serializes an object into a file
		/// </summary>
		/// <param name="aObj"></param>
		/// <param name="aFile"></param>
		private void Save(object aObj, string aFile)
		{
			try
			{
				Stream streamWrite = System.IO.File.Create(aFile + ".new");
				this.formatter.Serialize(streamWrite, aObj);
				streamWrite.Close();
				try { System.IO.File.Delete(aFile + ".bak"); }
				catch (Exception) { };
				try { System.IO.File.Move(aFile, aFile + ".bak"); }
				catch (Exception) { };
				System.IO.File.Move(aFile + ".new", aFile);
				log.Debug("Save(" + aFile + ")");
			}
			catch (Exception ex)
			{
				log.Fatal("Save(" + aFile + ")", ex);
			}
		}

		/// <summary>
		/// Deserializes an object from a file
		/// </summary>
		/// <param name="aFile">Name of the File</param>
		/// <returns>the object or null if the deserializing failed</returns>
		private object Load(string aFile)
		{
			object obj = null;
			if (System.IO.File.Exists(aFile))
			{
				try
				{
					Stream streamRead = System.IO.File.OpenRead(aFile);
					obj = this.formatter.Deserialize(streamRead);
					streamRead.Close();
					log.Debug("Load(" + aFile + ")");
				}
				catch (Exception ex)
				{
					log.Fatal("Load(" + aFile + ")" , ex);
					// try to load the backup
					try
					{
						Stream streamRead = System.IO.File.OpenRead(aFile + ".bak");
						obj = this.formatter.Deserialize(streamRead);
						streamRead.Close();
						log.Debug("Load(" + aFile + ".bak)");
					}
					catch (Exception)
					{
						log.Fatal("Load(" + aFile + ".bak)", ex);
					}
				}
			}
			return obj;
		}

		private void SaveDataLoop()
		{
			DateTime timeIrc = DateTime.Now;
			DateTime timeStats = DateTime.Now;

			while (true)
			{
				// IRC Data
				if ((DateTime.Now - timeIrc).TotalMilliseconds > Settings.Instance.BackupDataTime)
				{
					timeIrc = DateTime.Now;

					this.Save(this.Parent.ObjectRepository, Settings.Instance.DataBinary);
				}

				// File Data
				if (this.isSaveFile)
				{
					lock (this.saveFileLock)
					{
						this.Save(this.Parent.FileRepository, Settings.Instance.FilesBinary);
						this.isSaveFile = false;
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
		private void SaveFileDataNow()
		{
			lock (this.saveFileLock)
			{
				this.Save(this.Parent.FileRepository, Settings.Instance.FilesBinary);
				this.isSaveFile = false;
			}
		}

		#endregion
	}
}

