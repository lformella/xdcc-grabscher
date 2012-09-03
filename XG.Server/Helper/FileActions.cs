// 
//  FileActions.cs
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
using System.Text.RegularExpressions;
using System.Threading;

using log4net;

using XG.Core;

namespace XG.Server.Helper
{
	public class FileActions
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(FileActions));

		XG.Core.Servers _servers;
		public XG.Core.Servers Servers
		{
			set { _servers = value; }
		}

		Files _files;
		public Files Files
		{
			set { _files = value; }
		}

		#endregion

		#region HELPER

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aFile"></param>
		/// <returns></returns>
		public string CompletePath(File aFile)
		{
			return Settings.Instance.TempPath + aFile.TmpPath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aPart"></param>
		/// <returns></returns>
		public string CompletePath(FilePart aPart)
		{
			return CompletePath(aPart.Parent) + aPart.StartSize;
		}

		#endregion

		#region FILE

		/// <summary>
		/// Returns a file or null if there isnt one
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aSize"></param>
		/// <returns></returns>
		File File(string aName, Int64 aSize)
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
						_log.Warn("File(" + aName + ", " + aSize + ") directory " + file.TmpPath + " is missing ");
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
		public File NewFile(string aName, Int64 aSize)
		{
			File tFile = File(aName, aSize);
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
					_log.Fatal("NewFile()", ex);
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

			FileSystem.DeleteDirectory(CompletePath(aFile));
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
		public FilePart Part(File aFile, Int64 aSize)
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
				FileSystem.DeleteFile(CompletePath(aPart));
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
		public Int64 NextAvailablePartSize(string aName, Int64 aSize)
		{
			File tFile = File(aName, aSize);
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
		void CheckNextReferenceBytes(object aObj)
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
						if (part.Packet != null)
						{
							if (!XGHelper.IsEqual(part.StartReference, aBytes))
							{
								_log.Warn("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") removing next part " + part.StartSize);
								part.Packet.Enabled = false;
								part.Packet.Commit();
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
							_log.Error("CheckNextReferenceBytes(" + tFile.Name + ", " + tFile.Size + ", " + aPart.StartSize + ") part " + part.StartSize + " is open, but has no packet");
							return 0;
						}
					}
					// it is already ready
					else if (part.PartState == FilePartState.Closed || part.PartState == FilePartState.Ready)
					{
						string fileName = CompletePath(part);
						try
						{
							System.IO.BinaryReader reader = FileSystem.OpenFileReadable(fileName);
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
		void JoinCompleteParts(object aObject)
		{
			File tFile = aObject as File;
			lock (tFile.Locked)
			{
				_log.Info("JoinCompleteParts(" + tFile.Name + ", " + tFile.Size + ") starting");

				#region DISABLE ALL MATCHING PACKETS

				// TODO remove all CD* packets if a multi packet was downloaded

				string fileName = XGHelper.ShrinkFileName(tFile.Name, 0);

				foreach (XG.Core.Server tServ in _servers.All)
				{
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
							System.IO.BinaryReader reader = FileSystem.OpenFileReadable(CompletePath(part));
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
						FileSystem.DeleteDirectory(Settings.Instance.TempPath + tFile.TmpPath);
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
					FileSystem.DeleteFile(fileReady);
					RemoveFile(tFile);
				}
			}
		}

		/// <summary>
		/// Handles a downloaded file by calling the FileHandler string in the settings file
		/// </summary>
		/// <param name="aFile"></param>
		void HandleFile(string aFile)
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
	}
}
