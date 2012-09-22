// 
//  FileLoaderWeb.cs
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
using System.Reflection;
using log4net;

namespace XG.Client.Web
{
	public class FileLoaderWeb
	{
		static readonly ILog _log = LogManager.GetLogger(typeof(FileLoaderWeb));

		Dictionary<string, string> _dicStr;
		Dictionary<string, byte[]> _dicByt;

		public static FileLoaderWeb Instance
		{
			get { return Nested.Instance; }
		}
		class Nested
		{
			static Nested() { }
			internal static readonly FileLoaderWeb Instance = new FileLoaderWeb();
		}

		FileLoaderWeb()
		{
			_dicStr = new Dictionary<string, string>();
			_dicByt = new Dictionary<string, byte[]>();
		}

		public string LoadFile(string aFile, string[] aLanguages)
		{
			if (_dicStr.ContainsKey(aFile))
			{
				return _dicStr[aFile];
			}
			else
			{
#if !UNSAFE
				try
				{
#endif
#if DEBUG
					return PatchLanguage(File.OpenText("./Resources" + aFile).ReadToEnd(), aLanguages);
#else
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoaderWeb));
                    string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
					_dicStr.Add(aFile, PatchLanguage(new StreamReader(assembly.GetManifestResourceStream(name)).ReadToEnd(), aLanguages));
					return _dicStr[aFile];
#endif
#if !UNSAFE
				}
				catch (Exception ex)
				{
					_log.Fatal("LoadFile(" + aFile + ")", ex);
				}
#endif
			}
#if !UNSAFE
			return "";
#endif
		}

		string PatchLanguage(string aContent, string[] aLanguages)
		{
			string lng = aLanguages[0].Substring(0, 2);
			return aContent.Replace("#LANGUAGE_SHORT#", lng);
		}

		public byte[] LoadImage(string aFile)
		{
			if (_dicByt.ContainsKey(aFile))
			{
				return _dicByt[aFile];
			}
			else
			{
#if !UNSAFE
				try
				{
#endif
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoaderWeb));
                    string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
					Stream stream = assembly.GetManifestResourceStream(name);
					byte[] data = new byte[stream.Length];
					int offset = 0;
					int remaining = data.Length;
					while (remaining > 0)
					{
						int read = stream.Read(data, offset, remaining);
						if (read <= 0) { throw new EndOfStreamException(String.Format("End of stream reached with {0} bytes left to read", remaining)); }
						remaining -= read;
						offset += read;
					}
					_dicByt.Add(aFile, data);
					return _dicByt[aFile];
#if !UNSAFE
				}
				catch (Exception ex)
				{
					_log.Fatal("LoadImage(" + aFile + ")", ex);
				}
#endif
			}
#if !UNSAFE
			return null;
#endif
		}
	}
}
