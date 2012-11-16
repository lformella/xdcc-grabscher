// 
//  BrowserConnection.cs
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
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;

using log4net;

namespace XG.Server.Plugin.General.Webserver
{
	public class BrowserConnection : APlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public HttpListenerContext Context { get; set; }

		public FileLoader FileLoader;
		public string Salt;

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			string str = Context.Request.RawUrl;
			Log.Debug("StartRun() " + str);
#if !UNSAFE
			try
			{
#endif
				// serve the favicon
				if (str == "/favicon.ico")
				{
					WriteToStream(FileLoader.LoadImage(str));
				}
					// load a file
				else
				{
					if (str.Contains("?"))
					{
						str = str.Split('?')[0];
					}
					if (str == "/")
					{
						str = "/index.html";
					}

					if (str.EndsWith(".png"))
					{
						Context.Response.ContentType = "image/png";
						WriteToStream(FileLoader.LoadImage(str));
					}
					else
					{
						bool binary = false;

						if (str.EndsWith(".css"))
						{
							Context.Response.ContentType = "text/css";
						}
						else if (str.EndsWith(".js"))
						{
							Context.Response.ContentType = "application/x-javascript";
						}
						else if (str.EndsWith(".html"))
						{
							Context.Response.ContentType = "text/html;charset=UTF-8";
						}
						else if (str.EndsWith(".woff"))
						{
							Context.Response.ContentType = "application/x-font-woff";
							binary = true;
						}
						else if (str.EndsWith(".ttf"))
						{
							Context.Response.ContentType = "application/x-font-ttf";
							binary = true;
						}
						else if (str.EndsWith(".eog"))
						{
							binary = true;
						}
						else if (str.EndsWith(".svg"))
						{
							binary = true;
						}

						if (binary)
						{
							WriteToStream(FileLoader.LoadFile(str));
						}
						else
						{
							WriteToStream(FileLoader.LoadFile(str, Context.Request.UserLanguages));
						}
					}
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				try
				{
					Context.Response.Close();
				}
				catch (Exception)
				{
					// just ignore
				}
				Log.Fatal("StartRun(" + str + ")", ex);
			}
#endif
		}

		#endregion

		#region WRITE TO STREAM

		void WriteToStream(string aData)
		{
			WriteToStream(Encoding.UTF8.GetBytes(aData));
		}

		void WriteToStream(byte[] aData)
		{
			if (Context.Request.Headers["Accept-Encoding"] != null)
			{
				using (var ms = new MemoryStream())
				{
					Stream compress = null;
					if (Context.Request.Headers["Accept-Encoding"].Contains("gzip"))
					{
						Context.Response.AppendHeader("Content-Encoding", "gzip");
						compress = new GZipStream(ms, CompressionMode.Compress);
					}
					else if (Context.Request.Headers["Accept-Encoding"].Contains("deflate"))
					{
						Context.Response.AppendHeader("Content-Encoding", "deflate");
						compress = new DeflateStream(ms, CompressionMode.Compress);
					}

					if (compress != null)
					{
						compress.Write(aData, 0, aData.Length);
						compress.Dispose();
						aData = ms.ToArray();
					}
				}
			}

			Context.Response.AppendHeader("Server", "XG" + Settings.Instance.XgVersion);
			Context.Response.ContentLength64 = aData.Length;
			Context.Response.OutputStream.Write(aData, 0, aData.Length);
			Context.Response.OutputStream.Close();
		}

		#endregion
	}
}
