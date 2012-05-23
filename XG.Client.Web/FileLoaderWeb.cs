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
using System.Reflection;
using log4net;
using XG.Core;

namespace XG.Client.Web
{
	public class FileLoaderWeb
	{
		private static readonly ILog myLog = LogManager.GetLogger(typeof(FileLoaderWeb));

		private Dictionary<string, string> myDicStr;
		private Dictionary<string, byte[]> myDicByt;

		public static FileLoaderWeb Instance
		{
			get { return Nested.instance; }
		}
		class Nested
		{
			static Nested() { }
			internal static readonly FileLoaderWeb instance = new FileLoaderWeb();
		}

		private FileLoaderWeb()
		{
			this.myDicStr = new Dictionary<string, string>();
			this.myDicByt = new Dictionary<string, byte[]>();
		}

		public string LoadFile(string aFile, string[] aLanguages)
		{
			if (this.myDicStr.ContainsKey(aFile))
			{
				return this.myDicStr[aFile];
			}
			else
			{
#if !UNSAFE
				try
				{
#endif
#if DEBUG
					return this.PatchLanguage(File.OpenText("./Resources" +  aFile).ReadToEnd(), aLanguages);
#else
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoaderWeb));
					string name = assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
					this.myDicStr.Add(aFile, this.PatchLanguage(new StreamReader(assembly.GetManifestResourceStream(name)).ReadToEnd(), aLanguages));
					return this.myDicStr[aFile];
#endif
#if !UNSAFE
				}
				catch (Exception ex)
				{
					myLog.Fatal("LoadFile(" + aFile + ")", ex);
				}
#endif
			}
			return "";
		}

		private string PatchLanguage(string aContent, string[] aLanguages)
		{
			string lng = aLanguages[0].Substring(0, 2);
			return aContent.Replace("#LANGUAGE_SHORT#", lng);
		}

		public byte[] LoadImage(string aFile)
		{
			if (this.myDicByt.ContainsKey(aFile))
			{
				return this.myDicByt[aFile];
			}
			else
			{
#if !UNSAFE
				try
				{
#endif
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoaderWeb));
					string name = assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
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
					this.myDicByt.Add(aFile, data);
					return this.myDicByt[aFile];
#if !UNSAFE
				}
				catch (Exception ex)
				{
					myLog.Fatal("LoadImage(" + aFile + ")", ex);
				}
#endif
			}
			return null;
		}
	}
}
