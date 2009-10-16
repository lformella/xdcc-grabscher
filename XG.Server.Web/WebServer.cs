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
using System.Net;
using System.Text;
using System.Threading;
using XG.Client.Web;
using XG.Core;

namespace XG.Server.Web
{	
	public class WebServer : IServerPlugin
	{
		private const string VERSION = "0.1";
		
		private ServerRunner myRunner;

		private Thread myServerThread;
		private HttpListener myListener;

		#region RUN STOP RESTART

		/// <summary>
		/// Run method - should be called via thread
		/// </summary>
		public void Start(ServerRunner aParent)
		{
			this.myRunner = aParent;
			
			// start the server thread
			this.myServerThread = new Thread(new ThreadStart(OpenServer));
			this.myServerThread.Start();
		}

		/// <summary>
		/// called if the client signals to stop
		/// </summary>
		public void Stop()
		{
			this.CloseServer();
			this.myServerThread.Abort();
		}

		/// <summary>
		/// called if the client signals to do a restart
		/// </summary>
		public void Restart()
		{
			this.Stop();
			this.Start(this.myRunner);
		}

		#endregion

		#region SERVER

		/// <summary>
		/// Opens the server port, waiting for clients
		/// </summary>
		private void OpenServer()
		{
			this.myListener = new HttpListener();
#if !UNSAFE
			try
			{
#endif
				this.myListener.Prefixes.Add("http://*:" + (Settings.Instance.Port + 1) + "/");
				this.myListener.Start();

				while (true)
				{
#if !UNSAFE
					try
					{
#endif
						HttpListenerContext client = this.myListener.GetContext();
						Thread t = new Thread(new ParameterizedThreadStart(OpenClient));
						t.IsBackground = true;
						t.Start(client);
#if !UNSAFE
					}
					catch (Exception ex)
					{
						this.Log("OpenServer() client: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					}
#endif
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				this.Log("OpenServer() server: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
#endif
		}
		
		private void CloseServer()
		{
			this.myListener.Close();
		}

		#endregion

		#region CLIENT

		/// <summary>
		/// Called if a client connects
		/// </summary>
		/// <param name="aObject"></param>
		private void OpenClient(object aObject)
		{
			HttpListenerContext client = aObject as HttpListenerContext;
			
			Dictionary<string, string> tDic = new Dictionary<string, string>();

			string str = client.Request.RawUrl;
			this.Log("OpenClient() " + str, LogLevel.Traffic);

#if !UNSAFE
			try
			{
#endif	
				if(str.StartsWith("/?"))
				{	   
					string[] tCommand = str.Split('?')[1].Split('&');
					foreach(string tStr in tCommand)
					{
						string[] tArr = tStr.Split('=');
						tDic.Add(tArr[0], tArr[1]);
					}
					
					// no pass, no way
					try
					{
						// nice try
						if (!tDic.ContainsKey("password") || tDic["password"] != Settings.Instance.Password)
						{
							throw new Exception("Password wrong!");
						}
					}
					catch (Exception ex)
					{
						this.Log("OpenClient() password: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
						client.Response.Close();
						return;
					}
		
					TCPClientRequest tMessage = TCPClientRequest.None;
		
					// read the request id
					try { tMessage = (TCPClientRequest)int.Parse(tDic["request"]); }
					// this is ok
					catch (Exception ex)
					{
						this.Log("OpenClient() read client request: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
						return;
					}
					
					Comparison<XGObject> tComp = null;
					if(tDic.ContainsKey("sidx"))
					{
						switch(tDic["sidx"])
						{
							case "name":
								tComp = XGHelper.CompareObjectName;
								if(tDic["sord"] == "desc") tComp = XGHelper.CompareObjectNameReverse;
								break;
							case "connected":
								tComp = XGHelper.CompareObjectConnected;
								if(tDic["sord"] == "desc") tComp = XGHelper.CompareObjectConnectedReverse;
								break;
							case "enabled":
								tComp = XGHelper.CompareObjectEnabled;
								if(tDic["sord"] == "desc") tComp = XGHelper.CompareObjectEnabledReverse;
								break;
							case "id":
								tComp = XGHelper.ComparePacketId;
								if(tDic["sord"] == "desc") tComp = XGHelper.ComparePacketIdReverse;
								break;
							case "size":
								tComp = XGHelper.ComparePacketSize;
								if(tDic["sord"] == "desc") tComp = XGHelper.ComparePacketSizeReverse;
								break;
						}
					}
		
					#region DATA HANDLING

					switch (tMessage)
					{
						# region VERSION
	
						case TCPClientRequest.Version:
							this.WriteToStream(client.Response, VERSION);
							break;
						
						#endregion
						
						# region SERVER
	
						case TCPClientRequest.AddServer:
							this.myRunner.AddServer(tDic["name"]);
							this.WriteToStream(client.Response, "");
							break;
	
						case TCPClientRequest.RemoveServer:
							this.myRunner.RemoveServer(new Guid(tDic["guid"]));
							this.WriteToStream(client.Response, "");
							break;
	
						#endregion
	
						# region CHANNEL
	
						case TCPClientRequest.AddChannel:
							this.myRunner.AddChannel(new Guid(tDic["guid"]), tDic["name"]);
							this.WriteToStream(client.Response, "");
							break;
	
						case TCPClientRequest.RemoveChannel:
							this.myRunner.RemoveChannel(new Guid(tDic["guid"]));
							this.WriteToStream(client.Response, "");
							break;
	
						#endregion
	
						# region OBJECT
	
						case TCPClientRequest.ActivateObject:
							this.myRunner.ActivateObject(new Guid(tDic["guid"]));
							this.WriteToStream(client.Response, "");
							break;
	
						case TCPClientRequest.DeactivateObject:
							this.myRunner.DeactivateObject(new Guid(tDic["guid"]));
							this.WriteToStream(client.Response, "");
							break;
	
						#endregion
	
						# region SEARCH
	
						case TCPClientRequest.SearchPacket:
							this.WriteToStream(client.Response, this.myRunner.SearchPacket(tDic["name"], tComp), tDic["page"], tDic["rows"]);
							break;
	
						case TCPClientRequest.SearchPacketTime:
							this.WriteToStream(client.Response, this.myRunner.SearchPacketTime(tDic["name"], tComp), tDic["page"], tDic["rows"]);
							break;
	
						case TCPClientRequest.SearchBot:
							this.WriteToStream(client.Response, this.myRunner.SearchBot(tDic["name"], tComp), tDic["page"], tDic["rows"]);
							break;
	
						case TCPClientRequest.SearchBotTime:
							this.WriteToStream(client.Response, this.myRunner.SearchBotTime(tDic["name"], tComp), tDic["page"], tDic["rows"]);
							break;
	
						#endregion
	
						# region GET
	
						case TCPClientRequest.GetServersChannels:
							this.WriteToStream(client.Response, this.myRunner.GetServersChannels(), tDic["page"], tDic["rows"]);
							break;
	
						case TCPClientRequest.GetActivePackets:
							this.WriteToStream(client.Response, this.myRunner.GetActivePackets(), tDic["page"], tDic["rows"]);
							break;
	
						case TCPClientRequest.GetFiles:
							this.WriteToStream(client.Response, this.myRunner.GetFiles(), tDic["page"], tDic["rows"]);
							break;
	
						case TCPClientRequest.GetChildrenFromObject:
							this.WriteToStream(client.Response, this.myRunner.GetChildrenFromObject(new Guid(tDic["guid"]), tComp), tDic["page"], tDic["rows"]);
							break;
	
						#endregion
	
						# region COMMANDS
	
						case TCPClientRequest.RestartServer:
							this.Restart();
							this.WriteToStream(client.Response, "");
							break;
	
						case TCPClientRequest.CloseServer:
							this.Stop();
							this.WriteToStream(client.Response, "");
							break;
	
						#endregion
	
						default:
							this.WriteToStream(client.Response, "");
							break;
					}

					#endregion
				}
				else
				{
					if(str.StartsWith("/image&"))
					{
						this.WriteToStream(client.Response, ImageLoaderWeb.Instance.GetImage(str.Split('&')[1]));
					}
					else
					{
						if(str.Contains("?")) { str = str.Split('?')[0]; }	
						if(str == "/") { str = "/index.html"; }

						if(str.StartsWith("/css/style/"))
						{			
							str = str.Replace("/css/style/", "/css/" + Settings.Instance.StyleWebServer + "/");
						}
						
						if(str.EndsWith(".png")) { this.WriteToStream(client.Response, FileLoaderWeb.Instance.LoadImage(str)); }
						else { this.WriteToStream(client.Response, FileLoaderWeb.Instance.LoadFile(str)); }
					}
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				this.Log("OpenClient() read: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
#endif
	
			this.Log("OpenClient() disconnected", LogLevel.Info);
		}

		#endregion

		#region WRITE TO STREAM

		private void WriteToStream(HttpListenerResponse aResponse, XGObject[] aList, string aPage, string aRows)
		{
			int page = int.Parse(aPage);
			int rows = int.Parse(aRows);
			int count = 0;

			StringBuilder sb = new StringBuilder();
			bool first = true;
			sb.Append("{\"page\":\"" + page + "\",\"total\":\"" + Math.Ceiling((double)aList.Length / (double)rows).ToString() + "\",\"records\":\"" + aList.Length + "\",\"rows\":[\n");
			foreach (XGObject tObj in aList)
			{
				if(count >= (page - 1) * rows && count < page * rows)
				{
					if(first) { first = false; sb.Append("\t"); }
					else { sb.Append(", "); }
					sb.Append(this.Object2Json(tObj));
				}
				count++;
			}
			sb.Append("]\n");
			sb.Append("}\n");
			
			this.WriteToStream(aResponse, sb.ToString());
		}

		private void WriteToStream(HttpListenerResponse aResponse, string aData)
		{
			this.WriteToStream(aResponse, Encoding.UTF8.GetBytes(aData));
		}

		private void WriteToStream(HttpListenerResponse aResponse, byte[] aData)
		{
			aResponse.ContentLength64 = aData.Length;
			aResponse.OutputStream.Write(aData, 0, aData.Length);
			aResponse.OutputStream.Close();
		}

		#endregion
		
		#region JSON
		
		private string Object2Json(XGObject aObject)
		{
			StringBuilder sb = new StringBuilder();
			
			sb.Append("{\n");
			sb.Append("\t\t\"id\":\"" + aObject.Guid.ToString() + "\",\n");
			sb.Append("\t\t\"cell\":[\n");
			
			sb.Append("\t\t\t\"" + aObject.ParentGuid.ToString() + "\",\n");										//0
			sb.Append("\t\t\t" + aObject.Connected.ToString().ToLower() + ",\n");									//1
			sb.Append("\t\t\t" + aObject.Enabled.ToString().ToLower() + ",\n");										//2
			sb.Append("\t\t\t\"" + aObject.LastModified + "\",\n");													//3
			sb.Append("\t\t\t\"\",\n");																				//4

			if(aObject.GetType() == typeof(XGPacket))
			{
				XGPacket tPack = (XGPacket)aObject;
				XGFilePart tPart = this.myRunner.GetFilePart4Packet(tPack);

				sb.Append("\t\t\t\"" + tPack.Parent.Parent.Guid + "\",\n");											//5
				sb.Append("\t\t\t" + tPack.Id + ",\n");																//6
				sb.Append("\t\t\t\"" + (tPack.RealName != "" ? tPack.RealName : tPack.Name) + "\",\n");				//7
				sb.Append("\t\t\t" + (tPack.RealSize > 0 ? tPack.RealSize : tPack.Size) + ",\n");					//8
				sb.Append("\t\t\t" + (tPart == null ? "0" : tPart.Speed.ToString("0.00"))  + ",\n");				//9
				sb.Append("\t\t\t" + (tPart == null ? "0" : tPart.TimeMissing.ToString()) + ",\n");					//10
				sb.Append("\t\t\t" + (tPart == null ? "0" : tPart.StartSize.ToString()) + ",\n");					//11
				sb.Append("\t\t\t" + (tPart == null ? "0" : tPart.StopSize.ToString()) + ",\n");					//12
				sb.Append("\t\t\t" + (tPart == null ? "0" : tPart.CurrentSize.ToString()) + ",\n");					//13
				sb.Append("\t\t\t" + (tPack.Parent.getOldestActivePacket() != tPack ? "0" : "1") + ",\n");			//14
				sb.Append("\t\t\t\"" + tPack.LastUpdated + "\"\n");													//15
			}
			else
			{
				
				if(aObject.GetType() == typeof(XGChannel))
				{
					sb.Append("\t\t\t\"" + aObject.Name + "\",\n");													//5
					
					sb.Append("\t\t\t1,\n");																		//6
					sb.Append("\t\t\t\"" + aObject.ParentGuid + "\",\n");											//7
					sb.Append("\t\t\ttrue,\n");																		//8
					sb.Append("\t\t\tfalse\n");																		//9
				}
				
				if(aObject.GetType() == typeof(XGBot))
				{
					XGBot tBot = (XGBot)aObject;
					XGFilePart tPart = this.myRunner.GetFilePart4Bot(tBot);

					sb.Append("\t\t\t\"" + aObject.Name + "\",\n");													//5
					sb.Append("\t\t\t\"" + tBot.BotState + "\",\n");												//6
					sb.Append("\t\t\t\"" + (tPart == null ? "0" : tPart.Speed.ToString("0.00")) + "\",\n");			//7
					sb.Append("\t\t\t" + tBot.QueuePosition + ",\n");												//8
					sb.Append("\t\t\t\"" + tBot.QueueTime + "\",\n");												//9
					sb.Append("\t\t\t" + tBot.InfoSlotCurrent + ",\n");												//10
					sb.Append("\t\t\t" + tBot.InfoSlotTotal + ",\n");												//11
					sb.Append("\t\t\t" + tBot.InfoSpeedCurrent + ",\n");											//12
					sb.Append("\t\t\t" + tBot.InfoSpeedMax + ",\n");												//13
					sb.Append("\t\t\t" + tBot.InfoQueueCurrent + ",\n");											//14
					sb.Append("\t\t\t" + tBot.InfoQueueTotal + ",\n");												//15
					sb.Append("\t\t\t\"" + tBot.LastMessage.Replace('"', '\'') + "\",\n");							//16
					sb.Append("\t\t\t\"" + tBot.LastContact + "\"\n");												//17
				}
				
				if(aObject.GetType() == typeof(XGServer))
				{
					XGServer tServ = (XGServer)aObject;

					sb.Append("\t\t\t\"" + aObject.Name + ":" + tServ.Port + "\",\n");								//5
					
					sb.Append("\t\t\t0,\n");																		//6	
					sb.Append("\t\t\t0,\n");																		//7			
					sb.Append("\t\t\tfalse,\n");																	//8	
					sb.Append("\t\t\tfalse\n");																		//9	
				}
			}
			
			sb.Append("\t\t]\n\t}");

			return sb.ToString();
		}
		
		#endregion

		#region LOG

		/// <summary>
		/// Calls XGGelper.Log()
		/// </summary>
		/// <param name="aData"></param>
		/// <param name="aLevel"></param>
		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("WebServer." + aData, aLevel);
		}

		#endregion
	}
}
