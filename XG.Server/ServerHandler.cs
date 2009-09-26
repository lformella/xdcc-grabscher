using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using XG.Core;

namespace XG.Server
{
	public delegate void DownloadDelegate(XGPacket aPack, Int64 aChunk, IPAddress aIp, int aPort);
	public delegate void PacketBotConnectDelegate(XGPacket aPack, BotConnect aCon);

	public class ServerHandler
	{
		private Dictionary<XGServer, ServerConnect> myServers;
		private Dictionary<XGPacket, BotConnect> myDownloads;
		private List<XGFile> myFiles;

		#region DELEGATES

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
		}

		#endregion

		#region SERVER

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aServer"></param>
		public void ConnectServer(object aServer)
		{
			XGServer tServer = aServer as XGServer;
			new Thread(delegate()
			{
				if (!this.myServers.ContainsKey(tServer))
				{
					ServerConnect con = new ServerConnect(this);

					con.NewDownloadEvent += new DownloadDelegate(server_NewDownloadEventHandler);
					con.DisconnectedEvent += new ServerDelegate(server_DisconnectedEventHandler);
					con.ConnectedEvent += new ServerDelegate(server_ConnectedEventHandler);
					con.ParsingErrorEvent += new DataTextDelegate(server_ParsingErrorEventHandler);

					con.ObjectAddedEvent += new ObjectObjectDelegate(_ObjectAddedEventHandler);
					con.ObjectChangedEvent += new ObjectDelegate(_ObjectChangedEventHandler);
					con.ObjectRemovedEvent += new ObjectObjectDelegate(_ObjectRemovedEventHandler);

					this.myServers.Add(tServer, con);
					con.Connect(tServer);
				}
			}).Start();
		}
		void server_ConnectedEventHandler(XGServer aServer)
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
		private void server_DisconnectedEventHandler(XGServer aServer)
		{
			if (this.myServers.ContainsKey(aServer))
			{
				ServerConnect con = this.myServers[aServer];

				con.NewDownloadEvent -= new DownloadDelegate(server_NewDownloadEventHandler);
				con.DisconnectedEvent -= new ServerDelegate(server_DisconnectedEventHandler);
				con.ConnectedEvent -= new ServerDelegate(server_ConnectedEventHandler);
				con.ParsingErrorEvent -= new DataTextDelegate(server_ParsingErrorEventHandler);

				con.ObjectAddedEvent -= new ObjectObjectDelegate(_ObjectAddedEventHandler);
				con.ObjectChangedEvent -= new ObjectDelegate(_ObjectChangedEventHandler);
				con.ObjectRemovedEvent -= new ObjectObjectDelegate(_ObjectRemovedEventHandler);

				this.myServers.Remove(aServer);
			}

			if (aServer.Enabled)
			{
				new Timer(new TimerCallback(this.ConnectServer), aServer, Settings.Instance.ReconnectWaitTime, System.Threading.Timeout.Infinite);
			}
		}

		void server_ParsingErrorEventHandler(string aData) { this.ParsingErrorEvent(aData); }

		void _ObjectRemovedEventHandler(XGObject aParentObj, XGObject aObj) { this.ObjectRemovedEvent(aParentObj, aObj); }

		void _ObjectChangedEventHandler(XGObject aObj) { this.ObjectChangedEvent(aObj); }

		void _ObjectAddedEventHandler(XGObject aParentObj, XGObject aObj) { this.ObjectAddedEvent(aParentObj, aObj); }

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

		private void bot_Connected(XGPacket aPack, BotConnect aCon)
		{
		}
		private void bot_Disconnected(XGPacket aPacket, BotConnect aCon)
		{
			if (myDownloads.ContainsKey(aPacket))
			{
				aCon.DisconnectedEvent -= new PacketBotConnectDelegate(bot_Disconnected);
				aCon.ConnectedEvent -= new PacketBotConnectDelegate(bot_Connected);
				aCon.ObjectChangedEvent -= new ObjectDelegate(_ObjectChangedEventHandler);
				this.myDownloads.Remove(aPacket);

				// if a download is only one part it wil never call the next refbytes function, so the check must be done somewhere else...
				try { this.CheckFile(aCon.Part.Parent); }
				catch (Exception) { }

				try
				{
					ServerConnect sc = this.myServers[aPacket.Parent.Parent.Parent];
					sc.CreatTimer(aPacket.Parent, Settings.Instance.CommandWaitTime);
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
		/// 
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
				if(part.PartState == FilePartState.Open)
				{
					if(part.Packet != null)
					{
						part.Packet.Enabled = false;
						skip = true;
					}
				}
			}
			if(skip) { return; }

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
				aFile.addPart(returnPart);
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
								aFile.addPart(returnPart);
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

			aFile.removePart(aPart);
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

		public void CheckNextReferenceBytes(object aObj)
		{
			PartBytesObject pbo = aObj as PartBytesObject;
			this.CheckNextReferenceBytes(pbo.Part, pbo.Bytes);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <param name="aBytes"></param>
		/// <returns></returns>
		public Int64 CheckNextReferenceBytes(XGFilePart aPart, byte[] aBytes)
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
								this.Log("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is checked", LogLevel.Warning);
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
										fileStream.SetLength(fileStream.Length - Settings.Instance.FileRollbackCheck);
										fileReader.Close();

										//this.CheckNextReferenceBytes(part, bytes);
										new Thread(new ParameterizedThreadStart(CheckNextReferenceBytes)).Start(new PartBytesObject(part, bytes));
									}
									// last file, so check all downloaded files
									// this is done separately
									/*else
									{
										this.Log("CheckNextReferenceBytes(" + aPart.Parent.Name + ", " + aPart.Parent.Size + ", " + aPart.StartSize + ") ready, starting file check", LogLevel.Notice);
										this.CheckFile(tFile);
									}*/
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aObject"></param>
		public void JoinCompleteParts(object aObject)
		{
			XGFile tFile = aObject as XGFile;
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
								if (tPack.Enabled &&
								    (
								     XGHelper.ShrinkFileName(tPack.RealName, 0).EndsWith(fileName) || 
								     XGHelper.ShrinkFileName(tPack.Name, 0).EndsWith(fileName)
								    )
								   )
								{
									this.Log("CreateCompleteFile(" + tFile.Name + ", " + tFile.Size + ") disabling packet #" + tPack.Id + " (" + tPack.Name + ") from " + tPack.Parent.Name, LogLevel.Notice);
									tPack.Enabled = false;
									this.ObjectChangedEvent(tPack);
								}
							}
						}
					}
				}
			}

			#endregion

			if(tFile.Children.Length == 0)
			{
				return;
			}

			bool error = true;
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
						this.Log("CreateCompleteFile(" + tFile.Name + ", " + tFile.Size + ") handling part " + part.StartSize + ": " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
						break;
					}
				}
				writer.Close();
				stream.Close();

				Int64 size = new FileInfo(fileReady).Length;
				if (size == tFile.Size)
				{
					this.DirectoryDelete(Settings.Instance.TempPath + tFile.TmpPath);
					this.Log("CreateCompleteFile(" + tFile.Name + ", " + tFile.Size + ") file build", LogLevel.Notice);

					// the file is complete and enabled
					tFile.Enabled = true;
					error = false;

					// TODO check if we should run this here
					if(Settings.Instance.ClearReadyDownloads)
					{
						this.RemoveFile(tFile);
					}

					// great, all went right, so lets check what we can do with the file
					//TODO fix this
					//new Thread(delegate() { this.HandleFile(fileReady); }).Start();
				}
				else
				{
					this.Log("CreateCompleteFile(" + tFile.Name + ", " + tFile.Size + ") filesize is not the same: " + size, LogLevel.Error);
				}
			}
			catch (Exception ex)
			{
				this.Log("CreateCompleteFile(" + tFile.Name + ", " + tFile.Size + ") make: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
			if(error)
			{
				// the creation was not successfull, so delete the files and parts
				this.FileDelete(fileReady);
				this.RemoveFile(tFile);
			}
		}

		public void HandleFile(string aFile)
		{
			if(aFile != null && aFile != "")
			{
				int pos = aFile.LastIndexOf('.');
				if(pos > 0)
				{
					string extension = aFile.Substring(pos + 1);

					foreach(string line in Settings.Instance.FileHandler)
					{
						if(line == null || line == "") { continue; }

						string[] values = line.Split('#');
						if(values[0] == extension)
						{
							int pos2 = aFile.LastIndexOf('/');
							string folder = aFile.Substring(0, pos2);
							string name = aFile.Substring(pos2 + 1, pos - pos2 - 1);

							values[2] = values[2].Replace("%FILE%", aFile);
							values[2] = values[2].Replace("%FOLDER%", folder);
							values[2] = values[2].Replace("%EXTENSION%", extension);
							values[2] = values[2].Replace("%FILENAME%", name);

							try
							{
								Process p = Process.Start(values[1], values[2]);
								p.WaitForExit();

								if(Directory.Exists(aFile))
								{
									foreach(string file in Directory.GetFiles(aFile))
									{
										this.HandleFile(file);
									}
								}
							}
							catch {}

							break;
						}
					}
				}
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
