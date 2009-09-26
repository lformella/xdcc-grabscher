using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using XG.Core;

namespace XG.Server
{
	public class ServerRunner
	{
		private ServerHandler myServerHandler;
		private RootObject myRootObject;
		public Guid RootGuid
		{
			get { return this.myRootObject != null ? this.myRootObject.Guid : Guid.Empty; }
		}
		
		private List<XGFile> myFiles;

		private BinaryFormatter myFormatter = new BinaryFormatter();

		private Thread mySaveBinThread;
		private Thread mySaveFileThread;

		private bool isSaveFile = false;
		private object mySaveFileLock = new object();

		#region DELEGATES

		public event ObjectDelegate ObjectChangedEvent;
		public event ObjectObjectDelegate ObjectAddedEvent;
		public event ObjectObjectDelegate ObjectRemovedEvent;

		#endregion

		#region RUN STOP RESTART

		/// <summary>
		/// Run method - should be called via thread
		/// </summary>
		public void Start()
		{
			// the one and only root object
			this.myRootObject = (RootObject)this.Load(Settings.Instance.DataBinary);
			if (this.myRootObject == null) { this.myRootObject = new RootObject(); }
			this.myRootObject.ServerAddedEvent += new RootServerDelegate(rootObject_ServerAddedEventHandler);
			this.myRootObject.ServerRemovedEvent += new RootServerDelegate(rootObject_ServerRemovedEventHandler);

			// the file data
			this.myFiles = (List<XGFile>)this.Load(Settings.Instance.FilesBinary);
			if (this.myFiles == null) { this.myFiles = new List<XGFile>(); }

			#region SERVER INIT

			this.myServerHandler = new ServerHandler( this.myFiles );
			this.myServerHandler.ParsingErrorEvent += new DataTextDelegate( myServerHandler_ParsingErrorEventHandler );

			this.myServerHandler.ObjectAddedEvent += new ObjectObjectDelegate( myServerHandler_ObjectAddedEventHandler );
			this.myServerHandler.ObjectChangedEvent += new ObjectDelegate( myServerHandler_ObjectChangedEventHandler );
			this.myServerHandler.ObjectRemovedEvent += new ObjectObjectDelegate( myServerHandler_ObjectRemovedEventHandler );

			#endregion

			#region DUPE CHECK

			// check if there are some dupes in our database			
			foreach(XGServer serv in this.myRootObject.Children)
			{
				foreach(XGServer s in this.myRootObject.Children)
				{
					if(s.Name == serv.Name && s.Guid != serv.Guid)
					{
						this.Log("Run() removing dupe server " + s.Name, LogLevel.Error);
						this.myRootObject.removeServer(s);
					}
				}
			
				foreach(XGChannel chan in serv.Children)
				{
					foreach(XGChannel c in serv.Children)
					{
						if(c.Name == chan.Name && c.Guid != chan.Guid)
						{
							this.Log("Run() removing dupe channel " + c.Name, LogLevel.Error);
							serv.removeChannel(c);
						}
					}
			
					foreach(XGBot bot in chan.Children)
					{
						foreach(XGBot b in chan.Children)
						{
							if(b.Name == bot.Name && b.Guid != bot.Guid)
							{
								this.Log("Run() removing dupe bot " + b.Name, LogLevel.Error);
								chan.removeBot(b);
							}
						}
			
						foreach(XGPacket pack in bot.Children)
						{
							foreach(XGPacket p in bot.Children)
							{
								if(p.Id == pack.Id && p.Guid != pack.Guid)
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

			#region CLEAR OLD DL
			
			if(this.myFiles.Count > 0 && Settings.Instance.ClearReadyDownloads)
			{
				foreach(XGFile file in this.myFiles.ToArray())
				{
					bool remove = false;
					foreach(XGFilePart part in file.Children)
					{
						if(part.PartState == FilePartState.Ready)
						{
							remove = true;
							break;
						}
					}
					if(remove)
					{
						this.myFiles.Remove(file);
						this.Log("Run() removing ready file " + file.Name, LogLevel.Notice);
					}
				}
			}
			
			#endregion
			
			#region CRASH RECOVERY

			if(this.myFiles.Count > 0)
			{
				foreach(XGFile file in this.myFiles.ToArray())
				{
					bool complete = true;
					string tmpPath = Settings.Instance.TempPath + file.TmpPath;

					foreach(XGFilePart part in file.Children)
					{
						// check if the real file and the part is actual the same
						if(!file.Enabled)
						{
							FileInfo info = new FileInfo(tmpPath + part.StartSize);
							if(info.Exists)
							{
								// TODO uhm, should we do smt here ?! maybee check the size...
							}
							else
							{
								this.Log("Run() crash recovery part " + part.StartSize + " of file " + file.TmpPath + " is missing", LogLevel.Error);
								this.myServerHandler.RemovePart(file, part);
							}
						}

						// uhh, this is bad - close it and hope it works again
						if(part.PartState == FilePartState.Open) { part.PartState = FilePartState.Closed; }
						// the file is closed, so do smt
						else
						{
							// check the file for safety
							if(part.IsChecked && part.PartState == FilePartState.Ready)
							{
								XGFilePart next = file.getNextChild(part) as XGFilePart;
								if(next != null && !next.IsChecked && next.CurrentSize - next.StartSize >= Settings.Instance.FileRollbackCheck)
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
						}
					}

					// check and maybee join the files if something happend the last run
					// for exaple the disk was full or the rights were not there
					if(!file.Enabled && complete && file.Children.Length > 0) { this.myServerHandler.CheckFile(file); }
				}
			}

			#endregion

			#region THREADS

			// start saving routine
			this.mySaveBinThread = new Thread(new ThreadStart(SaveIrcData));
			this.mySaveBinThread.Start();

			// start saving routine
			this.mySaveFileThread = new Thread(new ThreadStart(SaveFileData));
			this.mySaveFileThread.Start();

			#endregion

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
			foreach (XGServer serv in myRootObject.Children)
			{
				this.myServerHandler.DisconnectServer(serv);
			}
			this.mySaveBinThread.Abort();
			this.mySaveFileThread.Abort();
		}

		/// <summary>
		/// restart
		/// </summary>
		public void Restart()
		{
			// TODO do something usefull
		}

		#endregion

		#region EVENTS

		/// <summary>
		/// Is called if the object state of a server is changed
		/// </summary>
		/// <param name="aObj"></param>
		private void serv_ObjectStateChangedEventHandler(XGObject aObj)
		{
			if(aObj.Enabled)
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
			if(aObj.GetType() == typeof(XGFile) || aObj.GetType() == typeof(XGFilePart))
			{
				this.SaveFileDataNow();
			}
			
			this.ObjectAddedEvent(aParentObj, aObj);
		}

		/// <summary>
		/// Is called if the server object changed an object
		/// </summary>
		/// <param name="aObj"></param>
		private void myServerHandler_ObjectChangedEventHandler(XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGFile))
			{
				this.SaveFileDataNow();
			}
			if(aObj.GetType() == typeof(XGFilePart))
			{
				XGFilePart part = aObj as XGFilePart;
				// if this change is lost, the data might be corrupt, so save it NOW
				if(part.PartState != FilePartState.Open)
				{
					this.SaveFileDataNow();
				}
				// the data saving can be scheduled
				else
				{
					this.isSaveFile = true;
				}
			}
			
			this.ObjectChangedEvent(aObj);
		}

		/// <summary>
		/// Is called if the server object removed an object
		/// </summary>
		/// <param name="aParentObj"></param>
		/// <param name="aObj"></param>
		private void myServerHandler_ObjectRemovedEventHandler(XGObject aParentObj, XGObject aObj)
		{
			// we are just interested in files or fileparts
			if(aObj.GetType() == typeof(XGFile) || aObj.GetType() == typeof(XGFilePart))
			{
				this.SaveFileDataNow();
			}
			
			this.ObjectRemovedEvent(aParentObj, aObj);
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
				catch (Exception) {};
				try { File.Move(aFile, aFile + ".bak"); }
				catch (Exception) {};
				File.Move(aFile + ".new", aFile);
				this.Log("Save(" + aFile + ")", LogLevel.Info);
			}
			catch (Exception ex)
			{
				this.Log("Save(" + aFile + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
		}

		/// <summary>
		/// Schedules the saving of the IrcData 
		/// </summary>
		private void SaveIrcData ()
		{
			while (true) 
			{
				Thread.Sleep ((int)Settings.Instance.BackupDataTime);
				this.Save (XGHelper.CloneObject (this.myRootObject, false), "./xg.xml");
			}
		}

		/// <summary>
		/// Schedules the saving of the FileData 
		/// </summary>
		private void SaveFileData()
		{
			while(true)
			{
				if(this.isSaveFile)
				{
					lock(this.mySaveFileLock)
					{
						this.Save(this.myFiles, Settings.Instance.FilesBinary);
						this.isSaveFile = false;
					}
				}
				Thread.Sleep(5000);
			}
		}

		/// <summary>
		/// Save the FileData right now 
		/// </summary>
		private void SaveFileDataNow()
		{
			lock(this.mySaveFileLock)
			{
				this.Save(this.myFiles, Settings.Instance.FilesBinary);
				this.isSaveFile = false;
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

		#endregion

		#region CLIENT REQUEST HANDLER
		
		#region SERVER
		
		public void AddServer(string aString)
		{
			this.myRootObject.addServer(aString);
		}

		public void RemoveServer(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.getChildByGuid(aGuid);
			if (tObj != null)
			{
				this.myRootObject.removeServer(tObj as XGServer);
			}
		}

		#endregion

		#region CHANNEL

		public void AddChannel(Guid aGuid, string aString)
		{
			XGObject tObj = this.myRootObject.getChildByGuid(aGuid);
			if (tObj != null)
			{
				(tObj as XGServer).addChannel(aString);
			}
		}

		public void RemoveChannel(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.getChildByGuid(aGuid);
			if (tObj != null)
			{
				XGChannel tChan = tObj as XGChannel;
				tChan.Parent.removeChannel(tChan);
			}
		}

		#endregion

		#region OBJECT

		public void ActivateObject(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.getChildByGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = true;
				myServerHandler_ObjectChangedEventHandler(tObj);
			}
		}

		public void DeactivateObject(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.getChildByGuid(aGuid);
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

		public XGObject[] SearchPacket(string aString)
		{
			return this.SearchPacket(aString, null);
		}

		public XGObject[] SearchPacket(string aString, Comparison<XGObject> aComp)
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
			if(aComp != null)
			{
				tList.Sort(aComp);
			}
			return tList.ToArray();
		}

		public XGObject[] SearchPacketTime(string aString)
		{
			return this.SearchPacketTime(aString, null);
		}

		public XGObject[] SearchPacketTime(string aString, Comparison<XGObject> aComp)
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
			if(aComp != null)
			{
				tList.Sort(aComp);
			}
			return tList.ToArray();
		}

		public XGObject[] SearchBot(string aString)
		{
			return this.SearchBot(aString, null);
		}

		public XGObject[] SearchBot(string aString, Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			
			XGObject[] tPackets = this.SearchPacket(aString);
			foreach(XGPacket tPack in tPackets)
			{
				if(tList.Contains(tPack.Parent))
				{
					continue;
				}
				tList.Add(tPack.Parent);
			}			
			
			if(aComp != null)
			{
				tList.Sort(aComp);
			}
			return tList.ToArray();
		}

		public XGObject[] SearchBotTime(string aString)
		{
			return this.SearchBotTime(aString, null);
		}

		public XGObject[] SearchBotTime(string aString, Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();

			XGObject[] tPackets = this.SearchPacketTime(aString);
			foreach(XGPacket tPack in tPackets)
			{
				if(tList.Contains(tPack.Parent))
				{
					continue;
				}
				tList.Add(tPack.Parent);
			}

			if(aComp != null)
			{
				tList.Sort(aComp);
			}
			return tList.ToArray();
		}

		#endregion

		#region GET

		public XGObject[] GetServersChannels()
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
			return tList.ToArray();
		}

		public XGObject[] GetActivePackets()
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
			return tList.ToArray();
		}

		public XGObject[] GetFiles()
		{
			return this.GetFiles(null);
		}

		public XGObject[] GetFiles(Comparison<XGObject> aComp)
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
			if(aComp != null)
			{
				tList.Sort(aComp);
			}
			return tList.ToArray();
		}

		public XGObject GetObject(Guid aGuid)
		{
			XGObject tObj = this.myRootObject.getChildByGuid(aGuid);
			if (tObj != null)
			{
				return tObj;
			}
			else
			{
				foreach(XGFile tFile in this.myFiles.ToArray())
				{
					if(tFile.Guid == aGuid)
					{
						return tFile;
					}
				}
			}
			return null;
		}

		public XGObject[] GetChildrenFromObject(Guid aGuid)
		{
			return this.GetChildrenFromObject(aGuid);
		}

		public XGObject[] GetChildrenFromObject(Guid aGuid, Comparison<XGObject> aComp)
		{
			List<XGObject> tList = new List<XGObject>();
			XGObject tObj = this.myRootObject.getChildByGuid(aGuid);
			if (tObj != null)
			{
				foreach (XGObject tChild in tObj.Children)
				{
					tList.Add(tChild);
				}
			}
			else
			{
				foreach(XGFile tFile in this.myFiles.ToArray())
				{
					if(tFile.Guid == aGuid)
					{
						foreach(XGFilePart tPart in tFile.Children)
						{
							tList.Add(tPart);
						}
						break;
					}
				}
			}
			if(aComp != null)
			{
				tList.Sort(aComp);
			}
			return tList.ToArray();
		}

		#endregion
		
		#endregion
		
		#region LOG

		/// <summary>
		/// Calls XGGelper.Log()
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