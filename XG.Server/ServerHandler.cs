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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using XG.Core;

namespace XG.Server
{
	public delegate void DownloadDelegate(XGPacket aPack, Int64 aChunk, IPAddress aIp, int aPort);
	public delegate void PacketBotConnectDelegate(XGPacket aPack, BotConnect aCon);

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

		private Dictionary<XGServer, ServerConnect> myServers;
		private Dictionary<XGPacket, BotConnect> myDownloads;
		private List<XGFile> myFiles;

		#endregion

		#region EVENTS

		public event DataTextDelegate ParsingErrorEvent;

		public event ObjectDelegate ObjectChangedEvent;
		public event ObjectObjectDelegate ObjectAddedEvent;
		public event ObjectObjectDelegate ObjectRemovedEvent;

		#endregion

		#region INIT

		public ServerHandler(List<XGFile> aFiles)
		{
			this.myServers = new Dictionary<XGServer, ServerConnect>();
			this.myDownloads = new Dictionary<XGPacket, BotConnect>();
			this.myFiles = aFiles;

			// create my stuff if its not there
			new DirectoryInfo(Settings.Instance.ReadyPath).Create();
			new DirectoryInfo(Settings.Instance.TempPath).Create();

			// start the timed tasks
			new Thread(new ThreadStart(RunBotWatchdog)).Start();
			new Thread(new ThreadStart(RunTimer)).Start();
		}

		#endregion

		#region SERVER

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aServer"></param>
		public void ConnectServer (object aServer)
		{
			XGServer tServer = aServer as XGServer;

			if (!this.myServers.ContainsKey (tServer))
			{
				ServerConnect con = new ServerConnect (this);
				this.myServers.Add (tServer, con);

				con.NewDownloadEvent += new DownloadDelegate (server_NewDownloadEventHandler);
				con.KillDownloadEvent += new BotDelegate (server_KillDownloadEventHandler);
				con.DisconnectedEvent += new ServerSocketErrorDelegate(server_DisconnectedEventHandler);
				con.ConnectedEvent += new ServerDelegate(server_ConnectedEventHandler);
				con.ParsingErrorEvent += new DataTextDelegate(server_ParsingErrorEventHandler);

				con.ObjectAddedEvent += new ObjectObjectDelegate(_ObjectAddedEventHandler);
				con.ObjectChangedEvent += new ObjectDelegate(_ObjectChangedEventHandler);
				con.ObjectRemovedEvent += new ObjectObjectDelegate(_ObjectRemovedEventHandler);

				// start a new thread wich connects to the given server
				new Thread(delegate() { con.Connect(tServer); }).Start();
			}
		}
		private void server_ConnectedEventHandler(XGServer aServer)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aServer"></param>
		public void DisconnectServer(XGServer aServer)
		{
			if (this.myServers.ContainsKey(aServer))
			{
				ServerConnect con = myServers[aServer];
				con.Disconnect();
			}
		}
		private void server_DisconnectedEventHandler(XGServer aServer, SocketErrorCode aValue)
		{
			if (this.myServers.ContainsKey (aServer))
			{
				ServerConnect con = this.myServers[aServer];

				if (!aServer.Enabled)
				{
					con.NewDownloadEvent -= new DownloadDelegate (server_NewDownloadEventHandler);
					con.KillDownloadEvent -= new BotDelegate (server_KillDownloadEventHandler);
					con.DisconnectedEvent -= new ServerSocketErrorDelegate(server_DisconnectedEventHandler);
					con.ConnectedEvent -= new ServerDelegate(server_ConnectedEventHandler);
					con.ParsingErrorEvent -= new DataTextDelegate(server_ParsingErrorEventHandler);

					con.ObjectAddedEvent -= new ObjectObjectDelegate(_ObjectAddedEventHandler);
					con.ObjectChangedEvent -= new ObjectDelegate(_ObjectChangedEventHandler);
					con.ObjectRemovedEvent -= new ObjectObjectDelegate(_ObjectRemovedEventHandler);

					this.myServers.Remove(aServer);
				}
				else
				{
					int time = Settings.Instance.ReconnectWaitTime;
					switch(aValue)
					{
						case SocketErrorCode.HostIsDown:
						case SocketErrorCode.HostNotFound:
						case SocketErrorCode.HostNotFoundTryAgain:
						case SocketErrorCode.HostUnreachable:
						case SocketErrorCode.ConnectionTimedOut:
						case SocketErrorCode.ConnectionRefused:
							time = Settings.Instance.ReconnectWaitTimeLong;
							break;
					}
					new Timer(new TimerCallback(this.ReconnectServer), aServer, time, System.Threading.Timeout.Infinite);
				}
			}
		}

		private void ReconnectServer(object aServer)
		{
			XGServer tServer = aServer as XGServer;

			if (this.myServers.ContainsKey(tServer))
			{
				ServerConnect con = this.myServers[tServer];

				if (tServer.Enabled)
				{
					con.Connect(tServer);
				}
			}
		}

		/// <summary>
		/// Just throws the event on step ahead
		/// </summary>
		/// <param name="aData"></param>
		void server_ParsingErrorEventHandler(string aData)
		{
			if (this.ParsingErrorEvent != null)
			{
				this.ParsingErrorEvent(aData);
			}
		}

		/// <summary>
		/// Just throws the event on step ahead
		/// </summary>
		/// <param name="aParentObj"></param>
		/// <param name="aObj"></param>
		void _ObjectRemovedEventHandler(XGObject aParentObj, XGObject aObj)
		{
			if (this.ObjectRemovedEvent != null)
			{
				this.ObjectRemovedEvent(aParentObj, aObj);
			}
		}

		/// <summary>
		/// Just throws the event on step ahead
		/// </summary>
		/// <param name="aObj"></param>
		void _ObjectChangedEventHandler(XGObject aObj)
		{
			if (this.ObjectChangedEvent != null)
			{
				this.ObjectChangedEvent(aObj);
			}
		}

		/// <summary>
		/// Just throws the event on step ahead
		/// </summary>
		/// <param name="aParentObj"></param>
		/// <param name="aObj"></param>
		void _ObjectAddedEventHandler(XGObject aParentObj, XGObject aObj)
		{
			if (this.ObjectAddedEvent != null)
			{
				this.ObjectAddedEvent(aParentObj, aObj);
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
		private void server_NewDownloadEventHandler(XGPacket aPack, Int64 aChunk, IPAddress aIp, int aPort)
		{
			new Thread(delegate()
			{
				if (!this.myDownloads.ContainsKey(aPack))
				{
					BotConnect con = new BotConnect(this);
					con.DisconnectedEvent += new PacketBotConnectDelegate(bot_Disconnected);
					con.ConnectedEvent += new PacketBotConnectDelegate(bot_Connected);
					con.ObjectChangedEvent += new ObjectDelegate(_ObjectChangedEventHandler);

					this.myDownloads.Add(aPack, con);
					con.Connect(aPack, aChunk, aIp, aPort);
				}
				else
				{
					// uhh - that should not happen
					this.Log("StartDownload(" + aPack.Name + ") is already downloading", LogLevel.Error);
				}
			}).Start();
		}
		private void bot_Connected (XGPacket aPack, BotConnect aCon)
		{
		}

		private void server_KillDownloadEventHandler (XGBot aBot)
		{
			foreach (KeyValuePair<XGPacket, BotConnect> kvp in this.myDownloads)
			{
				if (kvp.Key.Parent == aBot)
				{
					kvp.Value.Disconnect();
					break;
				}
			}
		}
		private void bot_Disconnected(XGPacket aPacket, BotConnect aCon)
		{
			if (myDownloads.ContainsKey(aPacket))
			{
				aCon.DisconnectedEvent -= new PacketBotConnectDelegate(bot_Disconnected);
				aCon.ConnectedEvent -= new PacketBotConnectDelegate(bot_Connected);
				aCon.ObjectChangedEvent -= new ObjectDelegate(_ObjectChangedEventHandler);
				this.myDownloads.Remove(aPacket);

				try
				{
					// if the connection never connected, there will be no part!
					if(aCon.Part != null)
					{
						// do this here because the bothandler sets the part state and after this we can check the file
						this.CheckFile(aCon.Part.Parent);
					}
				}
				catch (Exception ex)
				{
					this.Log("bot_Disconnected() " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				}

				try
				{
					ServerConnect sc = this.myServers[aPacket.Parent.Parent.Parent];
					sc.CreateTimer(aPacket.Parent, Settings.Instance.CommandWaitTime);
				}
				catch (Exception ex)
				{
					this.Log("bot_Disconnected() request: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
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
		public string GetCompletePath(XGFile aFile)
		{
			return Settings.Instance.TempPath + aFile.TmpPath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <returns></returns>
		public string GetCompletePath(XGFilePart aPart)
		{
			return this.GetCompletePath(aPart.Parent) + aPart.StartSize;
		}

		#endregion

		#region PHYSICAL FUNCTIONS

		/// <summary>
		/// Moves a file
		/// </summary>
		/// <param name="aNameOld">old filename</param>
		/// <param name="aNameNew">new filename</param>
		/// <returns>true if operation succeeded, false if it failed</returns>
		public bool FileMove(string aNameOld, string aNameNew)
		{
			if (File.Exists(aNameOld))
			{
				try
				{
					File.Move(aNameOld, aNameNew);
					return true;
				}
				catch (Exception ex)
				{
					this.Log("FileMove(" + aNameOld + ", " + aNameNew + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Deletes a file
		/// </summary>
		/// <param name="aName">file to delete</param>
		/// <returns>true if operation succeeded, false if it failed</returns>
		public bool FileDelete(string aName)
		{
			if (File.Exists(aName))
			{
				try
				{
					File.Delete(aName);
					return true;
				}
				catch (Exception ex)
				{
					this.Log("FileDlete(" + aName + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Deletes a directory
		/// </summary>
		/// <param name="aName">directory to delete</param>
		/// <returns>true if operation succeeded, false if it failed</returns>
		public bool DirectoryDelete(string aName)
		{
			if (Directory.Exists(aName))
			{
				try
				{
					Directory.Delete(aName, true);
					return true;
				}
				catch (Exception ex)
				{
					this.Log("DirectoryDlete(" + aName + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Lists a directory
		/// </summary>
		/// <param name="aDir">directory to list</param>
		/// <returns>file list</returns>
		public string[] ListDirectory(string aDir)
		{
			return this.ListDirectory(aDir, null);
		}

		/// <summary>
		/// Lists a directory with search pattern
		/// </summary>
		/// <param name="aDir">directory to list</param>
		/// <param name="aSearch">search pattern can be null to disable this</param>
		/// <returns>file list</returns>
		public string[] ListDirectory(string aDir, string aSearch)
		{
			string[] files = new string[] { };
			try
			{
				files = aSearch == null ? Directory.GetFiles(aDir) : Directory.GetFiles(aDir, aSearch);
				Array.Sort(files);
			}
			catch (Exception ex)
			{
				this.Log("ListDirectory(" + aDir + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
			return files;
		}

		#endregion

		#region FILE

		/// <summary>
		/// Returns a file or null if there isnt one
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aSize"></param>
		/// <returns></returns>
		private XGFile GetFile(string aName, Int64 aSize)
		{
			string name = XGHelper.ShrinkFileName(aName, aSize);
			foreach (XGFile file in this.myFiles.ToArray())
			{
				//Console.WriteLine(file.TmpPath + " - " + name);
				if (file.TmpPath == name)
				{
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
		public XGFile GetNewFile(string aName, Int64 aSize)
		{
			XGFile tFile = this.GetFile(aName, aSize);
			if (tFile == null)
			{
				tFile = new XGFile(aName, aSize);
				this.myFiles.Add(tFile);
				this.ObjectAddedEvent(null, tFile);
				new DirectoryInfo(Settings.Instance.TempPath + tFile.TmpPath).Create();
			}
			return tFile;
		}

		/// <summary>
		/// removes a XGFile
		/// stops all running part connections and removes the file
		/// </summary>
		/// <param name="aFile"></param>
		public void RemoveFile(XGFile aFile)
		{
			this.Log("RemoveFile(" + aFile.Name + ", " + aFile.Size + ")", LogLevel.Notice);

			// check if this file is currently downloaded
			bool skip = false;
			foreach (XGFilePart part in aFile.Children)
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

			this.DirectoryDelete(this.GetCompletePath(aFile));
			this.myFiles.Remove(aFile);
			this.ObjectRemovedEvent(null, aFile);
		}

		#endregion

		#region PART

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aFile"></param>
		/// <param name="aSize"></param>
		/// <returns></returns>
		public XGFilePart GetPart(XGFile aFile, Int64 aSize)
		{
			XGFilePart returnPart = null;

			XGObject[] parts = aFile.Children;
			if (parts.Length == 0 && aSize == 0)
			{
				returnPart = new XGFilePart();
				returnPart.StartSize = aSize;
				returnPart.CurrentSize = aSize;
				returnPart.StopSize = aFile.Size;
				returnPart.IsChecked = true;
				aFile.AddPart(returnPart);
				this.ObjectAddedEvent(aFile, returnPart);
			}
			else
			{
				// first search incomplete parts not in use
				foreach (XGFilePart part in parts)
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
					foreach (XGFilePart part in parts)
					{
						if (part.PartState == FilePartState.Open)
						{
							// split the part
							if (part.StartSize < aSize && part.StopSize > aSize)
							{
								returnPart = new XGFilePart();
								returnPart.StartSize = aSize;
								returnPart.CurrentSize = aSize;
								returnPart.StopSize = part.StopSize;

								// update previous part
								part.StopSize = aSize;
								this.ObjectChangedEvent(part);
								aFile.AddPart(returnPart);
								this.ObjectAddedEvent(aFile, returnPart);
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
		public void RemovePart(XGFile aFile, XGFilePart aPart)
		{
			this.Log("RemovePart(" + aFile.Name + ", " + aFile.Size + ", " + aPart.StartSize + ")", LogLevel.Notice);

			XGObject[] parts = aFile.Children;
			foreach (XGFilePart part in parts)
			{
				if (part.StopSize == aPart.StartSize)
				{
					part.StopSize = aPart.StopSize;
					if (part.PartState == FilePartState.Ready)
					{
						this.Log("RemovePart(" + aFile.Name + ", " + aFile.Size + ", " + aPart.StartSize + ") expanding part " + part.StartSize + " to " + aPart.StopSize, LogLevel.Notice);
						part.PartState = FilePartState.Closed;
						this.ObjectChangedEvent(part);
						break;
					}
				}
			}

			aFile.RemovePart(aPart);
			if (aFile.Children.Length == 0) { this.RemoveFile(aFile); }
			else
			{
				this.FileDelete(this.GetCompletePath(aPart));
				this.ObjectRemovedEvent(aFile, aPart);
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
			XGFile tFile = this.GetFile(aName, aSize);
			if (tFile == null) { return 0; }

			Int64 nextSize = -1;
			XGObject[] parts = tFile.Children;
			if (parts.Length == 0) { nextSize = 0; }
			else
			{
				// first search incomplete parts not in use
				foreach (XGFilePart part in parts)
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
					foreach (XGFilePart part in parts)
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
			this.CheckNextReferenceBytes(pbo.Part, pbo.Bytes, true);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <param name="aBytes"></param>
		/// <returns></returns>
		public Int64 CheckNextReferenceBytes(XGFilePart aPart, byte[] aBytes)
		{
			return this.CheckNextReferenceBytes(aPart, aBytes, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <param name="aBytes"></param>
		/// <param name="aThreaded"></param>
		/// <returns></returns>
		private Int64 CheckNextReferenceBytes(XGFilePart aPart, byte[] aBytes, bool aThreaded)
		{
			XGFile tFile = aPart.Parent;
			XGObject[] parts = tFile.Children;
			this.Log("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ", " + aPart.StopSize + ") with " + parts.Length + " parts called", LogLevel.Notice);

			foreach (XGFilePart part in parts)
			{
				if (part.StartSize == aPart.StopSize)
				{
					// is the part already checked?
					if (part.IsChecked) { break; }

					// the file is open
					if (part.PartState == FilePartState.Open)
					{
						BotConnect bc = null;
						XGPacket pack = null;
						foreach (KeyValuePair<XGPacket, BotConnect> kvp in this.myDownloads)
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
								this.Log("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") removing next part " + part.StartSize, LogLevel.Warning);
								bc.Remove();
								pack.Enabled = false;
								this.RemovePart(tFile, part);
								return part.StopSize;
							}
							else
							{
								this.Log("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is checked", LogLevel.Notice);
								part.IsChecked = true;
								this.ObjectChangedEvent(part);
								return 0;
							}
						}
						else
						{
							this.Log("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is open, but has no bot connect", LogLevel.Error);
							return 0;
						}
					}
					// it is already ready
					else if (part.PartState == FilePartState.Closed || part.PartState == FilePartState.Ready)
					{
						string fileName = this.GetCompletePath(part);
						try
						{
							BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read));
							byte[] bytes = reader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
							reader.Close();

							if (!XGHelper.IsEqual(bytes, aBytes))
							{
								this.Log("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") removing closed part " + part.StartSize, LogLevel.Warning);
								this.RemovePart(tFile, part);
								return part.StopSize;
							}
							else
							{
								part.IsChecked = true;
								this.ObjectChangedEvent(part);

								if (part.PartState == FilePartState.Ready)
								{
									// file is not the last, so check the next one
									if (part.StopSize < tFile.Size)
									{
										FileStream fileStream = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite);
										BinaryReader fileReader = new BinaryReader(fileStream);
										// extract the needed refernce bytes
										fileStream.Seek(-Settings.Instance.FileRollbackCheck, SeekOrigin.End);
										bytes = fileReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
										// and truncate the file
										//fileStream.SetLength(fileStream.Length - Settings.Instance.FileRollbackCheck);
										fileStream.SetLength(part.StopSize - part.StartSize);
										fileReader.Close();

										// dont open a new thread if we are already threaded
										if (aThreaded)
										{
											this.CheckNextReferenceBytes(part, bytes, false);
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
							this.Log("CheckNextReferenceBytes(" + aPart.Parent.Name + ", " + aPart.Parent.Size + ", " + aPart.StartSize + ") handling part " + part.StartSize + ": " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
						}
					}
					else
					{
						this.Log("CheckNextReferenceBytes(" + aPart.Parent.Name + ", " + aPart.Parent.Size + ", " + aPart.StartSize + ") do not know what to do with part " + part.StartSize, LogLevel.Error);
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
		public void CheckFile(XGFile aFile)
		{
			lock (aFile.locked)
			{
				this.Log("CheckFile(" + aFile.Name + ")", LogLevel.Notice);
				if (aFile.Children.Length == 0) { return; }

				bool complete = true;
				XGObject[] parts = aFile.Children;
				foreach (XGFilePart part in parts)
				{
					if (part.PartState != FilePartState.Ready)
					{
						complete = false;
						this.Log("CheckFile(" + aFile.Name + ") part " + part.StartSize + " is not complete", LogLevel.Notice);
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
		/// Joins all parts of a XGFile together by doing this
		/// - disabling all matching packets to avoid the same download again
		/// - merging the file and checking the file size
		/// </summary>
		/// <param name="aObject"></param>
		public void JoinCompleteParts(object aObject)
		{
			XGFile tFile = aObject as XGFile;
			lock (tFile.locked)
			{
				this.Log("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") starting", LogLevel.Notice);

				#region DISABLE ALL MATCHING PACKETS

				// TODO remove all CD* packets if a multi packet was downloaded

				string fileName = XGHelper.ShrinkFileName(tFile.Name, 0);

				foreach (KeyValuePair<XGServer, ServerConnect> kvp in this.myServers)
				{
					XGServer tServ = kvp.Key;
					foreach (XGChannel tChan in tServ.Children)
					{
						if (tChan.Connected)
						{
							foreach (XGBot tBot in tChan.Children)
							{
								foreach (XGPacket tPack in tBot.Children)
								{
									if (tPack.Enabled && (
										XGHelper.ShrinkFileName(tPack.RealName, 0).EndsWith(fileName) ||
										XGHelper.ShrinkFileName(tPack.Name, 0).EndsWith(fileName)
										))
									{
										this.Log("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") disabling packet #" + tPack.Id + " (" + tPack.Name + ") from " + tPack.Parent.Name, LogLevel.Notice);
										tPack.Enabled = false;
										this.ObjectChangedEvent(tPack);
									}
								}
							}
						}
					}
				}

				#endregion

				if (tFile.Children.Length == 0)
				{
					return;
				}

				bool error = true;
				bool deleteOnError = true;
				XGObject[] parts = tFile.Children;
				string fileReady = Settings.Instance.ReadyPath + tFile.Name;

				try
				{
					FileStream stream = File.Open(fileReady, FileMode.Create, FileAccess.Write);
					BinaryWriter writer = new BinaryWriter(stream);
					foreach (XGFilePart part in parts)
					{
						try
						{
							FileStream fs = File.Open(this.GetCompletePath(part), FileMode.Open, FileAccess.Read);
							BinaryReader reader = new BinaryReader(fs);
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
							this.Log("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") handling part " + part.StartSize + ": " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
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

					Int64 size = new FileInfo(fileReady).Length;
					if (size == tFile.Size)
					{
						this.DirectoryDelete(Settings.Instance.TempPath + tFile.TmpPath);
						this.Log("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") file build", LogLevel.Notice);

						// the file is complete and enabled
						tFile.Enabled = true;
						error = false;

						// maybee clear it
						if (Settings.Instance.ClearReadyDownloads)
						{
							this.RemoveFile(tFile);
						}

						// great, all went right, so lets check what we can do with the file
						new Thread(delegate() { this.HandleFile(fileReady); }).Start();
					}
					else
					{
						this.Log("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") filesize is not the same: " + size, LogLevel.Error);
					}
				}
				catch (Exception ex)
				{
					this.Log("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") make: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				}

				if (error && deleteOnError)
				{
					// the creation was not successfull, so delete the files and parts
					this.FileDelete(fileReady);
					this.RemoveFile(tFile);
				}
			}
		}

		/// <summary>
		/// Handles a downloaded file by calling the FileHandler string in the settings file
		/// </summary>
		/// <param name="aFile"></param>
		public void HandleFile(string aFile)
		{
			if (aFile != null && aFile != "")
			{
				int pos = aFile.LastIndexOf('/');
				string folder = aFile.Substring(0, pos);
				string file = aFile.Substring(pos + 1);

				pos = file.LastIndexOf('.');
				string filename = pos != -1 ? file.Substring(0, pos) : file;
				string fileext = pos != -1 ? file.Substring(pos + 1) : "";

				foreach (string line in Settings.InstanceReload.FileHandler)
				{
					if (line == null || line == "") { continue; }
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
								this.Log("HandleFile(" + aFile + ") Process.Start(" + process + ", " + arguments + ") " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
							}
						}
					}
				}
			}
		}

		#endregion

		#endregion

		#region TIMER TASKS

		#region BOT WATCHDOG

		private void RunBotWatchdog()
		{
			while (true)
			{
				Thread.Sleep(Settings.Instance.BotOfflineCheckTime);

				int a = 0;
				foreach (KeyValuePair<XGServer, ServerConnect> kvp in this.myServers)
				{
					if (kvp.Value.IsRunning)
					{
						foreach (XGChannel tChan in kvp.Key.Children)
						{
							if (tChan.Connected)
							{
								foreach (XGBot tBot in tChan.Children)
								{
									if (!tBot.Connected && (DateTime.Now - tBot.LastContact).TotalMilliseconds > Settings.Instance.BotOfflineTime && tBot.getOldestActivePacket() == null)
									{
										a++;
										tChan.RemoveBot(tBot);
										this.ObjectRemovedEvent(tChan, tBot);
									}
								}
							}
						}
					}
				}
				if (a > 0) { this.Log("RunBotWatchdog() removed " + a + " offline bot(s)", LogLevel.Notice); }
			}
		}

		#endregion

		#region TIMER

		private void RunTimer()
		{
			while (true)
			{
				foreach (KeyValuePair<XGServer, ServerConnect> kvp in this.myServers)
				{
					ServerConnect sc = kvp.Value;
					if (sc.IsRunning)
					{
						sc.TriggerTimerRun();
					}
				}

				Thread.Sleep((int)Settings.Instance.TimerSleepTime);
			}
		}

		#endregion

		#endregion

		#region LOG

		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("ServerHandler." + aData, aLevel);
		}

		#endregion
	}

	#region DOWNLOAD OBJECT

	/// <summary>
	/// 
	/// </summary>
	public class DownloadObject
	{
		private XGPacket packet = null;
		public XGPacket Packet
		{
			get { return this.packet; }
			set { this.packet = value; }
		}

		private Int64 startSize = 0;
		public Int64 StartSize
		{
			get { return this.startSize; }
			set { this.startSize = value; }
		}

		private IPAddress ip = IPAddress.Loopback;
		public IPAddress Ip
		{
			get { return this.ip; }
			set { this.ip = value; }
		}

		private int port = 0;
		public int Port
		{
			get { return this.port; }
			set { this.port = value; }
		}

		public DownloadObject(XGPacket aPacket, Int64 aStartSize, IPAddress aIp, int aPort)
		{
			this.packet = aPacket;
			this.startSize = aStartSize;
			this.ip = aIp;
			this.port = aPort;
		}
	}

	#endregion

	#region PARTYBYTES OBJECT

	/// <summary>
	/// 
	/// </summary>
	public class PartBytesObject
	{
		private XGFilePart part;
		public XGFilePart Part
		{
			get { return part; }
			set { part = value; }
		}

		private byte[] bytes;
		public byte[] Bytes
		{
			get { return bytes; }
			set { bytes = value; }
		}

		public PartBytesObject(XGFilePart aPart, byte[] aBytes)
		{
			this.part = aPart;
			this.bytes = aBytes;
		}
	}

	#endregion
}
