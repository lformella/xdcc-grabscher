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
using XG.Server.Helper;
using XG.Server.Plugin.Backend;
using XG.Server.Plugin.General;

namespace XG.Server
{
	/// <summary>
	/// This class holds all information about the files and the irc objects
	/// it does the following things
	/// - loading and saving the object to a file
	/// - calling the ServerHandler to connect to or disconnect from an irc server
	/// - communicate with XG.Server.* plugins, meaning the client
	/// </summary>
	public class MainInstance
	{
		#region VARIABLES

		private static readonly ILog log = LogManager.GetLogger(typeof(MainInstance));

		private IrcParser ircParser;
		private ServerHandler serverHandler;

		private XG.Core.Repository.Object objectRepository;
		public XG.Core.Repository.Object ObjectRepository
		{
			get { return this.objectRepository; }
			private set
			{
				if(this.objectRepository != null)
				{
					this.objectRepository.ChildAddedEvent -= new ObjectObjectDelegate(ObjectRepository_ChildAddedEventHandler);
					this.objectRepository.ChildRemovedEvent -= new ObjectObjectDelegate(ObjectRepository_ChildRemovedEventHandler);
					this.objectRepository.EnabledChangedEvent -= new ObjectDelegate(ObjectRepository_EnabledChangedEventHandler);
				}
				this.objectRepository = value;
				if(this.objectRepository != null)
				{
					this.objectRepository.ChildAddedEvent += new ObjectObjectDelegate(ObjectRepository_ChildAddedEventHandler);
					this.objectRepository.ChildRemovedEvent += new ObjectObjectDelegate(ObjectRepository_ChildRemovedEventHandler);
					this.objectRepository.EnabledChangedEvent += new ObjectDelegate(ObjectRepository_EnabledChangedEventHandler);
				}
			}
		}

		private XG.Core.Repository.File fileRepository;
		public XG.Core.Repository.File FileRepository
		{
			get { return this.fileRepository; }
			private set { this.fileRepository = value; }
		}

		private List<string> searches;
		public List<string> Searches
		{
			get { return this.searches; }
			private set { this.searches = value; }
		}

		#endregion

		#region EVENTS

		public event DataTextDelegate SearchAddedEvent;
		public event DataTextDelegate SearchRemovedEvent;

		#endregion

		#region RUN STOP

		/// <summary>
		/// Run method - should be called via thread
		/// </summary>
		public void Start()
		{
			this.ircParser = new IrcParser();
			this.ircParser.ParsingErrorEvent += new DataTextDelegate(IrcParser_ParsingErrorEventHandler);

			this.serverHandler = new ServerHandler();
			this.serverHandler.FileRepository = this.fileRepository;
			this.serverHandler.IrcParser = this.ircParser;

			#region DUPE CHECK

			// check if there are some dupes in our database
			foreach (XGServer serv in this.objectRepository.Servers)
			{
				foreach (XGServer s in this.objectRepository.Servers)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						log.Error("Run() removing dupe server " + s.Name);
						this.objectRepository.RemoveServer(s);
					}
				}

				foreach (XGChannel chan in serv.Channels)
				{
					foreach (XGChannel c in serv.Channels)
					{
						if (c.Name == chan.Name && c.Guid != chan.Guid)
						{
							log.Error("Run() removing dupe channel " + c.Name);
							serv.RemoveChannel(c);
						}
					}

					foreach (XGBot bot in chan.Bots)
					{
						foreach (XGBot b in chan.Bots)
						{
							if (b.Name == bot.Name && b.Guid != bot.Guid)
							{
								log.Error("Run() removing dupe bot " + b.Name);
								chan.RemoveBot(b);
							}
						}

						foreach (XGPacket pack in bot.Packets)
						{
							foreach (XGPacket p in bot.Packets)
							{
								if (p.Id == pack.Id && p.Guid != pack.Guid)
								{
									log.Error("Run() removing dupe Packet " + p.Name);
									bot.RemovePacket(p);
								}
							}
						}
					}
				}
			}

			#endregion

			#region RESET

			// reset all objects if the server crashed
			foreach (XGServer serv in this.objectRepository.Servers)
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

			if (this.fileRepository.Files.Count() > 0 && Settings.Instance.ClearReadyDownloads)
			{
				foreach (XGFile file in this.fileRepository.Files)
				{
					if (file.Enabled)
					{
						this.fileRepository.RemoveFile(file);
						log.Info("Run() removing ready file " + file.Name);
					}
				}
			}

			#endregion

			#region CRASH RECOVERY

			if (this.fileRepository.Files.Count() > 0)
			{
				foreach (XGFile file in this.fileRepository.Files)
				{
					// lets check if the directory is still on the harddisk
					if(!Directory.Exists(Settings.Instance.TempPath + file.TmpPath))
					{
						log.Warn("Run() crash recovery directory " + file.TmpPath + " is missing ");
						this.serverHandler.RemoveFile(file);
						continue;
					}

					file.Locked = new object();

					if (!file.Enabled)
					{
						bool complete = true;
						string tmpPath = Settings.Instance.TempPath + file.TmpPath;

						foreach (XGFilePart part in file.Parts)
						{
							// check if the real file and the part is actual the same
							FileInfo info = new FileInfo(tmpPath + part.StartSize);
							if (info.Exists)
							{
								// TODO uhm, should we do smt here ?! maybe check the size and set the state to ready?
								if (part.CurrentSize != part.StartSize + info.Length)
								{
									log.Warn("Run() crash recovery size mismatch of part " + part.StartSize + " from file " + file.TmpPath + " - db:" + part.CurrentSize + " real:" + info.Length);
									part.CurrentSize = part.StartSize + info.Length;
									complete = false;
								}
							}
							else
							{
								log.Error("Run() crash recovery part " + part.StartSize + " of file " + file.TmpPath + " is missing");
								this.serverHandler.RemovePart(file, part);
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
											log.Fatal("Run() crash recovery checking " + next.Name);
											FileStream fileStream = File.Open(this.serverHandler.GetCompletePath(part), FileMode.Open, FileAccess.ReadWrite);
											BinaryReader fileReader = new BinaryReader(fileStream);
											// extract the needed refernce bytes
											fileStream.Seek(-Settings.Instance.FileRollbackCheck, SeekOrigin.End);
											byte[] bytes = fileReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
											fileReader.Close();

											this.serverHandler.CheckNextReferenceBytes(part, bytes);
										}
										catch (Exception ex)
										{
											log.Fatal("Run() crash recovery", ex);
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
						if (complete && file.Parts.Count() > 0) { this.serverHandler.CheckFile(file); }
					}
				}
			}

			#endregion

			// connect to all servers which are enabled
			foreach (XGServer serv in this.objectRepository.Servers)
			{
				// TODO check this
				serv.Parent = null;
				serv.Parent = this.objectRepository;

				if (serv.Enabled)
				{
					this.serverHandler.ConnectServer(serv);
				}
			}
		}

		/// <summary>
		/// stop
		/// </summary>
		public void Stop()
		{
			// TODO stop server plugins
			foreach (XGServer serv in objectRepository.Servers)
			{
				this.serverHandler.DisconnectServer(serv);
			}
		}

		#endregion

		#region SERVER BACKEND PLUGIN

		public void AddServerBackendPlugin(AServerBackendPlugin aPlugin)
		{
			this.ObjectRepository = aPlugin.GetObjectRepository();
			this.FileRepository = aPlugin.GetFileRepository();
			this.Searches = aPlugin.GetSearchRepository();

			aPlugin.Parent = this;

			aPlugin.Start();
		}

		#endregion

		#region SERVER PLUGIN

		public void AddServerPlugin(AServerGeneralPlugin aPlugin)
		{
			aPlugin.Parent = this;
			aPlugin.ObjectRepository = this.ObjectRepository;

			aPlugin.Start();
		}

		#endregion

		#region EVENTHANDLER

		private void ObjectRepository_ChildAddedEventHandler(XGObject aParent, XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGServer))
			{
				XGServer aServer = aObj as XGServer;

				log.Info("RootObject_ChildAddedEventHandler(" + aServer.Name + ")");
				this.serverHandler.ConnectServer(aServer);
			}
		}

		private void ObjectRepository_ChildRemovedEventHandler(XGObject aParent, XGObject aObj)
		{
			if(aObj.GetType() == typeof(XGServer))
			{
				XGServer aServer = aObj as XGServer;

				aServer.Enabled = false;
				aServer.Commit();

				log.Info("RootObject_ChildRemovedEventHandler(" + aServer.Name + ")");
				this.serverHandler.DisconnectServer(aServer);
			}
		}

		private void ObjectRepository_EnabledChangedEventHandler(XGObject aObj)
		{
			if (aObj.GetType() == typeof(XGServer))
			{
				if(aObj.Enabled)
				{
					this.serverHandler.ConnectServer(aObj as XGServer);
				}
				else
				{
					this.serverHandler.DisconnectServer(aObj as XGServer);
				}
			}
		}

		private void IrcParser_ParsingErrorEventHandler(string aData)
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

		#region SEARCH

		public void AddSearch(string aSearch)
		{
			this.searches.Add(aSearch);

			if (this.SearchAddedEvent != null)
			{
				this.SearchAddedEvent(aSearch);
			}
		}

		public void RemoveSearch(string aSearch)
		{
			this.searches.Remove(aSearch);

			if (this.SearchRemovedEvent != null)
			{
				this.SearchRemovedEvent(aSearch);
			}
		}

		#endregion
	}
}
