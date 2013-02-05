// 
//  FileLoader.cs
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

namespace XG.Server.Plugin.General.Webserver.Webserver
{
	public class FileLoader
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly Dictionary<string, string> _dicString;
		readonly Dictionary<string, byte[]> _dicByte;

		readonly string[] _cssFiles =
		{
			"reset",
			"fontello",
			"jquery-ui",
			"slick.grid",
			"slick.pager",
			"layout-default",
			"xg"};

		readonly string[] _jsFiles =
		{
			"external/moment.min",
			"external/jquery.min",
			"external/jquery.event.drag-2.0.min",
			"external/jquery.cookie.min",
			"external/jquery.flot",
			"external/jquery.flot.axislabels",
			"external/jquery-timing.min",
			"external/jquery-ui.min",
			"external/jquery.layout.min",
			"external/json2.min",
			"external/sha256",
			"external/slick.core",
			"external/slick.formatters",
			"external/slick.grid",
			"external/slick.dataview",
			"external/slick.pager",
			"i18n/moment/#LANGUAGE_SHORT#",
			"i18n/xg/#LANGUAGE_SHORT#",
			"xg/enum",
			"xg/cookie",
			"xg/formatter",
			"xg/helper",
			"xg/grid",
			"xg/main",
			"xg/password",
			"xg/dataview",
			"xg/resize",
			"xg/statistics",
			"xg/translate",
			"xg/websocket"
		};

		public string Salt;

		#endregion

		public FileLoader()
		{
			_dicString = new Dictionary<string, string>();
			_dicByte = new Dictionary<string, byte[]>();
		}

		public string LoadFile(string aFile, string aHost, string[] aLanguages)
		{
			if (_dicString.ContainsKey(aFile))
			{
				return _dicString[aFile];
			}
#if !UNSAFE
			try
			{
#endif
				string content = "";
				if (aFile == "/js/all.js")
				{
					foreach (string file in _jsFiles)
					{
						content += LoadFile("/js/" + PatchLanguage(file, aLanguages) + ".js", aHost, aLanguages) + "\n";
					}
				}
				else if (aFile == "/css/all.css")
				{
					foreach (string file in _cssFiles)
					{
						content += LoadFile("/css/" + file + ".css", aHost, aLanguages) + "\n";
					}
				}
				else
				{
#if DEBUG && !WINDOWS
					content = File.OpenText("./Resources" + aFile).ReadToEnd();
#else
					Assembly assembly = Assembly.GetAssembly(typeof (FileLoader));
					string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
					Stream stream = assembly.GetManifestResourceStream(name);
					if (stream != null)
					{
						content = new StreamReader(stream).ReadToEnd();
					}
#endif
					if (aFile == "/index.html" || aFile == "/index2.html")
					{
						content = content.Replace("#HOST#", aHost);
						content = content.Replace("#PORT#", "" + (Settings.Instance.WebServerPort + 1));
#if DEBUG
						string css = "";
						foreach (string cssFile in _cssFiles)
						{
							css += "\t\t<link rel=\"stylesheet\" type=\"text/css\" media=\"screen\" href=\"css/" + cssFile + ".css\" />\n";
						}
#else
						string css = "\t\t<link rel=\"stylesheet\" type=\"text/css\" media=\"screen\" href=\"css/all.css\" />\n";
#endif
						content = content.Replace("#CSS_FILES#", css);

						//#if DEBUG
						string js = "";
						foreach (string jsFile in _jsFiles)
						{
							js += "\t\t<script type=\"text/javascript\" src=\"js/" + jsFile + ".js\"></script>\n";
						}
						//#else
						//	string js = "\t\t<script type=\"text/javascript\" src=\"js/all.js\"></script>\n";
						//#endif 
						content = content.Replace("#JS_FILES#", js);

						content = content.Replace("#SALT#", Salt);
						content = PatchLanguage(content, aLanguages);
					}
				}
#if !DEBUG
				_dicString.Add(aFile, content);
#endif
				return content;
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("LoadFile(" + aFile + ")", ex);
			}
			return "";
#endif
		}

		public byte[] LoadFile(string aFile)
		{
			if (_dicByte.ContainsKey(aFile))
			{
				return _dicByte[aFile];
			}
#if !UNSAFE
			try
			{
#endif
#if DEBUG && !WINDOWS
				return File.ReadAllBytes("./Resources" + aFile);
#else
				Assembly assembly = Assembly.GetAssembly(typeof (FileLoader));
				string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
				Stream stream = assembly.GetManifestResourceStream(name);
				if (stream != null)
				{
					_dicByte.Add(aFile, new BinaryReader(stream).ReadAllBytes());
					return _dicByte[aFile];
				}
#endif
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("LoadFile(" + aFile + ")", ex);
			}
			return new byte[0];
#endif
		}

		string PatchLanguage(string aContent, string[] aLanguages)
		{
			string lng = aLanguages[0].Substring(0, 2);
			return aContent.Replace("#LANGUAGE_SHORT#", lng);
		}

		public byte[] LoadImage(string aFile)
		{
			if (_dicByte.ContainsKey(aFile))
			{
				return _dicByte[aFile];
			}
#if !UNSAFE
			try
			{
#endif
				Assembly assembly = Assembly.GetAssembly(typeof (FileLoader));
				string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
				Stream stream = assembly.GetManifestResourceStream(name);
				if (stream != null)
				{
					byte[] data = new byte[stream.Length];
					int offset = 0;
					int remaining = data.Length;
					while (remaining > 0)
					{
						int read = stream.Read(data, offset, remaining);
						if (read <= 0)
						{
							throw new EndOfStreamException(String.Format("End of stream reached with {0} bytes left to read", remaining));
						}
						remaining -= read;
						offset += read;
					}
					_dicByte.Add(aFile, data);
					return _dicByte[aFile];
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("LoadImage(" + aFile + ")", ex);
			}
#endif
			return null;
		}
	}

	public static class BinaryReaderExtension
	{
		public static byte[] ReadAllBytes(this BinaryReader reader)
		{
			const int bufferSize = 4096;
			using (var ms = new MemoryStream())
			{
				byte[] buffer = new byte[bufferSize];
				int count;
				while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
				{
					ms.Write(buffer, 0, count);
				}
				return ms.ToArray();
			}
		}
	}
}
