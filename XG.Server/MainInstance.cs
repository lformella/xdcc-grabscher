// 
//  MainInstance.cs
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
using System.Linq;

using log4net;

using XG.Core;
using XG.Server.Helper;
using XG.Server.Plugin;

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

		static readonly ILog _log = LogManager.GetLogger(typeof(MainInstance));

		IrcParser _ircParser;
		ServerHandler _serverHandler;

		Servers _server;
		Servers ObjectRepository
		{
			get { return _server; }
			set
			{
				if(_server != null)
				{
					_server.Added -= new ObjectsDelegate(ServerObjectAdded);
					_server.Removed -= new ObjectsDelegate(ServerObjectRemoved);
					_server.EnabledChanged -= new ObjectDelegate(ServerObjectEnabledChanged);
				}
				_server = value;
				if(_server != null)
				{
					_server.Added += new ObjectsDelegate(ServerObjectAdded);
					_server.Removed += new ObjectsDelegate(ServerObjectRemoved);
					_server.EnabledChanged += new ObjectDelegate(ServerObjectEnabledChanged);
				}
			}
		}

		Files _files;
		Files FileRepository
		{
			get { return _files; }
			set { _files = value; }
		}

		Objects _searches;
		Objects Searches
		{
			get { return _searches; }
			set { _searches = value; }
		}

		#endregion

		#region RUN STOP

		/// <summary>
		/// Run method - should be called via thread
		/// </summary>
		public void Start()
		{
			_ircParser = new IrcParser();
			_ircParser.ParsingError += new DataTextDelegate(IrcParserParsingError);

			_serverHandler = new ServerHandler();
			_serverHandler.FileRepository = _files;
			_serverHandler.IrcParser = _ircParser;

			#region DUPE CHECK

			// check if there are some dupes in our database
			foreach (XG.Core.Server serv in _server.All)
			{
				foreach (XG.Core.Server s in _server.All)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						_log.Error("Run() removing dupe server " + s.Name);
						_server.Remove(s);
					}
				}

				foreach (Channel chan in serv.Channels)
				{
					foreach (Channel c in serv.Channels)
					{
						if (c.Name == chan.Name && c.Guid != chan.Guid)
						{
							_log.Error("Run() removing dupe channel " + c.Name);
							serv.RemoveChannel(c);
						}
					}

					foreach (Bot bot in chan.Bots)
					{
						foreach (Bot b in chan.Bots)
						{
							if (b.Name == bot.Name && b.Guid != bot.Guid)
							{
								_log.Error("Run() removing dupe bot " + b.Name);
								chan.RemoveBot(b);
							}
						}

						foreach (Packet pack in bot.Packets)
						{
							foreach (Packet p in bot.Packets)
							{
								if (p.Id == pack.Id && p.Guid != pack.Guid)
								{
									_log.Error("Run() removing dupe Packet " + p.Name);
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
			foreach (XG.Core.Server serv in _server.All)
			{
				serv.Connected = false;
				serv.ErrorCode = SocketErrorCode.None;

				foreach (Channel chan in serv.Channels)
				{
					chan.Connected = false;
					chan.ErrorCode = 0;

					foreach (Bot bot in chan.Bots)
					{
						bot.Connected = false;
						bot.BotState = BotState.Idle;

						foreach (Packet pack in bot.Packets)
						{
							pack.Connected = false;
						}
					}
				}
			}

			#endregion

			#region CLEAR OLD DL

			if (_files.All.Count() > 0 && Settings.Instance.ClearReadyDownloads)
			{
				foreach (File file in _files.All)
				{
					if (file.Enabled)
					{
						_files.Remove(file);
						_log.Info("Run() removing ready file " + file.Name);
					}
				}
			}

			#endregion

			#region CRASH RECOVERY

			if (_files.All.Count() > 0)
			{
				foreach (File file in _files.All)
				{
					// lets check if the directory is still on the harddisk
					if(!System.IO.Directory.Exists(Settings.Instance.TempPath + file.TmpPath))
					{
						_log.Warn("Run() crash recovery directory " + file.TmpPath + " is missing ");
						_serverHandler.RemoveFile(file);
						continue;
					}

					file.Locked = new object();

					if (!file.Enabled)
					{
						bool complete = true;
						string tmpPath = Settings.Instance.TempPath + file.TmpPath;

						foreach (FilePart part in file.Parts)
						{
							// check if the real file and the part is actual the same
							System.IO.FileInfo info = new System.IO.FileInfo(tmpPath + part.StartSize);
							if (info.Exists)
							{
								// TODO uhm, should we do smt here ?! maybe check the size and set the state to ready?
								if (part.CurrentSize != part.StartSize + info.Length)
								{
									_log.Warn("Run() crash recovery size mismatch of part " + part.StartSize + " from file " + file.TmpPath + " - db:" + part.CurrentSize + " real:" + info.Length);
									part.CurrentSize = part.StartSize + info.Length;
									complete = false;
								}
							}
							else
							{
								_log.Error("Run() crash recovery part " + part.StartSize + " of file " + file.TmpPath + " is missing");
								_serverHandler.RemovePart(file, part);
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
									FilePart next = file.Next(part) as FilePart;
									if (next != null && !next.IsChecked && next.CurrentSize - next.StartSize >= Settings.Instance.FileRollbackCheck)
									{
										complete = false;
										try
										{
											_log.Fatal("Run() crash recovery checking " + next.Name);
											System.IO.FileStream fileStream = System.IO.File.Open(_serverHandler.GetCompletePath(part), System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
											System.IO.BinaryReader fileReader = new System.IO.BinaryReader(fileStream);
											// extract the needed refernce bytes
											fileStream.Seek(-Settings.Instance.FileRollbackCheck, System.IO.SeekOrigin.End);
											byte[] bytes = fileReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
											fileReader.Close();

											_serverHandler.CheckNextReferenceBytes(part, bytes);
										}
										catch (Exception ex)
										{
											_log.Fatal("Run() crash recovery", ex);
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
						if (complete && file.Parts.Count() > 0) { _serverHandler.CheckFile(file); }
					}
				}
			}

			#endregion

			// connect to all servers which are enabled
			foreach (XG.Core.Server serv in _server.All)
			{
				// TODO check this
				serv.Parent = null;
				serv.Parent = _server;

				if (serv.Enabled)
				{
					_serverHandler.ConnectServer(serv);
				}
			}
		}

		/// <summary>
		/// stop
		/// </summary>
		public void Stop()
		{
			// TODO stop server plugins
			foreach (XG.Core.Server serv in _server.All)
			{
				_serverHandler.DisconnectServer(serv);
			}
		}

		#endregion

		#region SERVER BACKEND PLUGIN

		public void AddServerBackendPlugin(ABackendPlugin aPlugin)
		{
			ObjectRepository = aPlugin.LoadServers();
			FileRepository = aPlugin.LoadFiles();
			Searches = aPlugin.LoadSearches();

			AddServerPlugin(aPlugin);
		}

		#endregion

		#region SERVER PLUGIN

		public void AddServerPlugin(APlugin aPlugin)
		{
			aPlugin.Servers = ObjectRepository;
			aPlugin.Files = FileRepository;
			aPlugin.Searches = Searches;

			aPlugin.Start();
		}

		#endregion

		#region EVENTHANDLER

		void ServerObjectAdded(AObject aParent, AObject aObj)
		{
			if(aObj is XG.Core.Server)
			{
				XG.Core.Server aServer = aObj as XG.Core.Server;

				_log.Info("RootObject_ChildAddedEventHandler(" + aServer.Name + ")");
				_serverHandler.ConnectServer(aServer);
			}
		}

		void ServerObjectRemoved(AObject aParent, AObject aObj)
		{
			if(aObj is XG.Core.Server)
			{
				XG.Core.Server aServer = aObj as XG.Core.Server;

				aServer.Enabled = false;
				aServer.Commit();

				_log.Info("RootObject_ChildRemovedEventHandler(" + aServer.Name + ")");
				_serverHandler.DisconnectServer(aServer);
			}
		}

		void ServerObjectEnabledChanged(AObject aObj)
		{
			if(aObj is XG.Core.Server)
			{
				XG.Core.Server aServer = aObj as XG.Core.Server;

				if(aObj.Enabled)
				{
					_serverHandler.ConnectServer(aServer);
				}
				else
				{
					_serverHandler.DisconnectServer(aServer);
				}
			}
		}

		void IrcParserParsingError(string aData)
		{
			lock (this)
			{
				try
				{
					System.IO.StreamWriter sw = new System.IO.StreamWriter(System.IO.File.OpenWrite(Settings.Instance.ParsingErrorFile));
					sw.BaseStream.Seek(0, System.IO.SeekOrigin.End);
					sw.WriteLine(aData.Normalize());
					sw.Close();
				}
				catch (Exception) { }
			}
		}

		#endregion
	}
}
