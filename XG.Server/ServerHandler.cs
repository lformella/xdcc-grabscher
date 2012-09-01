// 
//  ServerHandler.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

using log4net;

using XG.Core;
using XG.Server.Helper;

namespace XG.Server
{
	public delegate void DownloadDelegate(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort);

	/// <summary>
	/// This class describes a irc server connection handler
	/// it does the following things
	/// - connect to or disconnect from an irc server
	/// - handling of global bot downloads
	/// - splitting and merging the files to download
	/// - writing files to disk
	/// - timering some clean up tasks
	/// </summary>
	public class ServerHandler
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(ServerHandler));

		IrcParser _ircParser;
		public IrcParser IrcParser
		{
			set
			{
				if(_ircParser != null)
				{
					_ircParser.Parent = null;
					_ircParser.AddDownload -= new DownloadDelegate (IrcParserAddDownload);
					_ircParser.RemoveDownload -= new BotDelegate (IrcParserRemoveDownload);
				}
				_ircParser = value;
				if(_ircParser != null)
				{
					_ircParser.Parent = this;
					_ircParser.AddDownload += new DownloadDelegate (IrcParserAddDownload);
					_ircParser.RemoveDownload += new BotDelegate (IrcParserRemoveDownload);
				}
			}
		}

		Files _files;
		public Files FileRepository
		{
			set
			{
				if(_files != null)
				{
				}
				_files = value;
				if(_files != null)
				{
				}
			}
		}

		Dictionary<XG.Core.Server, ServerConnection> _servers;
		Dictionary<Packet, BotConnection> _downloads;

		#endregion

		#region INIT

		public ServerHandler()
		{
			_servers = new Dictionary<XG.Core.Server, ServerConnection>();
			_downloads = new Dictionary<Packet, BotConnection>();

			// create my stuff if its not there
			new System.IO.DirectoryInfo(Settings.Instance.ReadyPath).Create();
			new System.IO.DirectoryInfo(Settings.Instance.TempPath).Create();

			// start the timed tasks
			new Thread(new ThreadStart(RunBotWatchdog)).Start();
			new Thread(new ThreadStart(RunTimer)).Start();
		}

		#endregion

		#region SERVER

		/// <summary>
		/// Connects to the given server by using a new ServerConnnect class
		/// </summary>
		/// <param name="aServer"></param>
		public void ConnectServer (XG.Core.Server aServer)
		{
			if (!_servers.ContainsKey (aServer))
			{
				ServerConnection con = new ServerConnection ();
				con.Parent = this;
				con.Server = aServer;
				con.IrcParser = _ircParser;

				con.Connection = new XG.Server.Connection.Connection();
				con.Connection.Hostname = aServer.Name;
				con.Connection.Port = aServer.Port;
				con.Connection.MaxData = 0;

				_servers.Add (aServer, con);

				con.Connected += new ServerDelegate(ServerConnectionConnected);
				con.Disconnected += new ServerSocketErrorDelegate(ServerConnectionDisconnected);

				// start a new thread wich connects to the given server
				new Thread(delegate() { con.Connection.Connect(); }).Start();
			}
			else
			{
				_log.Error("ConnectServer(" + aServer.Name + ") server is already in the dictionary");
			}
		}
		void ServerConnectionConnected(XG.Core.Server aServer)
		{
			// nom nom nom ...
		}

		/// <summary>
		/// Disconnects the given server
		/// </summary>
		/// <param name="aServer"></param>
		public void DisconnectServer(XG.Core.Server aServer)
		{
			if (_servers.ContainsKey(aServer))
			{
				ServerConnection con = _servers[aServer];
				con.Connection.Disconnect();
			}
			else
			{
				_log.Error("DisconnectServer(" + aServer.Name + ") server is not in the dictionary");
			}
		}
		void ServerConnectionDisconnected(XG.Core.Server aServer, SocketErrorCode aValue)
		{
			if (_servers.ContainsKey (aServer))
			{
				ServerConnection con = _servers[aServer];

				if (aServer.Enabled)
				{
					// disable the server if the host was not found
					// this is also triggered if we have no internet connection and disables all channels
					/*if(	aValue == SocketErrorCode.HostNotFound ||
						aValue == SocketErrorCode.HostNotFoundTryAgain)
					{
						aServer.Enabled = false;
					}
					else*/
					{
						int time = Settings.Instance.ReconnectWaitTime;
						switch(aValue)
						{
							case SocketErrorCode.HostIsDown:
							case SocketErrorCode.HostUnreachable:
							case SocketErrorCode.ConnectionTimedOut:
							case SocketErrorCode.ConnectionRefused:
								time = Settings.Instance.ReconnectWaitTimeLong;
								break;
//							case SocketErrorCode.HostNotFound:
//							case SocketErrorCode.HostNotFoundTryAgain:
//								time = Settings.Instance.ReconnectWaitTimeReallyLong;
//								break;
						}
						new Timer(new TimerCallback(ReconnectServer), aServer, time, System.Threading.Timeout.Infinite);
					}
				}
				else
				{
					con.Connected -= new ServerDelegate(ServerConnectionConnected);
					con.Disconnected -= new ServerSocketErrorDelegate(ServerConnectionDisconnected);

					con.Server = null;
					con.IrcParser = null;

					_servers.Remove(aServer);
				}

				con.Connection = null;
			}
			else
			{
				_log.Error("server_DisconnectedEventHandler(" + aServer.Name + ", " + aValue + ") server is not in the dictionary");
			}
		}

		void ReconnectServer(object aServer)
		{
			XG.Core.Server tServer = aServer as XG.Core.Server;

			if (_servers.ContainsKey(tServer))
			{
				ServerConnection con = _servers[tServer];

				if (tServer.Enabled)
				{
					_log.Error("ReconnectServer(" + tServer.Name + ")");

					// TODO do we need a new connection here?
					con.Connection = new XG.Server.Connection.Connection();
					con.Connection.Hostname = tServer.Name;
					con.Connection.Port = tServer.Port;
					con.Connection.MaxData = 0;

					con.Connection.Connect();
				}
			}
			else
			{
				_log.Error("ReconnectServer(" + tServer.Name + ") server is not in the dictionary");
			}
		}

		#endregion

		#region BOT

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPack"></param>
		/// <param name="aChunk"></param>
		/// <param name="aIp"></param>
		/// <param name="aPort"></param>
		void IrcParserAddDownload(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort)
		{
			new Thread(delegate()
			{
				if (!_downloads.ContainsKey(aPack))
				{
					BotConnection con = new BotConnection();
					con.Parent = this;
					con.Packet = aPack;
					con.StartSize = aChunk;

					con.Connection = new XG.Server.Connection.Connection();
					con.Connection.Hostname = aIp.ToString();
					con.Connection.Port = aPort;
					con.Connection.MaxData = aPack.RealSize - aChunk;

					con.Connected += new PacketBotConnectDelegate(BotConnected);
					con.Disconnected += new PacketBotConnectDelegate(BotDisconnected);

					_downloads.Add(aPack, con);
					con.Connection.Connect();
				}
				else
				{
					// uhh - that should not happen
					_log.Error("StartDownload(" + aPack.Name + ") is already downloading");
				}
			}).Start();
		}
		void BotConnected (Packet aPack, BotConnection aCon)
		{
		}

		void IrcParserRemoveDownload (Bot aBot)
		{
			foreach (KeyValuePair<Packet, BotConnection> kvp in _downloads)
			{
				if (kvp.Key.Parent == aBot)
				{
					kvp.Value.Connection.Disconnect();
					break;
				}
			}
		}
		void BotDisconnected(Packet aPacket, BotConnection aCon)
		{
			aCon.Packet = null;
			aCon.Connection = null;

			if (_downloads.ContainsKey(aPacket))
			{
				aCon.Connected -= new PacketBotConnectDelegate(BotConnected);
				aCon.Disconnected -= new PacketBotConnectDelegate(BotDisconnected);
				_downloads.Remove(aPacket);

				try
				{
					// if the connection never connected, there will be no part!
					// and if we manually killed stopped the packet there will be no parent of the part
					if(aCon.Part != null && aCon.Part.Parent != null)
					{
						// do this here because the bothandler sets the part state and after this we can check the file
						CheckFile(aCon.Part.Parent);
					}
				}
				catch (Exception ex)
				{
					_log.Fatal("bot_Disconnected()", ex);
				}

				try
				{
					ServerConnection sc = _servers[aPacket.Parent.Parent.Parent];
					sc.CreateTimer(aPacket.Parent, Settings.Instance.CommandWaitTime, false);
				}
				catch (Exception ex)
				{
					_log.Fatal("bot_Disconnected() request", ex);
				}
			}
		}

		#endregion

		#region FILE

		#region HELPER

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aFile"></param>
		/// <returns></returns>
		public string GetCompletePath(File aFile)
		{
			return Settings.Instance.TempPath + aFile.TmpPath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <returns></returns>
		public string GetCompletePath(FilePart aPart)
		{
			return GetCompletePath(aPart.Parent) + aPart.StartSize;
		}

		#endregion

		#region FILE

		/// <summary>
		/// Returns a file or null if there isnt one
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aSize"></param>
		/// <returns></returns>
		File GetFile(string aName, Int64 aSize)
		{
			string name = XGHelper.ShrinkFileName(aName, aSize);
			foreach (File file in _files.All)
			{
				//Console.WriteLine(file.TmpPath + " - " + name);
				if (file.TmpPath == name)
				{
					// lets check if the directory is still on the harddisk
					if(!System.IO.Directory.Exists(Settings.Instance.TempPath + file.TmpPath))
					{
						_log.Warn("GetFile(" + aName + ", " + aSize + ") directory " + file.TmpPath + " is missing ");
						RemoveFile(file);
						break;
					}
					return file;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns a file - an old if it is already there, or a new
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aSize"></param>
		/// <returns></returns>
		public File GetNewFile(string aName, Int64 aSize)
		{
			File tFile = GetFile(aName, aSize);
			if (tFile == null)
			{
				tFile = new File(aName, aSize);
				_files.Add(tFile);
				try
				{
					System.IO.Directory.CreateDirectory(Settings.Instance.TempPath + tFile.TmpPath);
				}
				catch (Exception ex)
				{
					_log.Fatal("GetNewFile()", ex);
					tFile = null;
				}
			}
			return tFile;
		}

		/// <summary>
		/// removes a File
		/// stops all running part connections and removes the file
		/// </summary>
		/// <param name="aFile"></param>
		public void RemoveFile(File aFile)
		{
			_log.Info("RemoveFile(" + aFile.Name + ", " + aFile.Size + ")");

			// check if this file is currently downloaded
			bool skip = false;
			foreach (FilePart part in aFile.Parts)
			{
				// disable the packet if it is active
				if (part.PartState == FilePartState.Open)
				{
					if (part.Packet != null)
					{
						part.Packet.Enabled = false;
						skip = true;
					}
				}
			}
			if (skip) { return; }

			Helper.Filesystem.DeleteDirectory(GetCompletePath(aFile));
			_files.Remove(aFile);
		}

		#endregion

		#region PART

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aFile"></param>
		/// <param name="aSize"></param>
		/// <returns></returns>
		public FilePart GetPart(File aFile, Int64 aSize)
		{
			FilePart returnPart = null;

			IEnumerable<FilePart> parts = aFile.Parts;
			if (parts.Count() == 0 && aSize == 0)
			{
				returnPart = new FilePart();
				returnPart.StartSize = aSize;
				returnPart.CurrentSize = aSize;
				returnPart.StopSize = aFile.Size;
				returnPart.IsChecked = true;
				aFile.Add(returnPart);
			}
			else
			{
				// first search incomplete parts not in use
				foreach (FilePart part in parts)
				{
					Int64 size = part.CurrentSize - Settings.Instance.FileRollback;
					if (part.PartState == FilePartState.Closed && (size < 0 ? 0 : size) == aSize)
					{
						returnPart = part;
						break;
					}
				}
				// if multi dling is enabled
				if (returnPart == null && Settings.Instance.EnableMultiDownloads)
				{
					// now search incomplete parts in use
					foreach (FilePart part in parts)
					{
						if (part.PartState == FilePartState.Open)
						{
							// split the part
							if (part.StartSize < aSize && part.StopSize > aSize)
							{
								returnPart = new FilePart();
								returnPart.StartSize = aSize;
								returnPart.CurrentSize = aSize;
								returnPart.StopSize = part.StopSize;

								// update previous part
								part.StopSize = aSize;
								part.Commit();
								aFile.Add(returnPart);
								break;
							}
						}
					}
				}
			}
			return returnPart;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aFile"></param>
		/// <param name="aPart"></param>
		public void RemovePart(File aFile, FilePart aPart)
		{
			_log.Info("RemovePart(" + aFile.Name + ", " + aFile.Size + ", " + aPart.StartSize + ")");

			IEnumerable<FilePart> parts = aFile.Parts;
			foreach (FilePart part in parts)
			{
				if (part.StopSize == aPart.StartSize)
				{
					part.StopSize = aPart.StopSize;
					if (part.PartState == FilePartState.Ready)
					{
						_log.Info("RemovePart(" + aFile.Name + ", " + aFile.Size + ", " + aPart.StartSize + ") expanding part " + part.StartSize + " to " + aPart.StopSize);
						part.PartState = FilePartState.Closed;
						part.Commit();
						break;
					}
				}
			}

			aFile.Remove(aPart);
			if (aFile.Parts.Count() == 0) { RemoveFile(aFile); }
			else
			{
				Helper.Filesystem.DeleteFile(GetCompletePath(aPart));
			}
		}

		#endregion

		#region GET NEXT STUFF

		/// <summary>
		/// Returns the next chunk of a file (subtracts already the rollback value for continued downloads)
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aSize"></param>
		/// <returns>-1 if there is no part, 0<= if there is a new part available</returns>
		public Int64 GetNextAvailablePartSize(string aName, Int64 aSize)
		{
			File tFile = GetFile(aName, aSize);
			if (tFile == null) { return 0; }

			Int64 nextSize = -1;
			IEnumerable<FilePart> parts = tFile.Parts;
			if (parts.Count() == 0) { nextSize = 0; }
			else
			{
				// first search incomplete parts not in use
				foreach (FilePart part in parts)
				{
					if (part.PartState == FilePartState.Closed)
					{
						nextSize = part.CurrentSize - Settings.Instance.FileRollback;
						// uhm, this is a bug if we have a very small downloaded file
						// so just return 0
						if (nextSize < 0) { nextSize = 0; }
						break;
					}
				}
				// if multi downloading is enabled
				if (nextSize == -1 && Settings.Instance.EnableMultiDownloads)
				{
					Int64 timeMissing = 0;
					Int64 startChunk = 0;

					// now search incomplete parts in use
					foreach (FilePart part in parts)
					{
						if (part.PartState == FilePartState.Open)
						{
							// find the file with the max time, but only if there is some space to download
							if (timeMissing < part.TimeMissing && part.MissingSize > 4 * Settings.Instance.FileRollback)
							{
								timeMissing = part.TimeMissing;
								// and divide the missing size in two parts
								startChunk = part.StopSize - (part.MissingSize / 2);
							}
						}
					}

					// only try a new download if there is some time
					if (timeMissing > Settings.Instance.MutliDownloadMinimumTime)
					{
						nextSize = startChunk;
					}
				}
			}
			return nextSize;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aObj"></param>
		public void CheckNextReferenceBytes(object aObj)
		{
			PartBytesObject pbo = aObj as PartBytesObject;
			CheckNextReferenceBytes(pbo.Part, pbo.Bytes, true);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <param name="aBytes"></param>
		/// <returns></returns>
		public Int64 CheckNextReferenceBytes(FilePart aPart, byte[] aBytes)
		{
			return CheckNextReferenceBytes(aPart, aBytes, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <param name="aBytes"></param>
		/// <param name="aThreaded"></param>
		/// <returns></returns>
		Int64 CheckNextReferenceBytes(FilePart aPart, byte[] aBytes, bool aThreaded)
		{
			File tFile = aPart.Parent;
			IEnumerable<FilePart> parts = tFile.Parts;
			_log.Info("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ", " + aPart.StopSize + ") with " + parts.Count() + " parts called");

			foreach (FilePart part in parts)
			{
				if (part.StartSize == aPart.StopSize)
				{
					// is the part already checked?
					if (part.IsChecked) { break; }

					// the file is open
					if (part.PartState == FilePartState.Open)
					{
						BotConnection bc = null;
						Packet pack = null;
						foreach (KeyValuePair<Packet, BotConnection> kvp in _downloads)
						{
							if (kvp.Value.Part == part)
							{
								bc = kvp.Value;
								pack = kvp.Key;
								break;
							}
						}
						if (bc != null)
						{
							if (!XGHelper.IsEqual(bc.ReferenceBytes, aBytes))
							{
								_log.Warn("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") removing next part " + part.StartSize);
								bc.RemovePart = true;
								bc.Connection.Disconnect();
								pack.Enabled = false;
								RemovePart(tFile, part);
								return part.StopSize;
							}
							else
							{
								_log.Info("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is checked");
								part.IsChecked = true;
								part.Commit();
								return 0;
							}
						}
						else
						{
							_log.Error("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is open, but has no bot connect");
							return 0;
						}
					}
					// it is already ready
					else if (part.PartState == FilePartState.Closed || part.PartState == FilePartState.Ready)
					{
						string fileName = GetCompletePath(part);
						try
						{
							System.IO.BinaryReader reader = Filesystem.OpenFileReadable(fileName);
							byte[] bytes = reader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
							reader.Close();

							if (!XGHelper.IsEqual(bytes, aBytes))
							{
								_log.Warn("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") removing closed part " + part.StartSize);
								RemovePart(tFile, part);
								return part.StopSize;
							}
							else
							{
								part.IsChecked = true;
								part.Commit();

								if (part.PartState == FilePartState.Ready)
								{
									// file is not the last, so check the next one
									if (part.StopSize < tFile.Size)
									{
										System.IO.FileStream fileStream = System.IO.File.Open(fileName, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
										System.IO.BinaryReader fileReader = new System.IO.BinaryReader(fileStream);
										// extract the needed refernce bytes
										fileStream.Seek(-Settings.Instance.FileRollbackCheck, System.IO.SeekOrigin.End);
										bytes = fileReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
										// and truncate the file
										//fileStream.SetLength(fileStream.Length - Settings.Instance.FileRollbackCheck);
										fileStream.SetLength(part.StopSize - part.StartSize);
										fileReader.Close();

										// dont open a new thread if we are already threaded
										if (aThreaded)
										{
											CheckNextReferenceBytes(part, bytes, false);
										}
										else
										{
											new Thread(new ParameterizedThreadStart(CheckNextReferenceBytes)).Start(new PartBytesObject(part, bytes));
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							_log.Fatal("CheckNextReferenceBytes(" + aPart.Parent.Name + ", " + aPart.Parent.Size + ", " + aPart.StartSize + ") handling part " + part.StartSize + "", ex);
						}
					}
					else
					{
						_log.Error("CheckNextReferenceBytes(" + aPart.Parent.Name + ", " + aPart.Parent.Size + ", " + aPart.StartSize + ") do not know what to do with part " + part.StartSize);
					}

					break;
				}
			}
			return 0;
		}

		#endregion

		#region CHECK AND JOIN FILE

		/// <summary>
		/// Checks a file and if it is complete it starts a thread which join the file
		/// </summary>
		/// <param name="aFile"></param>
		public void CheckFile(File aFile)
		{
			lock (aFile.Locked)
			{
				_log.Info("CheckFile(" + aFile.Name + ")");
				if (aFile.Parts.Count() == 0) { return; }

				bool complete = true;
				IEnumerable<FilePart> parts = aFile.Parts;
				foreach (FilePart part in parts)
				{
					if (part.PartState != FilePartState.Ready)
					{
						complete = false;
						_log.Info("CheckFile(" + aFile.Name + ") part " + part.StartSize + " is not complete");
						break;
					}
				}
				if (complete)
				{
					new Thread(new ParameterizedThreadStart(JoinCompleteParts)).Start(aFile);
				}
			}
		}

		/// <summary>
		/// Joins all parts of a File together by doing this
		/// - disabling all matching packets to avoid the same download again
		/// - merging the file and checking the file size
		/// </summary>
		/// <param name="aObject"></param>
		public void JoinCompleteParts(object aObject)
		{
			File tFile = aObject as File;
			lock (tFile.Locked)
			{
				_log.Info("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") starting");

				#region DISABLE ALL MATCHING PACKETS

				// TODO remove all CD* packets if a multi packet was downloaded

				string fileName = XGHelper.ShrinkFileName(tFile.Name, 0);

				foreach (KeyValuePair<XG.Core.Server, ServerConnection> kvp in _servers)
				{
					XG.Core.Server tServ = kvp.Key;
					foreach (Channel tChan in tServ.Channels)
					{
						if (tChan.Connected)
						{
							foreach (Bot tBot in tChan.Bots)
							{
								foreach (Packet tPack in tBot.Packets)
								{
									if (tPack.Enabled && (
										XGHelper.ShrinkFileName(tPack.RealName, 0).EndsWith(fileName) ||
										XGHelper.ShrinkFileName(tPack.Name, 0).EndsWith(fileName)
										))
									{
										_log.Info("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") disabling packet #" + tPack.Id + " (" + tPack.Name + ") from " + tPack.Parent.Name);
										tPack.Enabled = false;
										tPack.Commit();
									}
								}
							}
						}
					}
				}

				#endregion

				if (tFile.Parts.Count() == 0)
				{
					return;
				}

				bool error = true;
				bool deleteOnError = true;
				IEnumerable<FilePart> parts = tFile.Parts;
				string fileReady = Settings.Instance.ReadyPath + tFile.Name;

				try
				{
					System.IO.FileStream stream = System.IO.File.Open(fileReady, System.IO.FileMode.Create, System.IO.FileAccess.Write);
					System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);
					foreach (FilePart part in parts)
					{
						try
						{
							System.IO.BinaryReader reader = Filesystem.OpenFileReadable(GetCompletePath(part));
							byte[] data;
							while ((data = reader.ReadBytes((int)Settings.Instance.DownloadPerRead)).Length > 0)
							{
								writer.Write(data);
								writer.Flush();
							}
							reader.Close();
						}
						catch (Exception ex)
						{
							_log.Fatal("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") handling part " + part.StartSize + "", ex);
							// dont delete the source if the disk is full!
							// taken from http://www.dotnetspider.com/forum/101158-Disk-full-C.aspx
							// TODO this doesnt work :(
							int hresult = (int)ex.GetType().GetField("_HResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ex);
							if ((hresult & 0xFFFF) == 112L) { deleteOnError = false; }
							break;
						}
					}
					writer.Close();
					stream.Close();

					Int64 size = new System.IO.FileInfo(fileReady).Length;
					if (size == tFile.Size)
					{
						Helper.Filesystem.DeleteDirectory(Settings.Instance.TempPath + tFile.TmpPath);
						_log.Info("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") file build");

						// statistics
						Statistic.Instance.Increase(StatisticType.FilesCompleted);

						// the file is complete and enabled
						tFile.Enabled = true;
						error = false;

						// maybee clear it
						if (Settings.Instance.ClearReadyDownloads)
						{
							RemoveFile(tFile);
						}

						// great, all went right, so lets check what we can do with the file
						new Thread(delegate() { HandleFile(fileReady); }).Start();
					}
					else
					{
						_log.Error("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") filesize is not the same: " + size);

						// statistics
						Statistic.Instance.Increase(StatisticType.FilesBroken);
					}
				}
				catch (Exception ex)
				{
					_log.Fatal("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") make", ex);

					// statistics
					Statistic.Instance.Increase(StatisticType.FilesBroken);
				}

				if (error && deleteOnError)
				{
					// the creation was not successfull, so delete the files and parts
					Helper.Filesystem.DeleteFile(fileReady);
					RemoveFile(tFile);
				}
			}
		}

		/// <summary>
		/// Handles a downloaded file by calling the FileHandler string in the settings file
		/// </summary>
		/// <param name="aFile"></param>
		public void HandleFile(string aFile)
		{
			if (!string.IsNullOrEmpty(aFile))
			{
				int pos = aFile.LastIndexOf('/');
				string folder = aFile.Substring(0, pos);
				string file = aFile.Substring(pos + 1);

				pos = file.LastIndexOf('.');
				string filename = pos != -1 ? file.Substring(0, pos) : file;
				string fileext = pos != -1 ? file.Substring(pos + 1) : "";

				foreach (string line in Settings.InstanceReload.FileHandler)
				{
					if (string.IsNullOrEmpty(line)) { continue; }
					string[] values = line.Split('#');

					Match tMatch = Regex.Match(file, values[0], RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						for (int a = 1; a < values.Length; a++)
						{
							pos = values[a].IndexOf(' ');
							string process = values[a].Substring(0, pos);
							string arguments = values[a].Substring(pos + 1);

							arguments = arguments.Replace("%PATH%", aFile);
							arguments = arguments.Replace("%FOLDER%", folder);
							arguments = arguments.Replace("%FILE%", file);
							arguments = arguments.Replace("%FILENAME%", filename);
							arguments = arguments.Replace("%EXTENSION%", fileext);

							try
							{
								Process p = Process.Start(process, arguments);
								// TODO should we block all other procs?!
								p.WaitForExit();
							}
							catch (Exception ex)
							{
								_log.Fatal("HandleFile(" + aFile + ") Process.Start(" + process + ", " + arguments + ")", ex);
							}
						}
					}
				}
			}
		}

		#endregion

		#endregion

		#region TIMER TASKS

		void RunBotWatchdog()
		{
			while (true)
			{
				Thread.Sleep(Settings.Instance.BotOfflineCheckTime);

				int a = 0;
				foreach (KeyValuePair<XG.Core.Server, ServerConnection> kvp in _servers)
				{
					if (kvp.Value.IsRunning)
					{
						foreach (Channel tChan in kvp.Key.Channels)
						{
							if (tChan.Connected)
							{
								foreach (Bot tBot in tChan.Bots)
								{
									if (!tBot.Connected && (DateTime.Now - tBot.LastContact).TotalMilliseconds > Settings.Instance.BotOfflineTime && tBot.OldestActivePacket() == null)
									{
										a++;
										tChan.RemoveBot(tBot);
									}
								}
							}
						}
					}
				}
				if (a > 0) { _log.Info("RunBotWatchdog() removed " + a + " offline bot(s)"); }

				// TODO scan for empty channels and send a "xdcc list" command to all the people in there
				// in some channels the bots are silent and have the same (no) rights like normal users
			}
		}

		void RunTimer()
		{
			while (true)
			{
				foreach (KeyValuePair<XG.Core.Server, ServerConnection> kvp in _servers)
				{
					ServerConnection sc = kvp.Value;
					if (sc.IsRunning)
					{
						sc.TriggerTimerRun();
					}
				}

				Thread.Sleep((int)Settings.Instance.TimerSleepTime);
			}
		}

		#endregion
	}
}
