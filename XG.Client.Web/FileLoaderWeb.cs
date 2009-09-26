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
				try
				{
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoaderWeb));
					string name = assembly.GetName().Name + ".Resources" +  aFile.Replace('/', '.');
					//XGHelper.Log("FileLoaderWeb.LoadFile(" + aFile + ") resource: " + name, LogLevel.Notice);

					this.myDicStr.Add(aFile, new StreamReader(assembly.GetManifestResourceStream(name)).ReadToEnd());
					return this.myDicStr[aFile];
				}
				catch(Exception ex)
				{
					XGHelper.Log("FileLoaderWeb.LoadFile(" + aFile + ") " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				}
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
				try
				{
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoaderWeb));
					string name = assembly.GetName().Name + ".Resources" +  aFile.Replace('/', '.');
					//XGHelper.Log("FileLoaderWeb.LoadFile(" + aFile + ") resource: " + name, LogLevel.Notice);			byte[] data = new byte[aStream.Length];
					Stream stream = assembly.GetManifestResourceStream(name);
					byte[] data = new byte[stream.Length];
					int offset = 0;
					int remaining = data.Length;
					while (remaining > 0)
					{
						int read = stream.Read(data, offset, remaining);
						if (read <= 0)
							throw new EndOfStreamException (String.Format("End of stream reached with {0} bytes left to read", remaining));
						remaining -= read;
						offset += read;
					}
					this.myDicByt.Add(aFile, data);
					return this.myDicByt[aFile];
				}
				catch(Exception ex)
				{
					XGHelper.Log("FileLoaderWeb.LoadImage(" + aFile + ") " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				}
			}
			return null;
		}
	}
}
