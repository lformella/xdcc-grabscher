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
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

using log4net;

using XG.Client.Web;
using XG.Core;
using XG.Server.Plugin;

namespace XG.Server.Plugin.General.Webserver
{
	public class Plugin : AServerGeneralPlugin
	{
		#region VARIABLES

		private static readonly ILog log = LogManager.GetLogger(typeof(Plugin));

		private Thread serverThread;
		private HttpListener listener;

		#endregion

		#region RUN STOP

		/// <summary>
		/// Run method - opens itself in a new thread
		/// </summary>
		public override void Start()
		{
			// start the server thread
			this.serverThread = new Thread(new ThreadStart(OpenServer));
			this.serverThread.Start();
		}

		/// <summary>
		/// called if the client signals to stop
		/// </summary>
		public override void Stop()
		{
			this.CloseServer();
			this.serverThread.Abort();
		}

		#endregion

		#region SERVER

		/// <summary>
		/// Opens the server port, waiting for clients
		/// </summary>
		private void OpenServer()
		{
			this.listener = new HttpListener();
#if !UNSAFE
			try
			{
#endif
				this.listener.Prefixes.Add("http://*:" + (Settings.Instance.WebServerPort) + "/");
				this.listener.Start();

				while (true)
				{
#if !UNSAFE
					try
					{
#endif
						HttpListenerContext client = this.listener.GetContext();
						Thread t = new Thread(new ParameterizedThreadStart(OpenClient));
						t.IsBackground = true;
						t.Start(client);
#if !UNSAFE
					}
					catch (Exception ex)
					{
						log.Fatal("OpenServer() client", ex);
					}
#endif
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				log.Fatal("OpenServer() server", ex);
			}
#endif
		}

		private void CloseServer()
		{
			this.listener.Close();
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
			log.Debug("OpenClient() " + str);

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
					if (!tDic.ContainsKey("password") || HttpUtility.UrlDecode(tDic["password"]) != Settings.Instance.Password)
					{
						//throw new Exception("Password wrong!");
						client.Response.Close();
						return;
					}

					TCPClientRequest tMessage = (TCPClientRequest)int.Parse(tDic["request"]);

					#region DATA HANDLING

					string response = "";

					switch (tMessage)
					{
						# region VERSION

						case TCPClientRequest.Version:
							response = Settings.Instance.XgVersion;
							break;

						#endregion

						# region SERVER

						case TCPClientRequest.AddServer:
							this.ObjectRepository.AddServer(HttpUtility.UrlDecode(tDic["name"]));
							break;

						case TCPClientRequest.RemoveServer:
							this.RemoveServer(new Guid(tDic["guid"]));
							break;

						#endregion

						# region CHANNEL

						case TCPClientRequest.AddChannel:
							this.AddChannel(new Guid(tDic["guid"]), tDic["name"]);
							break;

						case TCPClientRequest.RemoveChannel:
							this.RemoveChannel(new Guid(tDic["guid"]));
							break;

						#endregion

						# region OBJECT

						case TCPClientRequest.ActivateObject:
							this.ActivateObject(new Guid(tDic["guid"]));
							break;

						case TCPClientRequest.DeactivateObject:
							this.DeactivateObject(new Guid(tDic["guid"]));
							break;

						#endregion

						# region SEARCH

						case TCPClientRequest.SearchPacket:
							client.Response.ContentType = "text/json";
							response = this.Objects2Json(
								this.GetPackets(tDic["offbots"] == "1", tDic["searchBy"], HttpUtility.UrlDecode(tDic["name"]), tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]), int.Parse(tDic["rows"]));
							break;

						case TCPClientRequest.SearchBot:
							client.Response.ContentType = "text/json";
							IEnumerable<XGPacket> tPacketList = this.GetPackets(tDic["offbots"] == "1", tDic["searchBy"], HttpUtility.UrlDecode(tDic["name"]), tDic["sidx"], tDic["sord"]);
							response = this.Objects2Json(
								(from s in this.ObjectRepository.Servers from c in s.Channels from b in c.Bots join p in tPacketList on b.Guid equals p.ParentGuid select b).Distinct(),
								int.Parse(tDic["page"]), int.Parse(tDic["rows"]));
							break;

						#endregion

						# region SEARCH SPECIAL

						case TCPClientRequest.AddSearch:
							this.Parent.AddSearch(HttpUtility.UrlDecode(tDic["name"]));
							break;

						case TCPClientRequest.RemoveSearch:
							this.Parent.RemoveSearch(HttpUtility.UrlDecode(tDic["name"]));
							break;

						case TCPClientRequest.GetSearches:
							response = this.Searches2Json(this.Searches);
							break;

						#endregion

						# region GET

						case TCPClientRequest.GetObject:
							client.Response.ContentType = "text/json";
							response = this.Object2Json(this.ObjectRepository.GetChildByGuid(new Guid(tDic["guid"])));
							break;

						case TCPClientRequest.GetServers:
							client.Response.ContentType = "text/json";
							response = this.Objects2Json(this.ObjectRepository.Servers, int.Parse(tDic["page"]), int.Parse(tDic["rows"]));
							break;

						case TCPClientRequest.GetChannelsFromServer:
							client.Response.ContentType = "text/json";
							response = this.Objects2Json(
								from server in this.ObjectRepository.Servers
								from channel in server.Channels
									where channel.ParentGuid == new Guid(tDic["guid"]) select channel,
								int.Parse(tDic["page"]), int.Parse(tDic["rows"]));
							break;

						case TCPClientRequest.GetBotsFromChannel:
							client.Response.ContentType = "text/json";
							response = this.Objects2Json(
								from server in this.ObjectRepository.Servers
								from channel in server.Channels
								from bot in channel.Bots
									where bot.ParentGuid == new Guid(tDic["guid"]) select bot,
								int.Parse(tDic["page"]), int.Parse(tDic["rows"]));
							break;

						case TCPClientRequest.GetPacketsFromBot:
							client.Response.ContentType = "text/json";
							response = this.Objects2Json(
								from server in this.ObjectRepository.Servers
								from channel in server.Channels
								from bot in channel.Bots
								from packet in bot.Packets
									where packet.ParentGuid == new Guid(tDic["guid"]) select packet,
								int.Parse(tDic["page"]), int.Parse(tDic["rows"]));
							break;

						case TCPClientRequest.GetStatistics:
							client.Response.ContentType = "text/json";
							response = this.Statistic2Json();
							break;

						#endregion

						# region COMMANDS

						case TCPClientRequest.CloseServer:
							this.Stop();
							break;

						#endregion

						# region XDCC Link

						case TCPClientRequest.ParseXdccLink:
							string[] link = HttpUtility.UrlDecode(tDic["name"]).Substring(7).Split('/');
							string serverName = link[0];
							string channelName = link[2];
							string botName = link[3];
							int packetId = int.Parse(link[4].Substring(1));

							// checking server
							XGServer serv = this.ObjectRepository[serverName];
							if(serv == null)
							{
								this.ObjectRepository.AddServer(serverName);
								serv = this.ObjectRepository[serverName];
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
							XGBot tBot = chan[botName];
							if (tBot == null)
							{
								tBot = new XGBot();
								tBot.Name = botName;
								chan.AddBot(tBot);
							}

							// checking packet
							XGPacket pack = tBot[packetId];
							if(pack == null)
							{
								pack = new XGPacket();
								pack.Id = packetId;
								pack.Name = link[5];
								tBot.AddPacket(pack);
							}
							pack.Enabled = true;
							break;

						#endregion
					}

					this.WriteToStream(client.Response, response);

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
				try
				{
					client.Response.Close();
				}
				catch {}
				log.Fatal("OpenClient(" + str + ")", ex);
			}
#endif
			log.Info("OpenClient() disconnected");
		}

		#endregion

		private IEnumerable<XGPacket> GetPackets(bool aShowOffBots, string aSearchBy, string aSearchString, string aSortBy, string aSortMode)
		{
			IEnumerable<XGBot> bots = from server in this.ObjectRepository.Servers from channel in server.Channels from bot in channel.Bots select bot;
			if(aShowOffBots)
			{
				bots = from bot in bots where bot.Connected select bot;
			}
			IEnumerable<XGPacket> tPackets = from bot in bots from packet in bot.Packets select packet;

			switch(aSearchBy)
			{
				case "name":
					string[] searches = aSearchString.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string currentSearch in searches)
					{
						tPackets = (from packet in tPackets where packet.Name.ToLower().Contains(currentSearch.ToLower()) select packet).ToArray();
					}
					break;

				case "time":
					string[] search = aSearchString.Split('-');
					double start = Double.Parse(search[0]);
					double stop = Double.Parse(search[1]);
					DateTime init = new DateTime(1, 1, 1);
					DateTime now = DateTime.Now;

					tPackets =
						from packet in tPackets where
							packet.LastUpdated != init &&
							start <= (now - packet.LastUpdated).TotalMilliseconds &&
							stop >= (now - packet.LastUpdated).TotalMilliseconds
						select packet;
					break;

				case "connected":
					tPackets = from packet in tPackets where packet.Connected select packet;
					break;

				case "enabled":
					tPackets = from packet in tPackets where packet.Enabled select packet;
					break;
			}

			switch (aSortBy)
			{
				case "Name":
					tPackets = from packet in tPackets orderby packet.Name select packet;
					break;

				case "Connected":
					tPackets = from packet in tPackets orderby packet.Connected select packet;
					break;

				case "Enabled":
					tPackets = from packet in tPackets orderby packet.Enabled select packet;
					break;

				case "Id":
					tPackets = from packet in tPackets orderby packet.Id select packet;
					break;

				case "Size":
					tPackets = from packet in tPackets orderby packet.Size select packet;
					break;

				case "LastUpdated":
					tPackets = from packet in tPackets orderby packet.LastUpdated select packet;
					break;
			}

			if (aSortMode == "desc")
			{
				tPackets = tPackets.Reverse();
			}

			return tPackets;
		}

		#region WRITE TO STREAM

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

		private string Objects2Json(IEnumerable<XGObject> aObjects, int aPage, int aRows)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append("\"page\":\"" + aPage + "\",");
			sb.Append("\"total\":\"" + Math.Ceiling((double)aObjects.Count() / (double)aRows).ToString() + "\",");
			sb.Append("\"records\":\"" + aObjects.Count() + "\",");
			sb.Append("\"rows\":[");

			List<string> tList = new List<string>();
			foreach (XGObject tObj in aObjects.Skip((aPage - 1) * aRows).Take(aRows))
			{
				tList.Add(this.Object2Json(tObj));
			}
			sb.Append(string.Join(",", tList));

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
				sb.Append("\"ErrorCode\":\"" + tServ.ErrorCode + "\",");
				sb.Append("\"level\":0,");
				sb.Append("\"parent\":0,");
				sb.Append("\"isLeaf\":false,");
				sb.Append("\"loaded\":true");
			}
			else if (aObject.GetType() == typeof(XGChannel))
			{
				XGChannel tChan = (XGChannel)aObject;

				sb.Append("\"Name\":\"" + aObject.Name + "\",");
				sb.Append("\"ErrorCode\":\"" + tChan.ErrorCode + "\",");
				sb.Append("\"level\":1,");
				sb.Append("\"parent\":\"" + aObject.ParentGuid + "\",");
				sb.Append("\"isLeaf\":true,");
				sb.Append("\"loaded\":true");
			}
			else if (aObject.GetType() == typeof(XGBot))
			{
				XGBot tBot = (XGBot)aObject;
				double speed = 0;
				try
				{
					foreach(XGFile file in this.FileRepository.Files)
					{
						foreach(XGFilePart filePart in file.Parts)
						{
							if(filePart.Packet != null && filePart.Packet.ParentGuid == tBot.Guid)
							{
								speed += filePart.Speed;
							}
						}
					}
					//speed = (from file in this.FileRepository.Files from part in file.Parts where part.Packet.ParentGuid == tBot.Guid select part.Speed).Sum();
				}
				catch {}

				sb.Append("\"Name\":\"" + this.ClearString(aObject.Name) + "\",");
				sb.Append("\"BotState\":\"" + tBot.BotState + "\",");
				sb.Append("\"Speed\":" + speed.ToString("0.00").Replace(",", ".") + ",");
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

				XGFilePart tPart = null;
				try
				{
					foreach(XGFile file in this.FileRepository.Files)
					{
						foreach(XGFilePart filePart in file.Parts)
						{
							if(filePart.Packet != null && filePart.Packet.Guid == tPack.Guid)
							{
								tPart = filePart;
								break;
							}
						}
					}
					//tPart = (from file in this.FileRepository.Files from part in file.Parts where part.Packet != null && part.Packet.Guid == tPack.Guid select part).SingleOrDefault();
				}
				catch {}

				sb.Append("\"Id\":" + tPack.Id + ",");
				sb.Append("\"Name\":\"" + this.ClearString(tPack.RealName != "" ? tPack.RealName : tPack.Name) + "\",");
				sb.Append("\"Size\":" + (tPack.RealSize > 0 ? tPack.RealSize : tPack.Size) + ",");
				sb.Append("\"Speed\":" + (tPart == null ? "0" : tPart.Speed.ToString("0.00").Replace(",", ".")) + ",");
				sb.Append("\"TimeMissing\":" + (tPart == null ? "0" : tPart.TimeMissing.ToString()) + ",");
				sb.Append("\"StartSize\":" + (tPart == null ? "0" : tPart.StartSize.ToString()) + ",");
				sb.Append("\"StopSize\":" + (tPart == null ? "0" : tPart.StopSize.ToString()) + ",");
				sb.Append("\"CurrentSize\":" + (tPart == null ? "0" : tPart.CurrentSize.ToString()) + ",");
				sb.Append("\"IsChecked\":\"" + (tPart == null ? "false" : tPart.IsChecked ? "true" : "false") + "\",");
				sb.Append("\"Order\":\"" + (tPack.Parent.GetOldestActivePacket() != tPack ? "false" : "true") + "\",");
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
	
		#region EVENTHANDLER

		protected override void ObjectRepository_ObjectAddedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void ObjectRepository_ObjectRemovedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void ObjectRepository_ObjectChangedEventHandler(XGObject aObj)
		{
		}

		protected override void FileRepository_ObjectAddedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void FileRepository_ObjectRemovedEventHandler (XGObject aParentObj, XGObject aObj)
		{
		}

		protected override void FileRepository_ObjectChangedEventHandler(XGObject aObj)
		{
		}

		#endregion
	}
}
