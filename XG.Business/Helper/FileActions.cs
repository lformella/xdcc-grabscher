// 
//  FileActions.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using XG.Model;
using XG.Model.Domain;
using XG.Config.Properties;

namespace XG.Business.Helper
{
	public class FileActions
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static Servers Servers { get; set; }

		static Files _files;
		public static Files Files
		{
			get { return _files; }
			set
			{
				if (_files != null)
				{
					_files.OnRemoved -= OnRemoveFile;
				}
				_files = value;
				if (_files != null)
				{
					_files.OnRemoved += OnRemoveFile;
				}
			}
		}

		#endregion

		#region EVENTS

		public static event EventHandler<EventArgs<Notification>> OnNotificationAdded;

		public static void FireNotificationAdded(Notification.Types aType, AObject aObject)
		{
			if (OnNotificationAdded != null)
			{
				OnNotificationAdded(null, new EventArgs<Notification>(new Notification(aType, aObject)));
			}
		}

		#endregion

		#region HELPER

		/// <summary>
		/// </summary>
		/// <param name="aFile"> </param>
		/// <returns> </returns>
		public static string CompletePath(XG.Model.Domain.File aFile)
		{
			return Settings.Default.TempPath + aFile.TmpPath;
		}

		/// <summary>
		/// </summary>
		/// <param name="aPart"> </param>
		/// <returns> </returns>
		public static string CompletePath(FilePart aPart)
		{
			return CompletePath(aPart.Parent) + aPart.StartSize;
		}

		#endregion

		#region FILE

		/// <summary>
		/// 	Returns a file or null if there isnt one
		/// </summary>
		/// <param name="aName"> </param>
		/// <param name="aSize"> </param>
		/// <returns> </returns>
		static XG.Model.Domain.File File(string aName, Int64 aSize)
		{
			string name = XG.Model.Domain.Helper.ShrinkFileName(aName, aSize);
			foreach (var file in Files.All)
			{
				//Console.WriteLine(file.TmpPath + " - " + name);
				if (file.TmpPath == name)
				{
					// lets check if the directory is still on the harddisk
					if (!Directory.Exists(Settings.Default.TempPath + file.TmpPath))
					{
						Log.Warn("File(" + aName + ", " + aSize + ") directory of " + file + " is missing ");
						RemoveFile(file);
						break;
					}
					return file;
				}
			}
			return null;
		}

		/// <summary>
		/// 	Returns a file - an old if it is already there, or a new
		/// </summary>
		/// <param name="aName"> </param>
		/// <param name="aSize"> </param>
		/// <returns> </returns>
		public static XG.Model.Domain.File NewFile(string aName, Int64 aSize)
		{
			var tFile = File(aName, aSize);
			if (tFile == null)
			{
				tFile = new XG.Model.Domain.File(aName, aSize);
				Files.Add(tFile);
				try
				{
					Directory.CreateDirectory(Settings.Default.TempPath + tFile.TmpPath);
				}
				catch (Exception ex)
				{
					Log.Fatal("NewFile()", ex);
					tFile = null;
				}
			}
			return tFile;
		}

		/// <summary>
		/// 	removes a File
		/// 	stops all running part connections and removes the file
		/// </summary>
		static void OnRemoveFile(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			if (aEventArgs.Value2 is XG.Model.Domain.File)
			{
				RemoveFile((XG.Model.Domain.File)aEventArgs.Value2);
			}
			else if (aEventArgs.Value2 is FilePart)
			{
				RemovePart((XG.Model.Domain.File)aEventArgs.Value2.Parent, (FilePart)aEventArgs.Value2);
			}
		}

		/// <summary>
		/// 	removes a File
		/// 	stops all running part connections and removes the file
		/// </summary>
		static void RemoveFile(XG.Model.Domain.File aFile)
		{
			Log.Info("RemoveFile(" + aFile + ")");

			// check if this file is currently downloaded
			bool skip = false;
			foreach (FilePart part in aFile.Parts)
			{
				// disable the packet if it is active
				if (part.State == FilePart.States.Open)
				{
					if (part.Packet != null)
					{
						part.Packet.Enabled = false;
						skip = true;
					}
				}
			}
			if (skip)
			{
				return;
			}

			FileSystem.DeleteDirectory(CompletePath(aFile));
		}

		#endregion

		#region PART

		/// <summary>
		/// </summary>
		/// <param name="aFile"> </param>
		/// <param name="aSize"> </param>
		/// <returns> </returns>
		public static FilePart Part(XG.Model.Domain.File aFile, Int64 aSize)
		{
			FilePart returnPart = null;

			IEnumerable<FilePart> parts = aFile.Parts;
			if (!parts.Any() && aSize == 0)
			{
				returnPart = new FilePart {StartSize = aSize, CurrentSize = aSize, StopSize = aFile.Size, Checked = true};
				aFile.Add(returnPart);
			}
			else
			{
				// first search incomplete parts not in use
				foreach (FilePart part in parts)
				{
					Int64 size = part.CurrentSize - Settings.Default.FileRollbackBytes;
					if (part.State == FilePart.States.Closed && (size < 0 ? 0 : size) == aSize)
					{
						returnPart = part;
						break;
					}
				}
				// if multi dling is enabled
				if (returnPart == null && Settings.Default.EnableMultiDownloads)
				{
					// now search incomplete parts in use
					foreach (FilePart part in parts)
					{
						if (part.State == FilePart.States.Open)
						{
							// split the part
							if (part.StartSize < aSize && part.StopSize > aSize)
							{
								returnPart = new FilePart {StartSize = aSize, CurrentSize = aSize, StopSize = part.StopSize};

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
		/// </summary>
		/// <param name="aFile"> </param>
		/// <param name="aPart"> </param>
		public static void RemovePart(XG.Model.Domain.File aFile, FilePart aPart)
		{
			Log.Info("RemovePart(" + aFile + ", " + aPart + ")");

			IEnumerable<FilePart> parts = aFile.Parts;
			foreach (FilePart part in parts)
			{
				if (part.StopSize == aPart.StartSize)
				{
					part.StopSize = aPart.StopSize;
					if (part.State == FilePart.States.Ready)
					{
						Log.Info("RemovePart(" + aFile + ", " + aPart + ") expanding " + part.StartSize + " to " + aPart.StopSize);
						part.State = FilePart.States.Closed;
						part.Commit();
						break;
					}
				}
			}

			aFile.Remove(aPart);
			if (!aFile.Parts.Any())
			{
				RemoveFile(aFile);
			}
			else
			{
				FileSystem.DeleteFile(CompletePath(aPart));
			}
		}

		#endregion

		#region GET NEXT STUFF

		/// <summary>
		/// 	Returns the next chunk of a file (subtracts already the rollback value for continued downloads)
		/// </summary>
		/// <param name="aName"> </param>
		/// <param name="aSize"> </param>
		/// <returns> -1 if there is no part, 0 or greater if there is a new part available </returns>
		public static Int64 NextAvailablePartSize(string aName, Int64 aSize)
		{
			var tFile = File(aName, aSize);
			if (tFile == null)
			{
				return 0;
			}

			Int64 nextSize = -1;
			IEnumerable<FilePart> parts = tFile.Parts;
			if (!parts.Any())
			{
				nextSize = 0;
			}
			else
			{
				// first search incomplete parts not in use
				foreach (FilePart part in parts)
				{
					if (part.State == FilePart.States.Closed)
					{
						nextSize = part.CurrentSize - Settings.Default.FileRollbackBytes;
						// uhm, this is a bug if we have a very small downloaded file
						// so just return 0
						if (nextSize < 0)
						{
							nextSize = 0;
						}
						break;
					}
				}
				// if multi downloading is enabled
				if (nextSize == -1 && Settings.Default.EnableMultiDownloads)
				{
					Int64 timeMissing = 0;
					Int64 startChunk = 0;

					// now search incomplete parts in use
					foreach (FilePart part in parts)
					{
						if (part.State == FilePart.States.Open)
						{
							// find the file with the max time, but only if there is some space to download
							if (timeMissing < part.TimeMissing && part.MissingSize > 4 * Settings.Default.FileRollbackBytes)
							{
								timeMissing = part.TimeMissing;
								// and divide the missing size in two parts
								startChunk = part.StopSize - (part.MissingSize / 2);
							}
						}
					}

					// only try a new download if there is some time
					if (timeMissing > Settings.Default.MutliDownloadMinimumTime)
					{
						nextSize = startChunk;
					}
				}
			}
			return nextSize;
		}

		/// <summary>
		/// </summary>
		/// <param name="aObj"> </param>
		static void CheckNextReferenceBytes(object aObj)
		{
			var pbo = aObj as PartBytesObject;
			if (pbo != null)
			{
				CheckNextReferenceBytes(pbo.Part, pbo.Bytes, true);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="aPart"> </param>
		/// <param name="aBytes"> </param>
		/// <returns> </returns>
		public static Int64 CheckNextReferenceBytes(FilePart aPart, byte[] aBytes)
		{
			return CheckNextReferenceBytes(aPart, aBytes, false);
		}

		/// <summary>
		/// </summary>
		/// <param name="aPart"> </param>
		/// <param name="aBytes"> </param>
		/// <param name="aThreaded"> </param>
		/// <returns> </returns>
		static Int64 CheckNextReferenceBytes(FilePart aPart, byte[] aBytes, bool aThreaded)
		{
			var tFile = aPart.Parent;
			var parts = tFile.Parts;
			Log.Info("CheckNextReferenceBytes(" + tFile + ", " + aPart + ") with " + parts.Count() + " parts called");

			foreach (FilePart part in parts)
			{
				if (part.StartSize == aPart.StopSize)
				{
					// is the part already checked?
					if (part.Checked)
					{
						break;
					}

					// the file is open
					if (part.State == FilePart.States.Open)
					{
						if (part.Packet != null)
						{
							if (!part.StartReference.IsEqualWith(aBytes))
							{
								Log.Warn("CheckNextReferenceBytes(" + tFile + ", " + aPart + ") removing next " + part);
								part.Packet.Enabled = false;
								RemovePart(tFile, part);
								return part.StopSize;
							}
							else
							{
								Log.Info("CheckNextReferenceBytes(" + tFile + ", " + aPart + ") " + part + " is checked");
								part.Checked = true;
								part.Commit();
								return 0;
							}
						}
						else
						{
							Log.Error("CheckNextReferenceBytes(" + tFile + ", " + aPart + ") " + part + " is open, but has no packet");
							return 0;
						}
					}
						// it is already ready
					if (part.State == FilePart.States.Closed || part.State == FilePart.States.Ready)
					{
						string fileName = CompletePath(part);
						try
						{
							byte[] bytes = null;
							using (var stream = System.IO.File.Open(fileName, FileMode.Open, FileAccess.Read))
							{
								var reader = new BinaryReader(stream);
								bytes = reader.ReadBytes(Settings.Default.FileRollbackCheckBytes);
								reader.Close();
							}

							if (!bytes.IsEqualWith(aBytes))
							{
								Log.Warn("CheckNextReferenceBytes(" + tFile + ", " + aPart + ") removing closed " + part);
								RemovePart(tFile, part);
								return part.StopSize;
							}
							else
							{
								part.Checked = true;
								part.Commit();

								if (part.State == FilePart.States.Ready)
								{
									// file is not the last, so check the next one
									if (part.StopSize < tFile.Size)
									{
										using (FileStream fileStream = System.IO.File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
										{
											var fileReader = new BinaryReader(fileStream);
											// extract the needed refernce bytes
											fileStream.Seek(-Settings.Default.FileRollbackCheckBytes, SeekOrigin.End);
											bytes = fileReader.ReadBytes(Settings.Default.FileRollbackCheckBytes);
											// and truncate the file
											//fileStream.SetLength(fileStream.Length - Settings.Default.FileRollbackCheckBytes);
											fileStream.SetLength(part.StopSize - part.StartSize);
											fileReader.Close();
										}

										// dont open a new thread if we are already threaded
										if (aThreaded)
										{
											CheckNextReferenceBytes(part, bytes, false);
										}
										else
										{
											new Thread(CheckNextReferenceBytes).Start(new PartBytesObject{ Part = part, Bytes = bytes});
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							Log.Fatal("CheckNextReferenceBytes(" + aPart.Parent + ", " + aPart + ") handling " + part + "", ex);
						}
					}
					else
					{
						Log.Error("CheckNextReferenceBytes(" + aPart.Parent + ", " + aPart + ") do not know what to do with " + part);
					}

					break;
				}
			}
			return 0;
		}

		#endregion

		#region CHECK AND JOIN FILE

		/// <summary>
		/// 	Checks a file and if it is complete it starts a thread which join the file
		/// </summary>
		/// <param name="aFile"> </param>
		public static void CheckFile(XG.Model.Domain.File aFile)
		{
			lock (aFile)
			{
				Log.Info("CheckFile(" + aFile + ")");
				if (!aFile.Parts.Any())
				{
					return;
				}

				bool complete = true;
				IEnumerable<FilePart> parts = aFile.Parts;
				foreach (FilePart part in parts)
				{
					if (part.State != FilePart.States.Ready)
					{
						complete = false;
						Log.Info("CheckFile(" + aFile + ") " + part + " is not complete");
						break;
					}
				}
				if (complete)
				{
					new Thread(JoinCompleteParts).Start(aFile);
				}
			}
		}

		/// <summary>
		/// 	Joins all parts of a File together by doing this
		/// 	- disabling all matching packets to avoid the same download again
		/// 	- merging the file and checking the file size
		/// </summary>
		/// <param name="aObject"> </param>
		static void JoinCompleteParts(object aObject)
		{
			var tFile = aObject as XG.Model.Domain.File;
			if (tFile != null)
			{
				lock (tFile)
				{
					Log.Info("JoinCompleteParts(" + tFile + ") starting");

					#region DISABLE ALL MATCHING PACKETS

					// TODO remove all CD* packets if a multi packet was downloaded

					string fileName = XG.Model.Domain.Helper.ShrinkFileName(tFile.Name, 0);

					foreach (Server tServ in Servers.All)
					{
						foreach (Channel tChan in tServ.Channels)
						{
							if (tChan.Connected)
							{
								foreach (Bot tBot in tChan.Bots)
								{
									foreach (Packet tPack in tBot.Packets)
									{
										if (tPack.Enabled && (XG.Model.Domain.Helper.ShrinkFileName(tPack.RealName, 0).EndsWith(fileName) || XG.Model.Domain.Helper.ShrinkFileName(tPack.Name, 0).EndsWith(fileName)))
										{
											Log.Info("JoinCompleteParts(" + tFile + ") disabling " + tPack + " from " + tPack.Parent);
											tPack.Enabled = false;
										}
									}
								}
							}
						}
					}

					#endregion

					if (!tFile.Parts.Any())
					{
						return;
					}

					bool buildComplete = false;
					IEnumerable<FilePart> parts = tFile.Parts;
					string fileReady = Settings.Default.ReadyPath + tFile.Name;

					try
					{
						using (var stream = System.IO.File.Open(fileReady, FileMode.Create, FileAccess.Write))
						{
							var writer = new BinaryWriter(stream);
							foreach (FilePart part in parts)
							{
								try
								{
									using (var stream2 = System.IO.File.Open(CompletePath(part), FileMode.Open, FileAccess.Read))
									{
										var reader = new BinaryReader(stream2);
										byte[] data;
										while ((data = reader.ReadBytes(Settings.Default.DownloadPerReadBytes)).Length > 0)
										{
											writer.Write(data);
											writer.Flush();
										}
										reader.Close();
									}
								}
								catch (Exception ex)
								{
									Log.Fatal("JoinCompleteParts(" + tFile + ") handling " + part + "", ex);
									break;
								}
							}
							writer.Close();
							stream.Close();
						}

						Int64 size = new FileInfo(fileReady).Length;
						if (size == tFile.Size)
						{
							FileSystem.DeleteDirectory(Settings.Default.TempPath + tFile.TmpPath);
							Log.Info("JoinCompleteParts(" + tFile + ") build");

							FireNotificationAdded(Notification.Types.FileCompleted, tFile);

							// the file is complete and enabled
							tFile.Enabled = true;
							buildComplete = true;

							// clear it
							RemoveFile(tFile);

							// great, all went right, so lets check what we can do with the file
							new Thread(() => HandleFile(fileReady)).Start();
						}
						else
						{
							Log.Error("JoinCompleteParts(" + tFile + ") filesize is not the same: " + size);

							FireNotificationAdded(Notification.Types.FileSizeMismatch, tFile);
						}
					}
					catch (Exception ex)
					{
						Log.Fatal("JoinCompleteParts(" + tFile + ") make", ex);

						FireNotificationAdded(Notification.Types.FileBuildFailed, tFile);
					}

					if (buildComplete)
					{
						RemoveFile(tFile);
					}
					else
					{
						// the creation was not successfull, so delete the files and parts
						FileSystem.DeleteFile(fileReady);
					}
				}
			}
		}

		/// <summary>
		/// 	Handles a downloaded file by calling the FileHandler string in the settings file
		/// </summary>
		/// <param name="aFile"> </param>
		static void HandleFile(string aFile)
		{
			if (!String.IsNullOrEmpty(aFile))
			{
				string folder = Path.GetDirectoryName(aFile);
				string file = Path.GetFileName(aFile);
				string fileName = Path.GetFileNameWithoutExtension(aFile);
				string fileExtension = Path.GetExtension(aFile);
				if (fileExtension != null && fileExtension.StartsWith("."))
				{
					fileExtension = fileExtension.Substring(1);
				}

				foreach (FileHandler handler in Settings.Default.GetFileHandlers())
				{
					try
					{
						Match tMatch = Regex.Match(aFile, handler.Regex, RegexOptions.IgnoreCase);
						if (tMatch.Success)
						{
							RunFileHandlerProcess(handler.Process, aFile, folder, file, fileName, fileExtension);
						}
					}
					catch (Exception ex)
					{
						Log.Fatal("RunFileHandler(" + aFile + ") Regex Error (" + handler.Regex + ")", ex);
					}
				}
			}
		}

		static void RunFileHandlerProcess(FileHandlerProcess aHandler, string aPath, string aFolder, string aFile, string aFileName, string aFileExtension)
		{
			if (aHandler == null || String.IsNullOrEmpty(aHandler.Command) || String.IsNullOrEmpty(aHandler.Arguments))
			{
				return;
			}

			string arguments = aHandler.Arguments;
			arguments = arguments.Replace("%PATH%", aPath);
			arguments = arguments.Replace("%FOLDER%", aFolder);
			arguments = arguments.Replace("%FILE%", aFile);
			arguments = arguments.Replace("%FILENAME%", aFileName);
			arguments = arguments.Replace("%EXTENSION%", aFileExtension);

			var p = new Process {Command = aHandler.Command, Arguments = arguments};

			if (p.Run())
			{
				RunFileHandlerProcess(aHandler.Next, aPath, aFolder, aFile, aFileName, aFileExtension);
			}
		}

		#endregion
	}
}
