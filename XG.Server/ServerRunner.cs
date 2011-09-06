//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
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
using XG.Core;

namespace XG.Server
{
	/// <summary>
	/// This class holds all information about the files and the irc objects
	/// it does the following things
	/// - loading and saving the object to a file
	/// - calling the ServerHandler to connect to or disconnect from an irc server
	/// - communicate with XG.Server.* plugins, meaning the client
	/// </summary>
	public class ServerRunner
	{
		#region VARIABLES

		private ServerHandler myServerHandler;
		public RootObject myRootObject;
		public Guid RootGuid
		{
			get { return this.myRootObject != null ? this.myRootObject.Guid : Guid.Empty; }
		}

		private List<XGFile> myFiles;
		private List<string> mySearches;

		private BinaryFormatter myFormatter = new BinaryFormatter();

		private Thread mySaveDataThread;

		private bool isSaveFile = false;
		private object mySaveFileLock = new object();

		private object mySaveSearchLock = new object();

		#endregion

		#region EVENTS

		public event ObjectDelegate ObjectChangedEvent;
		public event ObjectObjectDelegate ObjectAddedEvent;
		public event ObjectObjectDelegate ObjectRemovedEvent;

		#endregion

		public ServerRunner()
		{
			// set the loglevel
			XGHelper.LogLevel = Settings.Instance.LogLevel;

			// the one and only root object
			this.myRootObject = (RootObject)this.Load(Settings.Instance.DataBinary);
			if (this.myRootObject == null) { this.myRootObject = new RootObject(); }
			this.myRootObject.ServerAddedEvent += new RootServerDelegate(rootObject_ServerAddedEventHandler);
			this.myRootObject.ServerRemovedEvent += new RootServerDelegate(rootObject_ServerRemovedEventHandler);

			// the file data
			this.myFiles = (List<XGFile>)this.Load(Settings.Instance.FilesBinary);
			if (this.myFiles == null) { this.myFiles = new List<XGFile>(); }

			// previous searches
			this.mySearches = (List<string>)this.Load(Settings.Instance.SearchesBinary);
			if (this.mySearches == null) { this.mySearches = new List<string>(); }

			#region SERVERHANDLER INIT

			this.myServerHandler = new ServerHandler(this.myFiles);
			this.myServerHandler.ParsingErrorEvent += new DataTextDelegate(myServerHandler_ParsingErrorEventHandler);

			this.myServerHandler.ObjectAddedEvent += new ObjectObjectDelegate(myServerHandler_ObjectAddedEventHandler);
			this.myServerHandler.ObjectChangedEvent += new ObjectDelegate(myServerHandler_ObjectChangedEventHandler);
			this.myServerHandler.ObjectRemovedEvent += new ObjectObjectDelegate(myServerHandler_ObjectRemovedEventHandler);

			#endregion

			#region DUPE CHECK

			// check if there are some dupes in our database
			foreach (XGServer serv in this.myRootObject.Children)
			{
				foreach (XGServer s in this.myRootObject.Children)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						this.Log("Run() removing dupe server " + s.Name, LogLevel.Error);
						this.myRootObject.RemoveServer(s);
					}
				}

				foreach (XGChannel chan in serv.Children)
				{
					foreach (XGChannel c in serv.Children)
					{
						if (c.Name == chan.Name && c.Guid != chan.Guid)
						{
							this.Log("Run() removing dupe channel " + c.Name, LogLevel.Error);
							serv.RemoveChannel(c);
						}
					}

					foreach (XGBot bot in chan.Children)
					{
						foreach (XGBot b in chan.Children)
						{
							if (b.Name == bot.Name && b.Guid != bot.Guid)
							{
								this.Log("Run() removing dupe bot " + b.Name, LogLevel.Error);
								chan.RemoveBot(b);
							}
						}

						foreach (XGPacket pack in bot.Children)
						{
							foreach (XGPacket p in bot.Children)
							{
								if (p.Id == pack.Id && p.Guid != pack.Guid)
								{
									this.Log("Run() removing dupe Packet " + p.Name, LogLevel.Error);
									bot.removePacket(p);
								}
							}
						}
					}
				}
			}

			#endregion

			#region RESET

			// reset all objects if the server crashed
			foreach (XGServer serv in this.myRootObject.Children)
			{
				serv.Connected = false;
				serv.ErrorCode = SocketErrorCode.None;

				foreach (XGChannel chan in serv.Children)
				{
					chan.Connected = false;

					foreach (XGBot bot in chan.Children)
					{
						bot.Connected = false;
						bot.BotState = BotState.Idle;

						foreach (XGPacket pack in bot.Children)
						{
							pack.Connected = false;
						}
					}
				}
			}

			#endregion

			#region CLEAR OLD DL

			if (this.myFiles.Count > 0 && Settings.Instance.ClearReadyDownloads)
			{
				foreach (XGFile file in this.myFiles.ToArray())
				{
					if (file.Enabled)
					{
						this.myFiles.Remove(file);
						this.Log("Run() removing ready file " + file.Name, LogLevel.Notice);
					}
				}
			}

			#endregion

			#region CRASH RECOVERY

			if (this.myFiles.Count > 0)
			{
				foreach (XGFile file in this.myFiles.ToArray())
				{
					// lets check if the directory is still on the harddisk
					if(!Directory.Exists(Settings.Instance.TempPath + file.TmpPath))
					{
						this.Log("Run() crash recovery directory " + file.TmpPath + " is missing ", LogLevel.Warning);
						this.myServerHandler.RemoveFile(file);
					}

					file.locked = new object();

					if (!file.Enabled)
					{
						bool complete = true;
						string tmpPath = Settings.Instance.TempPath + file.TmpPath;

						foreach (XGFilePart part in file.Children)
						{
							part.locked = new object();

							// check if the real file and the part is actual the same
							FileInfo info = new FileInfo(tmpPath + part.StartSize);
							if (info.Exists)
							{
								// TODO uhm, should we do smt here ?! maybe check the size and set the state to ready?
								if (part.CurrentSize != part.StartSize + info.Length)
								{
									this.Log("Run() crash recovery size mismatch of part " + part.StartSize + " from file " + file.TmpPath + " - db:" + part.CurrentSize + " real:" + info.Length, LogLevel.Warning);
									part.CurrentSize = part.StartSize + info.Length;
									complete = false;
								}
							}
							else
							{
								this.Log("Run() crash recovery part " + part.StartSize + " of file " + file.TmpPath + " is missing", LogLevel.Error);
								this.myServerHandler.RemovePart(file, part);
								complete = false;
							}

							// uhh, this is bad - close it and hope it works again
							if (part.PartState == FilePartState.Open)
							{
								part.PartState = FilePartState.Closed;
								complete = false;
							}
							// the file is closed, so do smt
							else
							{
								// check the file for safety
								if (part.IsChecked && part.PartState == FilePartState.Ready)
								{
									XGFilePart next = file.GetNextChild(part) as XGFilePart;
									if (next != null && !next.IsChecked && next.CurrentSize - next.StartSize >= Settings.Instance.FileRollbackCheck)
									{
										complete = false;
										try
										{
											this.Log("Run() crash recovery checking " + next.Name, LogLevel.Exception);
											FileStream fileStream = File.Open(this.myServerHandler.GetCompletePath(part), FileMode.Open, FileAccess.ReadWrite);
											BinaryReader fileReader = new BinaryReader(fileStream);
											// extract the needed refernce bytes
											fileStream.Seek(-Settings.Instance.FileRollbackCheck, SeekOrigin.End);
											byte[] bytes = fileReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
											fileReader.Close();

											this.myServerHandler.CheckNextReferenceBytes(part, bytes);
										}
										catch (Exception ex)
										{
											this.Log("Run() crash recovery: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
										}
									}
								}
								else
								{
									complete = false;
								}
							}
						}

						// check and maybee join the files if something happend the last run
						// for exaple the disk was full or the rights were not there
						if (complete && file.Children.Length > 0) { this.myServerHandler.CheckFile(file); }
					}
				}
			}

			#endregion
		}

		#region RUN STOP

		/// <summary>
		/// Run method - should be called via thread
		/// </summary>
		public void Start()
		{
			// start data saving routine
			this.mySaveDataThread = new Thread(new ThreadStart(SaveDataLoop));
			this.mySaveDataThread.Start();

			// connect to all servers which are enabled
			foreach (XGServer serv in this.myRootObject.Children)
			{
				serv.Parent = null;
				serv.Parent = this.myRootObject;
				serv.EnabledChangedEvent += new ObjectDelegate(serv_ObjectStateChangedEventHandler);
				if (serv.Enabled)
				{
					this.myServerHandler.ConnectServer(serv);
				}
			}
		}

		/// <summary>
		/// stop
		/// </summary>
		public void Stop()
		{
			// TODO stop server plugins
			foreach (XGServer serv in myRootObject.Children)
			{
				this.myServerHandler.DisconnectServer(serv);
			}
			this.mySaveDataThread.Abort();
		}

		#endregion

		#region SERVER PLUGIN

		public void AddServerPlugin(IServerPlugin aPlugin)
		{
			aPlugin.Start(this);
		}

		#endregion

		#region EVENTS

		/// <summary>
		/// Is called if the object state of a server is changed
		/// </summary>
		/// <param name="aObj"></param>
		private void serv_ObjectStateChangedEventHandler(XGObject aObj)
		{
			if (aObj.Enabled)
			{
				this.myServerHandler.ConnectServer(aObj as XGServer);
			}
		}

		#region ROOT OBJECT

		/// <summary>
		/// Is called if the root object added a server
		/// </summary>
		/// <param name="aObj"></param>
		/// <param name="aServer"></param>
		private void rootObject_ServerAddedEventHandler(RootObject aObj, XGServer aServer)
		{
			this.Log("rootObject_ServerAdded(" + aServer.Name + ")", LogLevel.Notice);
			this.myServerHandler.ConnectServer(aServer);

			// dispatch this info to the clients to!
			if (this.ObjectAddedEvent != null)
			{
				this.ObjectAddedEvent(aObj, aServer);
			}
		}

		/// <summary>
		/// Is called if the root object removed a server
		/// </summary>
		/// <param name="aObj"></param>
		/// <param name="aServer"></param>
		private void rootObject_ServerRemovedEventHandler(RootObject aObj, XGServer aServer)
		{
			this.Log("rootObject_ServerRemoved(" + aServer.Name + ")", LogLevel.Notice);
			aServer.Enabled = false;
			this.myServerHandler.DisconnectServer(aServer);

			// dispatch this info to the clients to!
			if (this.ObjectRemovedEvent != null)
			{
				this.ObjectRemovedEvent(aObj, aServer);
			}
		}

		#endregion

		#region SERVER

		/// <summary>
		/// Is called if the server object added an object
		/// </summary>
		/// <param name="aParentObj"></param>
		/// <param name="aObj"></param>
		private void myServerHandler_ObjectAddedEventHandler(XGObject aParentObj, XGObject aObj)
		{
			// we are just interested in files or fileparts
			if (aObj.GetType() == typeof(XGFile) || aObj.GetType() == typeof(XGFilePart))
			{
				// to save em now
				this.SaveFileDataNow();
			}

			if (this.ObjectAddedEvent != null)
			{
				this.ObjectAddedEvent(aParentObj, aObj);
			}
		}

		/// <summary>
		/// Is called if the server object changed an object
		/// </summary>
		/// <param name="aObj"></param>
		private void myServerHandler_ObjectChangedEventHandler(XGObject aObj)
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

			if (this.ObjectChangedEvent != null)
			{
				this.ObjectChangedEvent(aObj);
			}
		}

		/// <summary>
		/// Is called if the server object removed an object
		/// </summary>
		/// <param name="aParentObj"></param>
		/// <param name="aObj"></param>
		private void myServerHandler_ObjectRemovedEventHandler(XGObject aParentObj, XGObject aObj)
		{
			// we are just interested in files or fileparts
			if (aObj.GetType() == typeof(XGFile) || aObj.GetType() == typeof(XGFilePart))
			{
				// to save em now
				this.SaveFileDataNow();
			}

			if (this.ObjectRemovedEvent != null)
			{
				this.ObjectRemovedEvent(aParentObj, aObj);
			}
		}

		/// <summary>
		/// Is called if the server object found a parse error
		/// </summary>
		/// <param name="aData"></param>
		private void myServerHandler_ParsingErrorEventHandler(string aData)
		{
			lock (this)
			{
				try
				{
					StreamWriter sw = new StreamWriter(File.OpenWrite(Settings.Instance.ParsingErrorFile));
					sw.BaseStream.Seek(0, SeekOrigin.End);
					sw.WriteLine(aData.Normalize());
					sw.Close();
				}
				catch (Exception) { }
			}
		}

		#endregion

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
				Stream streamWrite = File.Create(aFile + ".new");
				this.myFormatter.Serialize(streamWrite, aObj);
				streamWrite.Close();
				try { File.Delete(aFile + ".bak"); }
				catch (Exception) { };
				try { File.Move(aFile, aFile + ".bak"); }
				catch (Exception) { };
				File.Move(aFile + ".new", aFile);
				this.Log("Save(" + aFile + ")", LogLevel.Info);
			}
			catch (Exception ex)
			{
				this.Log("Save(" + aFile + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
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
			if (File.Exists(aFile))
			{
				try
				{
					Stream streamRead = File.OpenRead(aFile);
					obj = this.myFormatter.Deserialize(streamRead);
					streamRead.Close();
					this.Log("Load(" + aFile + ")", LogLevel.Info);
				}
				catch (Exception ex)
				{
					this.Log("Load(" + aFile + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					// try to load the backup
					try
					{
						Stream streamRead = File.OpenRead(aFile + ".bak");
						obj = this.myFormatter.Deserialize(streamRead);
						streamRead.Close();
						this.Log("Load(" + aFile + ".bak)", LogLevel.Info);
					}
					catch (Exception)
					{
						this.Log("Load(" + aFile + ".bak) : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
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
					RootObject clone = XGHelper.CloneObject(this.myRootObject, false);
					this.Save(clone, Settings.Instance.DataBinary);
					clone = null;
				}

				// File Data
				if (this.isSaveFile)
				{
					lock (this.mySaveFileLock)
					{
						this.Save(this.myFiles, Settings.Instance.FilesBinary);
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
			lock (this.mySaveFileLock)
			{
				this.Save(this.myFiles, Settings.Instance.FilesBinary);
				this.isSaveFile = false;
			}
		}

		#endregion

		#region CLIENT REQUEST HANDLER

		#region SERVER

		public void AddServer(string aString)
		{
			this.myRootObject.AddServer(aString);
		}

		public void RemoveServer(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				this.myRootObject.RemoveServer(tObj as XGServer);
			}
		}

		#endregion

		#region CHANNEL

		public void AddChannel(Guid aGuid, string aString)
		{
			XGObject tObj = this.myRootObject.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				(tObj as XGServer).AddChannel(aString);
			}
		}

		public void RemoveChannel(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				XGChannel tChan = tObj as XGChannel;
				tChan.Parent.RemoveChannel(tChan);
			}
		}

		#endregion

		#region OBJECT

		public void ActivateObject(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = true;
				myServerHandler_ObjectChangedEventHandler(tObj);
			}
		}

		public void DeactivateObject(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = false;
				myServerHandler_ObjectChangedEventHandler(tObj);
			}
			else
			{
				foreach (XGFile tFile in this.myFiles.ToArray())
				{
					if (tFile.Guid == aGuid)
					{
						this.myServerHandler.RemoveFile(tFile);
						break;
					}
				}
			}
		}

		#endregion

		#region SEARCH

		#region PACKET

		public List<XGObject> SearchPacket(string aString)
		{
			return this.SearchPacket(aString, null);
		}

		public List<XGObject> SearchPacket(string aString, Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			string[] searchList = aString.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (XGServer tServ in this.myRootObject.Children)
			{
				foreach (XGChannel tChan in tServ.Children)
				{
					foreach (XGBot tBot in tChan.Children)
					{
						foreach (XGPacket tPack in tBot.Children)
						{
							if (tPack.Name != "")
							{
								string name = tPack.Name.ToLower();

								bool add = true;
								for (int i = 0; i < searchList.Length; i++)
								{
									if (!name.Contains(searchList[i]))
									{
										add = false;
										break;
									}
								}
								if (add)
								{
									tList.Add(tPack);
								}
							}
						}
					}
				}
			}
			if (aComp != null) { tList.Sort(aComp); }
			return tList;
		}

		public List<XGObject> SearchPacketTime(string aString)
		{
			return this.SearchPacketTime(aString, null);
		}

		public List<XGObject> SearchPacketTime(string aString, Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			string[] search = aString.Split('-');
			double start = Double.Parse(search[0]);
			double stop = Double.Parse(search[1]);
			DateTime init = new DateTime(1, 1, 1);
			DateTime now = DateTime.Now;
			foreach (XGServer tServ in this.myRootObject.Children)
			{
				foreach (XGChannel tChan in tServ.Children)
				{
					foreach (XGBot tBot in tChan.Children)
					{
						foreach (XGPacket tPack in tBot.Children)
						{
							if (tPack.LastUpdated != init)
							{
								double diff = (now - tPack.LastUpdated).TotalMilliseconds;
								if (start <= diff && stop >= diff)
								{
									tList.Add(tPack);
								}
							}
						}
					}
				}
			}
			if (aComp != null) { tList.Sort(aComp); }
			return tList;
		}

		public List<XGObject> SearchPacketActiveDownloads()
		{
			return this.SearchPacketActiveDownloads(null);
		}

		public List<XGObject> SearchPacketActiveDownloads(Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			foreach (XGServer tServ in this.myRootObject.Children)
			{
				foreach (XGChannel tChan in tServ.Children)
				{
					foreach (XGBot tBot in tChan.Children)
					{
						foreach (XGPacket tPack in tBot.Children)
						{
							if (tPack.Connected)
							{
								tList.Add(tPack);
							}
						}
					}
				}
			}
			if (aComp != null) { tList.Sort(aComp); }
			return tList;
		}

		public List<XGObject> SearchPacketsEnabled()
		{
			return this.SearchPacketsEnabled(null);
		}

		public List<XGObject> SearchPacketsEnabled(Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			foreach (XGServer tServ in this.myRootObject.Children)
			{
				foreach (XGChannel tChan in tServ.Children)
				{
					foreach (XGBot tBot in tChan.Children)
					{
						foreach (XGPacket tPack in tBot.Children)
						{
							if (tPack.Enabled)
							{
								tList.Add(tPack);
							}
						}
					}
				}
			}
			if (aComp != null) { tList.Sort(aComp); }
			return tList;
		}

		#endregion

		#region BOT

		public List<XGObject> SearchBot(string aString)
		{
			return this.SearchBot(aString, null);
		}

		public List<XGObject> SearchBot(string aString, Comparison<XGObject> aComp)
		{
			return this.GetBots2Packets(this.SearchPacket(aString, aComp), aComp);
		}

		public List<XGObject> SearchBotTime(string aString)
		{
			return this.SearchBotTime(aString, null);
		}

		public List<XGObject> SearchBotTime(string aString, Comparison<XGObject> aComp)
		{
			return this.GetBots2Packets(this.SearchPacketTime(aString, aComp), aComp);
		}

		public List<XGObject> SearchBotActiveDownloads()
		{
			return this.SearchBotActiveDownloads(null);
		}

		public List<XGObject> SearchBotActiveDownloads(Comparison<XGObject> aComp)
		{
			return this.GetBots2Packets(this.SearchPacketActiveDownloads(aComp), aComp);
		}

		public List<XGObject> SearchBotsEnabled()
		{
			return this.SearchBotsEnabled(null);
		}

		public List<XGObject> SearchBotsEnabled(Comparison<XGObject> aComp)
		{
			return this.GetBots2Packets(this.SearchPacketsEnabled(aComp), aComp);
		}

		private List<XGObject> GetBots2Packets(List<XGObject> aList, Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			foreach (XGPacket tPack in aList)
			{
				if (tList.Contains(tPack.Parent)) { continue; }
				tList.Add(tPack.Parent);
			}
			if (aComp != null) { tList.Sort(aComp); }
			return tList;
		}

		#endregion

		#region SPECIAL

		public void AddSearch(string aSearch)
		{
			this.mySearches.Add(aSearch);

			lock (this.mySaveSearchLock)
			{
				this.Save(this.mySearches, Settings.Instance.SearchesBinary);
			}
		}

		public void RemoveSearch(string aSearch)
		{
			this.mySearches.Remove(aSearch);

			lock (this.mySaveSearchLock)
			{
				this.Save(this.mySearches, Settings.Instance.SearchesBinary);
			}
		}

		#endregion

		#endregion

		#region GET

		public List<XGObject> GetServersChannels()
		{
			List<XGObject> tList = new List<XGObject>();
			foreach (XGServer tServ in this.myRootObject.Children)
			{
				tList.Add(tServ);
				foreach (XGChannel tChan in tServ.Children)
				{
					tList.Add(tChan);
				}
			}
			return tList;
		}

		public List<XGObject> GetActivePackets()
		{
			List<XGObject> tList = new List<XGObject>();
			foreach (XGServer tServ in this.myRootObject.Children)
			{
				foreach (XGChannel tChan in tServ.Children)
				{
					foreach (XGBot tBot in tChan.Children)
					{
						foreach (XGPacket tPack in tBot.Children)
						{
							if (tPack.Enabled)
							{
								tList.Add(tPack);
							}
						}
					}
				}
			}
			return tList;
		}

		public XGFilePart GetFilePart4Packet(XGPacket aPacket)
		{
			foreach (XGFile tFile in this.myFiles)
			{
				foreach (XGFilePart tPart in tFile.Children)
				{
					if (tPart.Packet == aPacket)
					{
						return tPart;
					}
				}
			}
			return null;
		}

		public XGFilePart GetFilePart4Bot(XGBot aBot)
		{
			XGFilePart tPart = null;
			foreach (XGPacket tPack in aBot.Children)
			{
				tPart = this.GetFilePart4Packet(tPack);
				if (tPart != null)
				{
					break;
				}
			}
			return tPart;
		}

		public List<XGObject> GetFiles()
		{
			return this.GetFiles(null);
		}

		public List<string> GetSearches()
		{
			return this.mySearches;
		}

		public List<XGObject> GetFiles(Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			foreach (XGFile tFile in this.myFiles.ToArray())
			{
				tList.Add(tFile);
				foreach (XGFilePart tPart in tFile.Children)
				{
					tList.Add(tPart);
				}
			}
			if (aComp != null) { tList.Sort(aComp); }
			return tList;
		}

		public XGObject GetObject(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				return tObj;
			}
			else
			{
				foreach (XGFile tFile in this.myFiles.ToArray())
				{
					if (tFile.Guid == aGuid)
					{
						return tFile;
					}
				}
			}
			return null;
		}

		public List<XGObject> GetChildrenFromObject(Guid aGuid)
		{
			return this.GetChildrenFromObject(aGuid, null);
		}

		public List<XGObject> GetChildrenFromObject(Guid aGuid, Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			XGObject tObj = this.myRootObject.GetChildByGuid(aGuid);
			if (tObj != null)
			{
				foreach (XGObject tChild in tObj.Children)
				{
					tList.Add(tChild);
				}
			}
			else
			{
				foreach (XGFile tFile in this.myFiles.ToArray())
				{
					if (tFile.Guid == aGuid)
					{
						foreach (XGFilePart tPart in tFile.Children)
						{
							tList.Add(tPart);
						}
						break;
					}
				}
			}
			if (aComp != null) { tList.Sort(aComp); }
			return tList;
		}

		#endregion

		#endregion

		#region LOG

		/// <summary>
		/// Calls  XGHelper.Log()
		/// </summary>
		/// <param name="aData"></param>
		/// <param name="aLevel"></param>
		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("ServerRunner." + aData, aLevel);
		}

		#endregion
	}
}
