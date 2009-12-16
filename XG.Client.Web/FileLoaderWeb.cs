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
using XG.Core;
using System.Reflection;
using System.IO;

namespace XG.Client.Web
{
	public class FileLoaderWeb
	{
		private Dictionary<string, string> myDicStr;
		private Dictionary<string, byte[]> myDicByt;
		
		public static FileLoaderWeb Instance
		{
			get { return Nested.instance; }
		}
		class Nested
		{
			static Nested() {}
			internal static readonly FileLoaderWeb instance = new FileLoaderWeb();
		}

		private FileLoaderWeb()
		{
			this.myDicStr = new Dictionary<string, string>();
			this.myDicByt = new Dictionary<string, byte[]>();
		}
		
		public string LoadFile(string aFile)
		{			
			if(this.myDicStr.ContainsKey(aFile))
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
					return File.OpenText("./Resources" +  aFile).ReadToEnd();
#else
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoaderWeb));
					string name = assembly.GetName().Name + ".Resources" +  aFile.Replace('/', '.');
					this.myDicStr.Add(aFile, new StreamReader(assembly.GetManifestResourceStream(name)).ReadToEnd());
					return this.myDicStr[aFile];
#endif
#if !UNSAFE
				}
				catch(Exception ex)
				{
					this.Log("LoadFile(" + aFile + ") " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				}
#endif
			}
			return "";
		}

		public byte[] LoadImage(string aFile)
		{
			if(this.myDicByt.ContainsKey(aFile))
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
					string name = assembly.GetName().Name + ".Resources" +  aFile.Replace('/', '.');
					Stream stream = assembly.GetManifestResourceStream(name);
					byte[] data = new byte[stream.Length];
					int offset = 0;
					int remaining = data.Length;
					while (remaining > 0)
					{
						int read = stream.Read(data, offset, remaining);
						if (read <= 0) { throw new EndOfStreamException (String.Format("End of stream reached with {0} bytes left to read", remaining)); }
						remaining -= read;
						offset += read;
					}
					this.myDicByt.Add(aFile, data);
					return this.myDicByt[aFile];
#if !UNSAFE
				}
				catch(Exception ex)
				{
					this.Log("LoadImage(" + aFile + ") " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				}
#endif
			}
			return null;
		}

		#region LOG

		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("FileLoaderWeb." + aData, aLevel);
		}

		#endregion
	}
}
