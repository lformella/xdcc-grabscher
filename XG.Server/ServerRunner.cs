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
using System.Linq;
using log4net;
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

		private static readonly ILog myLog = LogManager.GetLogger(typeof(ServerRunner));

		private ServerHandler myServerHandler;

		private RootObject myRootObject = null;
		public RootObject RootObject
		{
			get { return this.myRootObject; }
		}

		private List<XGFile> myFiles;
		public List<XGFile> Files
		{
			get { return this.myFiles; }
		}

		private List<string> mySearches;
		public List<string> Searches
		{
			get { return this.mySearches; }
		}

		#endregion

		#region EVENTS

		public event ObjectDelegate ObjectChangedEvent;
		public event ObjectObjectDelegate ObjectAddedEvent;
		public event ObjectObjectDelegate ObjectRemovedEvent;

		public event DataTextDelegate SearchAddedEvent;
		public event DataTextDelegate SearchRemovedEvent;

		#endregion

		public ServerRunner()
		{
			// the one and only root object
			this.myRootObject = new RootObject();
		}

		#region RUN STOP

		/// <summary>
		/// Run method - should be called via thread
		/// </summary>
		public void Start()
		{
			this.myRootObject.ServerAddedEvent += new RootServerDelegate(rootObject_ServerAddedEventHandler);
			this.myRootObject.ServerRemovedEvent += new RootServerDelegate(rootObject_ServerRemovedEventHandler);

			#region SERVERHANDLER INIT

			this.myServerHandler = new ServerHandler(this.myFiles);
			this.myServerHandler.ParsingErrorEvent += new DataTextDelegate(myServerHandler_ParsingErrorEventHandler);

			this.myServerHandler.ObjectAddedEvent += new ObjectObjectDelegate(myServerHandler_ObjectAddedEventHandler);
			this.myServerHandler.ObjectChangedEvent += new ObjectDelegate(myServerHandler_ObjectChangedEventHandler);
			this.myServerHandler.ObjectRemovedEvent += new ObjectObjectDelegate(myServerHandler_ObjectRemovedEventHandler);

			#endregion

			#region DUPE CHECK

			// check if there are some dupes in our database
			foreach (XGServer serv in this.myRootObject.Servers)
			{
				foreach (XGServer s in this.myRootObject.Servers)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						myLog.Error("Run() removing dupe server " + s.Name);
						this.myRootObject.RemoveServer(s);

						// dispatch this info to the clients to!
						if (this.ObjectRemovedEvent != null)
						{
							this.ObjectRemovedEvent(myRootObject, s);
						}
					}
				}

				foreach (XGChannel chan in serv.Channels)
				{
					foreach (XGChannel c in serv.Channels)
					{
						if (c.Name == chan.Name && c.Guid != chan.Guid)
						{
							myLog.Error("Run() removing dupe channel " + c.Name);
							serv.RemoveChannel(c);

							// dispatch this info to the clients to!
							if (this.ObjectRemovedEvent != null)
							{
								this.ObjectRemovedEvent(serv, c);
							}
						}
					}

					foreach (XGBot bot in chan.Bots)
					{
						foreach (XGBot b in chan.Bots)
						{
							if (b.Name == bot.Name && b.Guid != bot.Guid)
							{
								myLog.Error("Run() removing dupe bot " + b.Name);
								chan.RemoveBot(b);

								// dispatch this info to the clients to!
								if (this.ObjectRemovedEvent != null)
								{
									this.ObjectRemovedEvent(chan, b);
								}
							}
						}

						foreach (XGPacket pack in bot.Packets)
						{
							foreach (XGPacket p in bot.Packets)
							{
								if (p.Id == pack.Id && p.Guid != pack.Guid)
								{
									myLog.Error("Run() removing dupe Packet " + p.Name);
									bot.RemovePacket(p);

									// dispatch this info to the clients to!
									if (this.ObjectRemovedEvent != null)
									{
										this.ObjectRemovedEvent(bot, p);
									}
								}
							}
						}
					}
				}
			}

			#endregion

			#region RESET

			// reset all objects if the server crashed
			foreach (XGServer serv in this.myRootObject.Servers)
			{
				serv.Connected = false;
				serv.ErrorCode = SocketErrorCode.None;

				foreach (XGChannel chan in serv.Channels)
				{
					chan.Connected = false;
					chan.ErrorCode = 0;

					foreach (XGBot bot in chan.Bots)
					{
						bot.Connected = false;
						bot.BotState = BotState.Idle;

						foreach (XGPacket pack in bot.Packets)
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
						myLog.Info("Run() removing ready file " + file.Name);
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
						myLog.Warn("Run() crash recovery directory " + file.TmpPath + " is missing ");
						this.myServerHandler.RemoveFile(file);
						continue;
					}

					file.locked = new object();

					if (!file.Enabled)
					{
						bool complete = true;
						string tmpPath = Settings.Instance.TempPath + file.TmpPath;

						foreach (XGFilePart part in file.Parts)
						{
							part.locked = new object();

							// check if the real file and the part is actual the same
							FileInfo info = new FileInfo(tmpPath + part.StartSize);
							if (info.Exists)
							{
								// TODO uhm, should we do smt here ?! maybe check the size and set the state to ready?
								if (part.CurrentSize != part.StartSize + info.Length)
								{
									myLog.Warn("Run() crash recovery size mismatch of part " + part.StartSize + " from file " + file.TmpPath + " - db:" + part.CurrentSize + " real:" + info.Length);
									part.CurrentSize = part.StartSize + info.Length;
									complete = false;
								}
							}
							else
							{
								myLog.Error("Run() crash recovery part " + part.StartSize + " of file " + file.TmpPath + " is missing");
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
											myLog.Fatal("Run() crash recovery checking " + next.Name);
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
											myLog.Fatal("Run() crash recovery", ex);
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
						if (complete && file.Parts.Count() > 0) { this.myServerHandler.CheckFile(file); }
					}
				}
			}

			#endregion

			// connect to all servers which are enabled
			foreach (XGServer serv in this.myRootObject.Servers)
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
			this.myRootObject.ServerAddedEvent -= new RootServerDelegate(rootObject_ServerAddedEventHandler);
			this.myRootObject.ServerRemovedEvent -= new RootServerDelegate(rootObject_ServerRemovedEventHandler);

			// TODO stop server plugins
			foreach (XGServer serv in myRootObject.Servers)
			{
				this.myServerHandler.DisconnectServer(serv);
			}
		}

		#endregion

		#region SERVER BACKEND PLUGIN

		public void AddServerBackendPlugin(IServerBackendPlugin aPlugin)
		{
			this.myRootObject = aPlugin.GetRootObject();
			this.myFiles = aPlugin.GetFiles();
			this.mySearches = aPlugin.GetSearches();

			this.AddServerPlugin(aPlugin);
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
			myLog.Info("rootObject_ServerAdded(" + aServer.Name + ")");
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
			myLog.Info("rootObject_ServerRemoved(" + aServer.Name + ")");
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

		public void AddSearch(string aSearch)
		{
			this.mySearches.Add(aSearch);

			if (this.SearchAddedEvent != null)
			{
				this.SearchAddedEvent(aSearch);
			}
		}

		public void RemoveSearch(string aSearch)
		{
			this.mySearches.Remove(aSearch);

			if (this.SearchRemovedEvent != null)
			{
				this.SearchRemovedEvent(aSearch);
			}
		}

		#endregion
		
		#endregion
	}
}
