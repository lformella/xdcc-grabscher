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
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

using log4net;

using XG.Core;
using XG.Server.Connection;
using XG.Server.Helper;

namespace XG.Server
{
	public delegate void DownloadDelegate(XGPacket aPack, Int64 aChunk, IPAddress aIp, int aPort);
	public delegate void PacketBotConnectDelegate(XGPacket aPack, BotConnection aCon);

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

		private static readonly ILog myLog = LogManager.GetLogger(typeof(ServerHandler));

		private IrcParser ircParser;
		public IrcParser IrcParser
		{
			set
			{
				if(this.ircParser != null)
				{
					this.ircParser.Parent = null;
					this.ircParser.AddDownloadEvent -= new DownloadDelegate (IrcParser_AddDownloadEventHandler);
					this.ircParser.RemoveDownloadEvent -= new BotDelegate (IrcParser_RemoveDownloadEventHandler);
				}
				this.ircParser = value;
				if(this.ircParser != null)
				{
					this.ircParser.Parent = this;
					this.ircParser.AddDownloadEvent += new DownloadDelegate (IrcParser_AddDownloadEventHandler);
					this.ircParser.RemoveDownloadEvent += new BotDelegate (IrcParser_RemoveDownloadEventHandler);
				}
			}
		}

		private XG.Core.Repository.File fileRepository;
		public XG.Core.Repository.File FileRepository
		{
			set
			{
				if(this.fileRepository != null)
				{
				}
				this.fileRepository = value;
				if(this.fileRepository != null)
				{
				}
			}
		}

		private Dictionary<XGServer, ServerConnection> servers;
		private Dictionary<XGPacket, BotConnection> downloads;

		#endregion

		#region INIT

		public ServerHandler()
		{
			this.servers = new Dictionary<XGServer, ServerConnection>();
			this.downloads = new Dictionary<XGPacket, BotConnection>();

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
		/// Connects to the given server by using a new ServerConnnect class
		/// </summary>
		/// <param name="aServer"></param>
		public void ConnectServer (XGServer aServer)
		{
			if (!this.servers.ContainsKey (aServer))
			{
				ServerConnection con = new ServerConnection ();
				con.Parent = this;
				con.Server = aServer;
				con.IrcParser = this.ircParser;

				con.Connection = new XG.Server.Connection.Connection();
				con.Connection.Hostname = aServer.Name;
				con.Connection.Port = aServer.Port;
				con.Connection.MaxData = 0;

				this.servers.Add (aServer, con);

				con.ConnectedEvent += new ServerDelegate(ServerConnection_ConnectedEventHandler);
				con.DisconnectedEvent += new ServerSocketErrorDelegate(ServerConnection_DisconnectedEventHandler);

				// start a new thread wich connects to the given server
				new Thread(delegate() { con.Connection.Connect(); }).Start();
			}
			else
			{
				myLog.Error("ConnectServer(" + aServer.Name + ") server is already in the dictionary");
			}
		}
		private void ServerConnection_ConnectedEventHandler(XGServer aServer)
		{
			// nom nom nom ...
		}

		/// <summary>
		/// Disconnects the given server
		/// </summary>
		/// <param name="aServer"></param>
		public void DisconnectServer(XGServer aServer)
		{
			if (this.servers.ContainsKey(aServer))
			{
				ServerConnection con = servers[aServer];
				con.Connection.Disconnect();
			}
			else
			{
				myLog.Error("DisconnectServer(" + aServer.Name + ") server is not in the dictionary");
			}
		}
		private void ServerConnection_DisconnectedEventHandler(XGServer aServer, SocketErrorCode aValue)
		{
			if (this.servers.ContainsKey (aServer))
			{
				ServerConnection con = this.servers[aServer];

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
						new Timer(new TimerCallback(this.ReconnectServer), aServer, time, System.Threading.Timeout.Infinite);
					}
				}
				else
				{
					con.ConnectedEvent -= new ServerDelegate(ServerConnection_ConnectedEventHandler);
					con.DisconnectedEvent -= new ServerSocketErrorDelegate(ServerConnection_DisconnectedEventHandler);

					con.Server = null;
					con.IrcParser = null;

					this.servers.Remove(aServer);
				}

				con.Connection = null;
			}
			else
			{
				myLog.Error("server_DisconnectedEventHandler(" + aServer.Name + ", " + aValue + ") server is not in the dictionary");
			}
		}

		private void ReconnectServer(object aServer)
		{
			XGServer tServer = aServer as XGServer;

			if (this.servers.ContainsKey(tServer))
			{
				ServerConnection con = this.servers[tServer];

				if (tServer.Enabled)
				{
					myLog.Error("ReconnectServer(" + tServer.Name + ")");

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
				myLog.Error("ReconnectServer(" + tServer.Name + ") server is not in the dictionary");
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
		private void IrcParser_AddDownloadEventHandler(XGPacket aPack, Int64 aChunk, IPAddress aIp, int aPort)
		{
			new Thread(delegate()
			{
				if (!this.downloads.ContainsKey(aPack))
				{
					BotConnection con = new BotConnection();
					con.Parent = this;
					con.Packet = aPack;
					con.StartSize = aChunk;

					con.Connection = new XG.Server.Connection.Connection();
					con.Connection.Hostname = aIp.ToString();
					con.Connection.Port = aPort;
					con.Connection.MaxData = aPack.RealSize - aChunk;

					con.DisconnectedEvent += new PacketBotConnectDelegate(Bot_Disconnected);
					con.ConnectedEvent += new PacketBotConnectDelegate(Bot_Connected);

					this.downloads.Add(aPack, con);
					con.Connection.Connect();
				}
				else
				{
					// uhh - that should not happen
					myLog.Error("StartDownload(" + aPack.Name + ") is already downloading");
				}
			}).Start();
		}
		private void Bot_Connected (XGPacket aPack, BotConnection aCon)
		{
		}

		private void IrcParser_RemoveDownloadEventHandler (XGBot aBot)
		{
			foreach (KeyValuePair<XGPacket, BotConnection> kvp in this.downloads)
			{
				if (kvp.Key.Parent == aBot)
				{
					kvp.Value.Connection.Disconnect();
					break;
				}
			}
		}
		private void Bot_Disconnected(XGPacket aPacket, BotConnection aCon)
		{
			aCon.Packet = null;
			aCon.Connection = null;

			if (downloads.ContainsKey(aPacket))
			{
				aCon.DisconnectedEvent -= new PacketBotConnectDelegate(Bot_Disconnected);
				aCon.ConnectedEvent -= new PacketBotConnectDelegate(Bot_Connected);
				this.downloads.Remove(aPacket);

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
					myLog.Fatal("bot_Disconnected()", ex);
				}

				try
				{
					ServerConnection sc = this.servers[aPacket.Parent.Parent.Parent];
					sc.CreateTimer(aPacket.Parent, Settings.Instance.CommandWaitTime, false);
				}
				catch (Exception ex)
				{
					myLog.Fatal("bot_Disconnected() request", ex);
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
			foreach (XGFile file in this.fileRepository.Files)
			{
				//Console.WriteLine(file.TmpPath + " - " + name);
				if (file.TmpPath == name)
				{
					// lets check if the directory is still on the harddisk
					if(!Directory.Exists(Settings.Instance.TempPath + file.TmpPath))
					{
						myLog.Warn("GetFile(" + aName + ", " + aSize + ") directory " + file.TmpPath + " is missing ");
						this.RemoveFile(file);
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
		public XGFile GetNewFile(string aName, Int64 aSize)
		{
			XGFile tFile = this.GetFile(aName, aSize);
			if (tFile == null)
			{
				tFile = new XGFile(aName, aSize);
				this.fileRepository.AddFile(tFile);
				try
				{
					Directory.CreateDirectory(Settings.Instance.TempPath + tFile.TmpPath);
				}
				catch (Exception ex)
				{
					myLog.Fatal("GetNewFile()", ex);
					tFile = null;
				}
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
			myLog.Info("RemoveFile(" + aFile.Name + ", " + aFile.Size + ")");

			// check if this file is currently downloaded
			bool skip = false;
			foreach (XGFilePart part in aFile.Parts)
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

			Helper.Filesystem.DeleteDirectory(this.GetCompletePath(aFile));
			this.fileRepository.RemoveFile(aFile);
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

			IEnumerable<XGFilePart> parts = aFile.Parts;
			if (parts.Count() == 0 && aSize == 0)
			{
				returnPart = new XGFilePart();
				returnPart.StartSize = aSize;
				returnPart.CurrentSize = aSize;
				returnPart.StopSize = aFile.Size;
				returnPart.IsChecked = true;
				aFile.AddPart(returnPart);
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
								part.Commit();
								aFile.AddPart(returnPart);
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
			myLog.Info("RemovePart(" + aFile.Name + ", " + aFile.Size + ", " + aPart.StartSize + ")");

			IEnumerable<XGFilePart> parts = aFile.Parts;
			foreach (XGFilePart part in parts)
			{
				if (part.StopSize == aPart.StartSize)
				{
					part.StopSize = aPart.StopSize;
					if (part.PartState == FilePartState.Ready)
					{
						myLog.Info("RemovePart(" + aFile.Name + ", " + aFile.Size + ", " + aPart.StartSize + ") expanding part " + part.StartSize + " to " + aPart.StopSize);
						part.PartState = FilePartState.Closed;
						part.Commit();
						break;
					}
				}
			}

			aFile.RemovePart(aPart);
			if (aFile.Parts.Count() == 0) { this.RemoveFile(aFile); }
			else
			{
				Helper.Filesystem.DeleteFile(this.GetCompletePath(aPart));
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
			IEnumerable<XGFilePart> parts = tFile.Parts;
			if (parts.Count() == 0) { nextSize = 0; }
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
			IEnumerable<XGFilePart> parts = tFile.Parts;
			myLog.Info("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ", " + aPart.StopSize + ") with " + parts.Count() + " parts called");

			foreach (XGFilePart part in parts)
			{
				if (part.StartSize == aPart.StopSize)
				{
					// is the part already checked?
					if (part.IsChecked) { break; }

					// the file is open
					if (part.PartState == FilePartState.Open)
					{
						BotConnection bc = null;
						XGPacket pack = null;
						foreach (KeyValuePair<XGPacket, BotConnection> kvp in this.downloads)
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
								myLog.Warn("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") removing next part " + part.StartSize);
								bc.RemovePart = true;
								bc.Connection.Disconnect();
								pack.Enabled = false;
								this.RemovePart(tFile, part);
								return part.StopSize;
							}
							else
							{
								myLog.Info("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is checked");
								part.IsChecked = true;
								part.Commit();
								return 0;
							}
						}
						else
						{
							myLog.Error("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is open, but has no bot connect");
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
								myLog.Warn("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") removing closed part " + part.StartSize);
								this.RemovePart(tFile, part);
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
							myLog.Fatal("CheckNextReferenceBytes(" + aPart.Parent.Name + ", " + aPart.Parent.Size + ", " + aPart.StartSize + ") handling part " + part.StartSize + "", ex);
						}
					}
					else
					{
						myLog.Error("CheckNextReferenceBytes(" + aPart.Parent.Name + ", " + aPart.Parent.Size + ", " + aPart.StartSize + ") do not know what to do with part " + part.StartSize);
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
			lock (aFile.Locked)
			{
				myLog.Info("CheckFile(" + aFile.Name + ")");
				if (aFile.Parts.Count() == 0) { return; }

				bool complete = true;
				IEnumerable<XGFilePart> parts = aFile.Parts;
				foreach (XGFilePart part in parts)
				{
					if (part.PartState != FilePartState.Ready)
					{
						complete = false;
						myLog.Info("CheckFile(" + aFile.Name + ") part " + part.StartSize + " is not complete");
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
			lock (tFile.Locked)
			{
				myLog.Info("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") starting");

				#region DISABLE ALL MATCHING PACKETS

				// TODO remove all CD* packets if a multi packet was downloaded

				string fileName = XGHelper.ShrinkFileName(tFile.Name, 0);

				foreach (KeyValuePair<XGServer, ServerConnection> kvp in this.servers)
				{
					XGServer tServ = kvp.Key;
					foreach (XGChannel tChan in tServ.Channels)
					{
						if (tChan.Connected)
						{
							foreach (XGBot tBot in tChan.Bots)
							{
								foreach (XGPacket tPack in tBot.Packets)
								{
									if (tPack.Enabled && (
										XGHelper.ShrinkFileName(tPack.RealName, 0).EndsWith(fileName) ||
										XGHelper.ShrinkFileName(tPack.Name, 0).EndsWith(fileName)
										))
									{
										myLog.Info("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") disabling packet #" + tPack.Id + " (" + tPack.Name + ") from " + tPack.Parent.Name);
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
				IEnumerable<XGFilePart> parts = tFile.Parts;
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
							myLog.Fatal("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") handling part " + part.StartSize + "", ex);
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
						Helper.Filesystem.DeleteDirectory(Settings.Instance.TempPath + tFile.TmpPath);
						myLog.Info("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") file build");

						// statistics
						Statistic.Instance.Increase(StatisticType.FilesCompleted);

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
						myLog.Error("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") filesize is not the same: " + size);

						// statistics
						Statistic.Instance.Increase(StatisticType.FilesBroken);
					}
				}
				catch (Exception ex)
				{
					myLog.Fatal("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") make", ex);

					// statistics
					Statistic.Instance.Increase(StatisticType.FilesBroken);
				}

				if (error && deleteOnError)
				{
					// the creation was not successfull, so delete the files and parts
					Helper.Filesystem.DeleteFile(fileReady);
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
								myLog.Fatal("HandleFile(" + aFile + ") Process.Start(" + process + ", " + arguments + ")", ex);
							}
						}
					}
				}
			}
		}

		#endregion

		#endregion

		#region TIMER TASKS

		private void RunBotWatchdog()
		{
			while (true)
			{
				Thread.Sleep(Settings.Instance.BotOfflineCheckTime);

				int a = 0;
				foreach (KeyValuePair<XGServer, ServerConnection> kvp in this.servers)
				{
					if (kvp.Value.IsRunning)
					{
						foreach (XGChannel tChan in kvp.Key.Channels)
						{
							if (tChan.Connected)
							{
								foreach (XGBot tBot in tChan.Bots)
								{
									if (!tBot.Connected && (DateTime.Now - tBot.LastContact).TotalMilliseconds > Settings.Instance.BotOfflineTime && tBot.GetOldestActivePacket() == null)
									{
										a++;
										tChan.RemoveBot(tBot);
									}
								}
							}
						}
					}
				}
				if (a > 0) { myLog.Info("RunBotWatchdog() removed " + a + " offline bot(s)"); }

				// TODO scan for empty channels and send a "xdcc list" command to all the people in there
				// in some channels the bots are silent and have the same (no) rights like normal users
			}
		}

		private void RunTimer()
		{
			while (true)
			{
				foreach (KeyValuePair<XGServer, ServerConnection> kvp in this.servers)
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
