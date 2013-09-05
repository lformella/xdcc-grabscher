// 
//  BackendPlugin.cs
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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

using XG.Core;
using XG.Server.Helper;

using log4net;

namespace XG.Server.Plugin.Backend.File
{
	public class BackendPlugin : ABackendPlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		BinaryFormatter _formatter;

		bool _isSaveFile;

		DateTime _lastObjectsSave = DateTime.Now;

		const string DataBinary = "xg.bin";
		const string FilesBinary = "xgfiles.bin";
		const string SearchesBinary = "xgsearches.bin";

		const int BackupDataTime = 900;

		#endregion

		public BackendPlugin()
		{
			_formatter = new BinaryFormatter();
			_formatter.Binder = new Version1ToVersion2DeserializationBinder();
		}

		#region ABackendPlugin

		public override Servers LoadServers()
		{
#if !UNSAFE
			try
			{
#endif
				var servers = (Servers) Load(Settings.Instance.AppDataPath + DataBinary);
				if (servers != null)
				{
					return Copy(servers);
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("LoadServers", ex);
			}
#endif
			return new Servers();
		}

		Servers Copy(Servers aServers, bool useHashset = true)
		{
			var servers = new Servers(useHashset);
			foreach (var aServer in aServers.All)
			{
				if (aServer == null)
				{
					continue;
				}
				var server = new Core.Server(aServer, useHashset);
				foreach (var aChannel in aServer.Channels)
				{
					if (aChannel == null)
					{
						continue;
					}
					var channel = new Channel(aChannel, useHashset);
					foreach (var aBot in aChannel.Bots)
					{
						if (aBot == null)
						{
							continue;
						}
						var bot = new Bot(aBot, useHashset);
						foreach (var aPacket in aBot.Packets)
						{
							if (aPacket == null)
							{
								continue;
							}
							var packet = new Packet(aPacket);
							bot.AddPacket(packet);
						}
						channel.AddBot(bot);
					}
					server.AddChannel(channel);
				}
				servers.Add(server);
			}
			return servers;
		}

		public override Files LoadFiles()
		{
#if !UNSAFE
			try
			{
#endif
				var files = (Files) Load(Settings.Instance.AppDataPath + FilesBinary);
				if (files != null)
				{
					return Copy(files);
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("LoadFiles", ex);
			}
#endif
			return new Files();
		}

		Files Copy(Files aFiles, bool useHashset = true)
		{
			var files = new Files(useHashset);
			foreach (var aFile in aFiles.All)
			{
				if (aFile == null)
				{
					continue;
				}
				var file = new Core.File(aFile, useHashset);
				foreach (var aFilePart in aFile.Parts)
				{
					if (aFilePart == null)
					{
						continue;
					}
					var filePart = new FilePart(aFilePart);
					file.Add(filePart);
				}
				files.Add(file);
			}
			return files;
		}

		public override Searches LoadSearches()
		{
#if !UNSAFE
			try
			{
#endif
				var searches = (Searches) Load(Settings.Instance.AppDataPath + SearchesBinary);
				if (searches != null)
				{
					return Copy(searches);
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("LoadSearches", ex);
			}
#endif
			return new Searches();
		}

		Searches Copy(Searches aSearches, bool useHashset = true)
		{
			var searches = new Searches(useHashset);
			foreach (var aSearch in aSearches.All)
			{
				if (aSearch == null)
				{
					continue;
				}
				var search = new Search(aSearch);
				searches.Add(search);
			}
			return searches;
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			DateTime _last = DateTime.Now;
			while (AllowRunning)
			{
				if (_last.AddSeconds(Settings.Instance.RunLoopTime) < DateTime.Now)
				{
					_last = DateTime.Now;

					// Objects
					if ((DateTime.Now - _lastObjectsSave).TotalSeconds > BackupDataTime)
					{
						SaveObjects();
					}

					// Files
					if (_isSaveFile)
					{
						SaveFiles();
					}
				}

				Thread.Sleep(500);
			}
		}

		protected override void StopRun()
		{
			// sync all to disk
			SaveFiles();
			SaveObjects();
			SaveSearches();
		}

		#endregion

		#region EVENTHANDLER

		protected override void FileAdded(AObject aParentObj, AObject aObj)
		{
			SaveFiles();
		}

		protected override void FileRemoved(AObject aParentObj, AObject aObj)
		{
			SaveFiles();
		}

		protected override void FileChanged(AObject aObj, string[] aFields)
		{
			if (aObj is Core.File)
			{
				SaveFiles();
			}
			else if (aObj is FilePart)
			{
				var part = aObj as FilePart;
				// if this change is lost, the data might be corrupt, so save it now
				if (part.State != FilePart.States.Open)
				{
					SaveFiles();
				}
				// the data saving can be scheduled
				else
				{
					_isSaveFile = true;
				}
			}
		}

		protected override void SearchAdded(AObject aParent, AObject aObj)
		{
			SaveSearches();
		}

		protected override void SearchRemoved(AObject aParent, AObject aObj)
		{
			SaveSearches();
		}

		protected override void ObjectAdded(AObject aParent, AObject aObj)
		{
			if (aObj is Core.Server || aObj is Channel)
			{
				SaveObjects();
			}
		}

		protected override void ObjectRemoved(AObject aParent, AObject aObj)
		{
			if (aObj is Core.Server || aObj is Channel)
			{
				SaveObjects();
			}
		}

		protected override void ObjectEnabledChanged(AObject aObj)
		{
			if (aObj is Core.Server || aObj is Channel || aObj is Packet)
			{
				SaveObjects();
			}
		}

		#endregion

		#region SAVE + LOAD

		/// <summary>
		/// 	Serializes an object into a file
		/// </summary>
		/// <param name="aObj"> </param>
		/// <param name="aFile"> </param>
		bool Save(object aObj, string aFile)
		{
			try
			{
				FileSystem.DeleteFile(aFile + ".new");
				using (Stream streamWrite = System.IO.File.Create(aFile + ".new"))
				{
					_formatter.Serialize(streamWrite, aObj);
					streamWrite.Close();
				}
				FileSystem.DeleteFile(aFile + ".bak");
				FileSystem.MoveFile(aFile, aFile + ".bak");
				FileSystem.MoveFile(aFile + ".new", aFile);
				Log.Debug("Save(" + aFile + ")");

				// run gc because serializing takes a lot of memory
				GC.Collect();
			}
			catch (Exception ex)
			{
				Log.Fatal("Save(" + aFile + ")", ex);
				return false;
			}
			return true;
		}

		/// <summary>
		/// 	Deserializes an object from a file
		/// </summary>
		/// <param name="aFile"> Name of the File </param>
		/// <returns> the object or null if the deserializing failed </returns>
		object Load(string aFile)
		{
			object obj = null;
			if (System.IO.File.Exists(aFile))
			{
				try
				{
					using (Stream streamRead = System.IO.File.OpenRead(aFile))
					{
						obj = _formatter.Deserialize(streamRead);
						streamRead.Close();
					}
					Log.Debug("Load(" + aFile + ")");

					// run gc because deserializing takes a lot of memory
					GC.Collect();
				}
				catch (Exception ex)
				{
					Log.Fatal("Load(" + aFile + ")", ex);
					// try to load the backup
					try
					{
						using (Stream streamRead = System.IO.File.OpenRead(aFile + ".bak"))
						{
							obj = _formatter.Deserialize(streamRead);
							streamRead.Close();
						}
						Log.Debug("Load(" + aFile + ".bak)");
					}
					catch (Exception)
					{
						Log.Fatal("Load(" + aFile + ".bak)", ex);
					}
				}
			}
			return obj;
		}

		bool SaveFiles()
		{
			lock (FilesBinary)
			{
				_isSaveFile = false;
				return Save(Copy(Files, false), Settings.Instance.AppDataPath + FilesBinary);
			}
		}

		bool SaveObjects()
		{
			lock (DataBinary)
			{
				_lastObjectsSave = DateTime.Now;
				bool ret = true;

				//var servers = (from server in Servers select server)
				ret = ret && Save(Copy(Servers, false), Settings.Instance.AppDataPath + DataBinary);

				return ret;
			}
		}

		bool SaveSearches()
		{
			lock (SearchesBinary)
			{
				return Save(Copy(Searches, false), Settings.Instance.AppDataPath + SearchesBinary);
			}
		}

		#endregion
	}

	public sealed class Version1ToVersion2DeserializationBinder : SerializationBinder
	{
		public override Type BindToType(string assemblyName, string typeName)
		{
			Type typeToDeserialize = null;

			if (typeName == "XG.Core.Object")
			{
				typeName = "XG.Core.Search";
			}
			if (typeName == "XG.Core.Objects")
			{
				typeName = "XG.Core.Searches";
			}

			typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));

			return typeToDeserialize;
		}
	}
}
