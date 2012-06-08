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
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using log4net;
using XG.Client.Web;
using XG.Core;

namespace XG.Server.Web
{
	public class WebServer : IServerPlugin
	{
		#region VARIABLES

		private static readonly ILog myLog = LogManager.GetLogger(typeof(WebServer));

		private ServerRunner myRunner;

		private Thread myServerThread;
		private HttpListener myListener;

		#endregion

		#region RUN STOP

		/// <summary>
		/// Run method - opens itself in a new thread
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
				this.myListener.Prefixes.Add("http://*:" + (Settings.Instance.WebServerPort) + "/");
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
						myLog.Fatal("OpenServer() client", ex);
					}
#endif
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				myLog.Fatal("OpenServer() server", ex);
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
			myLog.Debug("OpenClient() " + str);

#if !UNSAFE
			try
			{
#endif
				if (str.StartsWith("/?"))
				{
					string[] tCommand = str.Split('?')[1].Split('&');
					foreach (string tStr in tCommand)
					{
						if (tStr.Contains("="))
						{
							string[] tArr = tStr.Split('=');
							tDic.Add(tArr[0], HttpUtility.UrlDecode(tArr[1]));
						}
					}

					// no pass, no way
					try
					{
						// nice try
						if (!tDic.ContainsKey("password") || HttpUtility.UrlDecode(tDic["password"]) != Settings.Instance.Password)
						{
							throw new Exception("Password wrong!");
						}
					}
					catch (Exception ex)
					{
						myLog.Fatal("OpenClient() password", ex);
						client.Response.Close();
						return;
					}

					TCPClientRequest tMessage = TCPClientRequest.None;

					// read the request id
					try { tMessage = (TCPClientRequest)int.Parse(tDic["request"]); }
					catch (Exception ex)
					{
						myLog.Fatal("OpenClient() read client request", ex);
						return;
					}

					Comparison<XGObject> tComp = null;
					if (tDic.ContainsKey("sidx"))
					{
						switch (tDic["sidx"])
						{
							case "name":
								tComp = XGHelper.CompareObjectName;
								if (tDic["sord"] == "desc") tComp = XGHelper.CompareObjectNameReverse;
								break;
							case "connected":
								tComp = XGHelper.CompareObjectConnected;
								if (tDic["sord"] == "desc") tComp = XGHelper.CompareObjectConnectedReverse;
								break;
							case "enabled":
								tComp = XGHelper.CompareObjectEnabled;
								if (tDic["sord"] == "desc") tComp = XGHelper.CompareObjectEnabledReverse;
								break;
							case "id":
								tComp = XGHelper.ComparePacketId;
								if (tDic["sord"] == "desc") tComp = XGHelper.ComparePacketIdReverse;
								break;
							case "size":
								tComp = XGHelper.ComparePacketSize;
								if (tDic["sord"] == "desc") tComp = XGHelper.ComparePacketSizeReverse;
								break;
							case "lastupdated":
								tComp = XGHelper.ComparePacketLastUpdated;
								if (tDic["sord"] == "desc") tComp = XGHelper.ComparePacketLastUpdatedReverse;
								break;
						}
					}

					#region DATA HANDLING

					List<XGObject> list = null;

					switch (tMessage)
					{
						# region VERSION

						case TCPClientRequest.Version:
							this.WriteToStream(client.Response, Settings.Instance.XgVersion);
							break;

						#endregion

						# region SERVER

						case TCPClientRequest.AddServer:
							this.myRunner.AddServer(HttpUtility.UrlDecode(tDic["name"]));
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
							list = this.myRunner.SearchPacket(HttpUtility.UrlDecode(tDic["name"]), tComp);
							break;

						case TCPClientRequest.SearchPacketTime:
							list = this.myRunner.SearchPacketTime(HttpUtility.UrlDecode(tDic["name"]), tComp);
							break;

						case TCPClientRequest.SearchPacketActiveDownloads:
							list = this.myRunner.SearchPacketActiveDownloads(tComp);
							break;

						case TCPClientRequest.SearchPacketsEnabled:
							list = this.myRunner.SearchPacketsEnabled(tComp);
							break;

						case TCPClientRequest.SearchBot:
							list = this.myRunner.SearchBot(HttpUtility.UrlDecode(tDic["name"]), tComp);
							break;

						case TCPClientRequest.SearchBotTime:
							list = this.myRunner.SearchBotTime(HttpUtility.UrlDecode(tDic["name"]), tComp);
							break;

						case TCPClientRequest.SearchBotActiveDownloads:
							list = this.myRunner.SearchBotActiveDownloads(tComp);
							break;

						case TCPClientRequest.SearchBotsEnabled:
							list = this.myRunner.SearchBotsEnabled(tComp);
							break;

						#endregion

						# region SEARCH SPECIAL

						case TCPClientRequest.AddSearch:
							this.myRunner.AddSearch(HttpUtility.UrlDecode(tDic["name"]));
							this.WriteToStream(client.Response, "");
							break;

						case TCPClientRequest.RemoveSearch:
							this.myRunner.RemoveSearch(HttpUtility.UrlDecode(tDic["name"]));
							this.WriteToStream(client.Response, "");
							break;

						case TCPClientRequest.GetSearches:
							this.WriteToStream(client.Response, this.Searches2Json(this.myRunner.GetSearches()));
							break;

						#endregion

						# region GET

						case TCPClientRequest.GetObject:
							this.WriteToStream(client.Response, this.myRunner.GetObject(new Guid(tDic["guid"])));
							break;

						case TCPClientRequest.GetServers:
							list = this.myRunner.GetServers();
							break;

						case TCPClientRequest.GetActivePackets:
							list = this.myRunner.GetActivePackets();
							break;

						case TCPClientRequest.GetFiles:
							list = this.myRunner.GetFiles();
							break;

						case TCPClientRequest.GetChildrenFromObject:
							list = this.myRunner.GetChildrenFromObject(new Guid(tDic["guid"]), tComp);
							break;

						case TCPClientRequest.GetStatistics:
							this.WriteToStream(client.Response, this.Statistic2Json());
							break;

						#endregion

						# region COMMANDS

						case TCPClientRequest.CloseServer:
							this.Stop();
							this.WriteToStream(client.Response, "");
							break;

						#endregion

						# region XDCC Link

						case TCPClientRequest.ParseXdccLink:
							//xdcc://irc.1andallirc.net/irc.1andallirc.net/#cosmic/{CoSmIc-Mp3Z}-{21667}/#6/Asking_Alexandria-Stepped_Up_And_Scratched-2011-MTD.rar/
							string[] link = HttpUtility.UrlDecode(tDic["name"]).Substring(7).Split('/');
							string serverName = link[0];
							string channelName = link[2];
							string botName = link[3];
							int packetId = int.Parse(link[4].Substring(1));

							// checking server
							XGServer serv = this.myRunner.RootObject[serverName];
							if(serv == null)
							{
								this.myRunner.RootObject.AddServer(serverName);
								serv = this.myRunner.RootObject[serverName];
							}
							serv.Enabled = true;

							// checking channel
							XGChannel chan = serv[channelName];
							if(chan == null)
							{
								serv.AddChannel(channelName);
								chan = serv[channelName];
							}
							chan.Enabled = true;

							// checking bot
							XGBot bot = chan[botName];
							if(bot == null)
							{
								bot = new XGBot();
								bot.Name = botName;
								chan.AddBot(bot);
							}

							// checking packet
							XGPacket pack = bot[packetId];
							if(pack == null)
							{
								pack = new XGPacket();
								pack.Id = packetId;
								pack.Name = link[5];
								bot.addPacket(pack);
							}
							pack.Enabled = true;

							this.WriteToStream(client.Response, "");
							break;

						#endregion

						default:
							this.WriteToStream(client.Response, "");
							break;
					}
					
					if (list != null)
					{
						if(tDic["offbots"] == "1")
						{
							foreach (XGObject tObj in list.ToArray())
							{
								if (tObj.GetType() == typeof(XGPacket) && !((XGPacket)tObj).Parent.Connected)
								{
									list.Remove(tObj);
								}
								else if (tObj.GetType() == typeof(XGBot) && !((XGBot)tObj).Connected)
								{
									list.Remove(tObj);
								}
							}
						}
						this.WriteToStream(client.Response, list, tDic["page"], tDic["rows"]);
					}

					#endregion
				}
				else
				{
					// load an image
					if (str.StartsWith("/image&"))
					{
						this.WriteToStream(client.Response, ImageLoaderWeb.Instance.GetImage(str.Split('&')[1]));
					}
					// serve the favicon
					else if (str == "/favicon.ico")
					{
						this.WriteToStream(client.Response, ImageLoaderWeb.Instance.GetImage("Client"));
					}
					// load a file
					else
					{
						if (str.Contains("?")) { str = str.Split('?')[0]; }
						if (str == "/") { str = "/index.html"; }

						if (str.StartsWith("/css/style/"))
						{
							str = str.Replace("/css/style/", "/css/" + Settings.InstanceReload.StyleWebServer + "/");
						}

						if (str.EndsWith(".png"))
						{
							this.WriteToStream(client.Response, FileLoaderWeb.Instance.LoadImage(str));
						}
						else
						{
							this.WriteToStream(client.Response, FileLoaderWeb.Instance.LoadFile(str, client.Request.UserLanguages));
						}
					}
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				myLog.Fatal("OpenClient() read", ex);
			}
#endif
			myLog.Info("OpenClient() disconnected");
		}

		#endregion

		#region WRITE TO STREAM

		private void WriteToStream (HttpListenerResponse aResponse, XGObject aObj)
		{
			List<XGObject> list = new List<XGObject> ();
			list.Add (aObj);
			this.WriteToStream(aResponse, list, "", "");
		}

		private void WriteToStream(HttpListenerResponse aResponse, List<XGObject> aList, string aPage, string aRows)
		{
			int page = aPage != "" ? int.Parse(aPage) : 1;
			int rows = aRows != "" ? int.Parse(aRows) : 1;
			int count = 0;

			StringBuilder sb = new StringBuilder();
			bool first = true;
			if (aPage != "" && aRows != "")
			{
				sb.Append("{");
				sb.Append("\"page\":\"" + page + "\",");
				sb.Append("\"total\":\"" + Math.Ceiling((double)aList.Count / (double)rows).ToString() + "\",");
				sb.Append("\"records\":\"" + aList.Count + "\",");
				sb.Append("\"rows\":[");
			}
			foreach (XGObject tObj in aList)
			{
				if (count >= (page - 1) * rows && count < page * rows)
				{
					if (first) { first = false; sb.Append(""); }
					else { sb.Append(", "); }
					sb.Append(this.Object2Json(tObj));
				}
				count++;
			}
			if (aPage != "" && aRows != "")
			{
				sb.Append("]}");
			}

			aResponse.ContentType = "text/json";
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

		private string Searches2Json(List<string> aData)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append("\"Searches\":[");

			int count = 0;
			foreach (string str in aData)
			{
				count++;
				sb.Append("{\"Search\": \"" + this.ClearString(str) + "\"}");
				if(count < aData.Count) { sb.Append(","); }
				sb.Append("");
			}

			sb.Append("]}");

			return sb.ToString();
		}

		private string Object2Json(XGObject aObject)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append("\"id\":\"" + aObject.Guid.ToString() + "\",");
			sb.Append("\"cell\":{");

			sb.Append("\"ParentGuid\":\"" + aObject.ParentGuid.ToString() + "\",");
			sb.Append("\"Connected\":\"" + aObject.Connected.ToString().ToLower() + "\",");
			sb.Append("\"Enabled\":\"" + aObject.Enabled.ToString().ToLower() + "\",");
			sb.Append("\"LastModified\":\"" + aObject.LastModified + "\",");

			if (aObject.GetType() == typeof(XGServer))
			{
				XGServer tServ = (XGServer)aObject;

				sb.Append("\"Name\":\"" + aObject.Name + ":" + tServ.Port + "\",");
				sb.Append("\"level\":0,");
				sb.Append("\"parent\":0,");
				sb.Append("\"isLeaf\":false,");
				sb.Append("\"loaded\":true");
			}
			else if (aObject.GetType() == typeof(XGChannel))
			{
				sb.Append("\"Name\":\"" + aObject.Name + "\",");
				sb.Append("\"level\":1,");
				sb.Append("\"parent\":\"" + aObject.ParentGuid + "\",");
				sb.Append("\"isLeaf\":true,");
				sb.Append("\"loaded\":true");
			}
			else if (aObject.GetType() == typeof(XGBot))
			{
				XGBot tBot = (XGBot)aObject;
				XGFilePart tPart = this.myRunner.GetFilePart4Bot(tBot);

				sb.Append("\"Name\":\"" + this.ClearString(aObject.Name) + "\",");
				sb.Append("\"BotState\":\"" + tBot.BotState + "\",");
				sb.Append("\"Speed\":" + (tPart == null ? "0" : tPart.Speed.ToString("0.00").Replace(",", ".")) + ",");
				sb.Append("\"QueQueuePosition\":" + tBot.QueuePosition + ",");
				sb.Append("\"QueueTime\":" + tBot.QueueTime + ",");
				sb.Append("\"InfoSpeedMax\":" + tBot.InfoSpeedMax.ToString().Replace(',', '.') + ",");
				sb.Append("\"InfoSpeedCurrent\":" + tBot.InfoSpeedCurrent.ToString().Replace(',', '.') + ",");
				sb.Append("\"InfoSlotTotal\":" + tBot.InfoSlotTotal + ",");
				sb.Append("\"InfoSlotCurrent\":" + tBot.InfoSlotCurrent + ",");
				sb.Append("\"InfoQueueTotal\":" + tBot.InfoQueueTotal + ",");
				sb.Append("\"InfoQueueCurrent\":" + tBot.InfoQueueCurrent + ",");
				sb.Append("\"LastMessage\":\"" + this.ClearString(tBot.LastMessage) + "\",");
				sb.Append("\"LastContact\":\"" + tBot.LastContact + "\"");
			}
			else if (aObject.GetType() == typeof(XGPacket))
			{
				XGPacket tPack = (XGPacket)aObject;
				XGFilePart tPart = this.myRunner.GetFilePart4Packet(tPack);

				sb.Append("\"Id\":" + tPack.Id + ",");
				sb.Append("\"Name\":\"" + this.ClearString(tPack.RealName != "" ? tPack.RealName : tPack.Name) + "\",");
				sb.Append("\"Size\":" + (tPack.RealSize > 0 ? tPack.RealSize : tPack.Size) + ",");
				sb.Append("\"Speed\":" + (tPart == null ? "0" : tPart.Speed.ToString("0.00").Replace(",", ".")) + ",");
				sb.Append("\"TimeMissing\":" + (tPart == null ? "0" : tPart.TimeMissing.ToString()) + ",");
				sb.Append("\"StartSize\":" + (tPart == null ? "0" : tPart.StartSize.ToString()) + ",");
				sb.Append("\"StopSize\":" + (tPart == null ? "0" : tPart.StopSize.ToString()) + ",");
				sb.Append("\"CurrentSize\":" + (tPart == null ? "0" : tPart.CurrentSize.ToString()) + ",");
				sb.Append("\"IsChecked\":\"" + (tPart == null ? "false" : tPart.IsChecked ? "true" : "false") + "\",");
				sb.Append("\"Order\":\"" + (tPack.Parent.getOldestActivePacket() != tPack ? "false" : "true") + "\",");
				sb.Append("\"LastUpdated\":\"" + tPack.LastUpdated + "\"");
			}

			sb.Append("}}");

			return sb.ToString();
		}

		private string Statistic2Json()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{");

			List<StatisticType> types = new List<StatisticType>();
			// uncool, but works
			for (int a = (int)StatisticType.BytesLoaded; a <= (int)StatisticType.SpeedAvg; a++)
			{
				types.Add((StatisticType)a);
			}

			int count = 0;
			foreach (StatisticType type in types)
			{
				count++;
				double val = Statistic.Instance.Get(type);
				sb.Append ("\"" + type + "\":" + val.ToString().Replace(",", "."));
				if (count < types.Count) { sb.Append(","); }
				sb.Append("");
			}

			sb.Append("}");
			return sb.ToString();
		}

		private Regex myClearRegex = new Regex(@"[^A-Za-z0-9äÄöÖüÜß _.\[\]\{\}\(\)-]");
		private string ClearString(string aString)
		{
			string str = this.myClearRegex.Replace(aString, "");
			str = str.Replace("Ä", "&Auml;");
			str = str.Replace("ä", "&auml;");
			str = str.Replace("Ö", "&Ouml;");
			str = str.Replace("ö", "&ouml;");
			str = str.Replace("Ü", "&Uuml;");
			str = str.Replace("ü", "&uuml;");
			str = str.Replace("ß", "&szlig;");
			return str;
		}

		#endregion
	}
}
