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

namespace XG.Server.Plugin.General.Webserver
{
	public class Plugin : APlugin
	{
		#region VARIABLES

		static readonly ILog log = LogManager.GetLogger(typeof(Plugin));

		Thread serverThread;
		HttpListener listener;

		#endregion

		#region RUN STOP

		/// <summary>
		/// Run method - opens itself in a new thread
		/// </summary>
		public override void Start()
		{
			serverThread = new Thread(new ThreadStart(OpenServer));
			serverThread.Start();
		}

		/// <summary>
		/// called if the client signals to stop
		/// </summary>
		public override void Stop()
		{
			CloseServer();
			serverThread.Abort();
		}

		#endregion

		#region SERVER

		/// <summary>
		/// Opens the server port, waiting for clients
		/// </summary>
		void OpenServer()
		{
			listener = new HttpListener();
#if !UNSAFE
			try
			{
#endif
				listener.Prefixes.Add("http://*:" + (Settings.Instance.WebServerPort) + "/");
				listener.Start();

				while (true)
				{
#if !UNSAFE
					try
					{
#endif
						HttpListenerContext client = listener.GetContext();
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

		void CloseServer()
		{
			listener.Close();
		}

		#endregion

		#region CLIENT

		/// <summary>
		/// Called if a client connects
		/// </summary>
		/// <param name="aObject"></param>
		void OpenClient(object aObject)
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
						client.Response.StatusCode = 403;
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
							AddServer(HttpUtility.UrlDecode(tDic["name"]));
							break;

						case TCPClientRequest.RemoveServer:
							RemoveServer(new Guid(tDic["guid"]));
							break;

						#endregion

						# region CHANNEL

						case TCPClientRequest.AddChannel:
							AddChannel(new Guid(tDic["guid"]), tDic["name"]);
							break;

						case TCPClientRequest.RemoveChannel:
							RemoveChannel(new Guid(tDic["guid"]));
							break;

						#endregion

						# region OBJECT

						case TCPClientRequest.ActivateObject:
							ActivateObject(new Guid(tDic["guid"]));
							break;

						case TCPClientRequest.DeactivateObject:
							DeactivateObject(new Guid(tDic["guid"]));
							break;

						#endregion

						# region SEARCH

						case TCPClientRequest.SearchPacket:
							client.Response.ContentType = "text/json";
							response = Objects2Json(
								GetPackets(tDic["offbots"] == "1", tDic["searchBy"], HttpUtility.UrlDecode(tDic["name"]), tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
							);
							break;

						case TCPClientRequest.SearchBot:
							client.Response.ContentType = "text/json";
							response = Objects2Json(
								GetBots(tDic["offbots"] == "1", tDic["searchBy"], HttpUtility.UrlDecode(tDic["name"]), tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
							);
							break;

						#endregion

						# region SEARCH SPECIAL

						case TCPClientRequest.AddSearch:
							string name = HttpUtility.UrlDecode(tDic["name"]);
							XG.Core.Object obj = Searches.ByName(name);
							if(obj == null)
							{
								obj = new XG.Core.Object();
								obj.Name = name;
								Searches.Add(obj);
							}
							break;

						case TCPClientRequest.RemoveSearch:
							Searches.Remove(Searches.ByName(HttpUtility.UrlDecode(tDic["name"])));
							break;

						case TCPClientRequest.GetSearches:
							response = Searches2Json(Searches);
							break;

						#endregion

						# region GET

						case TCPClientRequest.GetObject:
							client.Response.ContentType = "text/json";
							response = Object2Json(
								Servers.ByGuid(new Guid(tDic["guid"]))
							);
							break;

						case TCPClientRequest.GetServers:
							client.Response.ContentType = "text/json";
							response = Objects2Json(
								GetSortedObjects(Servers.All, tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
							);
							break;

						case TCPClientRequest.GetChannelsFromServer:
							client.Response.ContentType = "text/json";
							response = Objects2Json(
								GetSortedObjects(
									from server in Servers.All
									from channel in server.Channels
										where channel.ParentGuid == new Guid(tDic["guid"]) select channel, tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
							);
							break;

						case TCPClientRequest.GetBotsFromChannel:
							client.Response.ContentType = "text/json";
							response = Objects2Json(
								GetSortedBots(
									from server in Servers.All
									from channel in server.Channels
									from bot in channel.Bots
										where bot.ParentGuid == new Guid(tDic["guid"]) select bot, tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
							);
							break;

						case TCPClientRequest.GetPacketsFromBot:
							client.Response.ContentType = "text/json";
							response = Objects2Json(
								GetSortedPackets(
									from server in Servers.All
									from channel in server.Channels
									from bot in channel.Bots
									from packet in bot.Packets
										where packet.ParentGuid == new Guid(tDic["guid"]) select packet, tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
							);
							break;

						case TCPClientRequest.GetStatistics:
							client.Response.ContentType = "text/json";
							response = Statistic2Json();
							break;

						#endregion

						# region COMMANDS

						case TCPClientRequest.CloseServer:
							Stop();
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
							XG.Core.Server serv = Servers[serverName];
							if(serv == null)
							{
								Servers.Add(serverName);
								serv = Servers[serverName];
							}
							serv.Enabled = true;

							// checking channel
							Channel chan = serv[channelName];
							if(chan == null)
							{
								serv.AddChannel(channelName);
								chan = serv[channelName];
							}
							chan.Enabled = true;

							// checking bot
							Bot tBot = chan[botName];
							if (tBot == null)
							{
								tBot = new Bot();
								tBot.Name = botName;
								chan.AddBot(tBot);
							}

							// checking packet
							Packet pack = tBot[packetId];
							if(pack == null)
							{
								pack = new Packet();
								pack.Id = packetId;
								pack.Name = link[5];
								tBot.AddPacket(pack);
							}
							pack.Enabled = true;
							break;

						#endregion
					}

					WriteToStream(client.Response, response);

					#endregion
				}
				else
				{
					// load an image
					if (str.StartsWith("/image&"))
					{
						WriteToStream(client.Response, ImageLoaderWeb.Instance.GetImage(str.Split('&')[1]));
					}
					// serve the favicon
					else if (str == "/favicon.ico")
					{
						WriteToStream(client.Response, ImageLoaderWeb.Instance.GetImage("Client"));
					}
					// load a file
					else
					{
						if (str.Contains("?")) { str = str.Split('?')[0]; }
						if (str == "/") { str = "/index.html"; }

						if (str.EndsWith(".png"))
						{
							WriteToStream(client.Response, FileLoaderWeb.Instance.LoadImage(str));
						}
						else
						{
							WriteToStream(client.Response, FileLoaderWeb.Instance.LoadFile(str, client.Request.UserLanguages));
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
		}

		IEnumerable<Packet> GetPackets(bool aShowOffBots, string aSearchBy, string aSearchString, string aSortBy, string aSortMode)
		{
			IEnumerable<Bot> bots = from server in Servers.All from channel in server.Channels from bot in channel.Bots select bot;
			if(aShowOffBots)
			{
				bots = from bot in bots where bot.Connected select bot;
			}
			IEnumerable<Packet> tPackets = from bot in bots from packet in bot.Packets select packet;

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

			return GetSortedPackets(tPackets, aSortBy, aSortMode);
		}

		IEnumerable<Packet> GetSortedPackets(IEnumerable<Packet> aPackets, string aSortBy, string aSortMode)
		{
			switch (aSortBy)
			{
				case "Name":
					aPackets = from packet in aPackets orderby packet.Name select packet;
					break;

				case "Connected":
					aPackets = from packet in aPackets orderby packet.Connected select packet;
					break;

				case "Enabled":
					aPackets = from packet in aPackets orderby packet.Enabled select packet;
					break;

				case "Id":
					aPackets = from packet in aPackets orderby packet.Id select packet;
					break;

				case "Size":
					aPackets = from packet in aPackets orderby packet.Size select packet;
					break;

				case "LastUpdated":
					aPackets = from packet in aPackets orderby packet.LastUpdated select packet;
					break;

				case "Speed":
					aPackets = from packet in aPackets orderby (packet.Part != null ? packet.Part.Speed : 0) select packet;
					break;

				case "TimeMissing":
					aPackets = from packet in aPackets orderby (packet.Part != null ? packet.Part.TimeMissing : 0) select packet;
					break;
			}

			if (aSortMode == "desc")
			{
				aPackets = aPackets.Reverse();
			}

			return aPackets;
		}

		IEnumerable<Bot> GetBots(bool aShowOffBots, string aSearchBy, string aSearchString, string aSortBy, string aSortMode)
		{
			IEnumerable<Packet> tPacketList = GetPackets(aShowOffBots, aSearchBy, aSearchString, aSortBy, aSortMode);
			IEnumerable<Bot> tBots = (
				from s in Servers.All 
					from c in s.Channels from b in c.Bots join p in tPacketList on b.Guid equals p.ParentGuid select b
				).Distinct();

			return GetSortedBots(tBots, aSortBy, aSortMode);
		}

		IEnumerable<Bot> GetSortedBots(IEnumerable<Bot> aBots, string aSortBy, string aSortMode)
		{
			switch (aSortBy)
			{
				case "Name":
					aBots = from bot in aBots orderby bot.Name select bot;
					break;

				case "Connected":
					aBots = from bot in aBots orderby bot.Connected select bot;
					break;

				case "Enabled":
					aBots = from bot in aBots orderby bot.Enabled select bot;
					break;

				case "Speed":
					aBots = from bot in aBots orderby bot.Speed select bot;
					break;

				case "QueuePosition":
					aBots = from bot in aBots orderby bot.QueuePosition select bot;
					break;

				case "QueueTime":
					aBots = from bot in aBots orderby bot.QueueTime select bot;
					break;

				case "InfoSpeedMax":
					aBots = from bot in aBots orderby bot.InfoSpeedCurrent, bot.InfoSpeedMax select bot;
					break;

				case "InfoSlotTotal":
					aBots = from bot in aBots orderby bot.InfoSlotCurrent, bot.InfoSlotTotal select bot;
					break;

				case "InfoQueueTotal":
					aBots = from bot in aBots orderby bot.InfoQueueCurrent, bot.InfoQueueTotal select bot;
					break;
			}

			if (aSortMode == "desc")
			{
				aBots = aBots.Reverse();
			}

			return aBots;
		}

		IEnumerable<AObject> GetSortedObjects(IEnumerable<AObject> aObjects, string aSortBy, string aSortMode)
		{
			switch (aSortBy)
			{
				case "Name":
					aObjects = from obj in aObjects orderby obj.Name select obj;
					break;

				/*case "Connected":
					aObjects = from obj in aObjects orderby obj.Connected select obj;
					break;*/

				case "Enabled":
					aObjects = from obj in aObjects orderby obj.Enabled select obj;
					break;
			}

			if (aSortMode == "desc")
			{
				aObjects = aObjects.Reverse();
			}

			return aObjects;
		}

		#endregion

		#region WRITE TO STREAM

		void WriteToStream(HttpListenerResponse aResponse, string aData)
		{
			WriteToStream(aResponse, Encoding.UTF8.GetBytes(aData));
		}

		void WriteToStream(HttpListenerResponse aResponse, byte[] aData)
		{
			aResponse.ContentLength64 = aData.Length;
			aResponse.OutputStream.Write(aData, 0, aData.Length);
			aResponse.OutputStream.Close();
		}

		#endregion

		#region JSON

		string Searches2Json(Objects aObjects)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append("\"Searches\":[");

			int count = 0;
			foreach (AObject obj in aObjects.All)
			{
				count++;
				sb.Append("{\"Search\": \"" + ClearString(obj.Name) + "\"}");
				if(count < aObjects.All.Count()) { sb.Append(","); }
				sb.Append("");
			}

			sb.Append("]}");

			return sb.ToString();
		}

		string Objects2Json(IEnumerable<AObject> aObjects, int aPage, int aRows)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append("\"page\":\"" + aPage + "\",");
			sb.Append("\"total\":\"" + Math.Ceiling((double)aObjects.Count() / (double)aRows).ToString() + "\",");
			sb.Append("\"records\":\"" + aObjects.Count() + "\",");
			sb.Append("\"rows\":[");

			List<string> tList = new List<string>();
			foreach (AObject tObj in aObjects.Skip((aPage - 1) * aRows).Take(aRows))
			{
				tList.Add(Object2Json(tObj));
			}
			sb.Append(string.Join(",", tList));

			sb.Append("]}");

			return sb.ToString();
		}

		string Object2Json(AObject aObject)
		{
			if(aObject == null)
			{
				return "";
			}

			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append("\"id\":\"" + aObject.Guid.ToString() + "\",");
			sb.Append("\"cell\":{");

			// push out an icon row - otherwise the wont update itself correctly m(
			sb.Append("\"Icon\":\"\",");
			sb.Append("\"ParentGuid\":\"" + aObject.ParentGuid.ToString() + "\",");
			sb.Append("\"Connected\":\"" + aObject.Connected.ToString().ToLower() + "\",");
			sb.Append("\"Enabled\":\"" + aObject.Enabled.ToString().ToLower() + "\",");
			sb.Append("\"LastModified\":\"" + aObject.EnabledTime + "\",");

			if (aObject is XG.Core.Server)
			{
				XG.Core.Server tServ = (XG.Core.Server)aObject;

				sb.Append("\"Name\":\"" + aObject.Name + ":" + tServ.Port + "\",");
				sb.Append("\"ErrorCode\":\"" + tServ.ErrorCode + "\",");
				sb.Append("\"level\":0,");
				sb.Append("\"parent\":0,");
				sb.Append("\"isLeaf\":false,");
				sb.Append("\"loaded\":true");
			}
			else if (aObject is Channel)
			{
				Channel tChan = (Channel)aObject;

				sb.Append("\"Name\":\"" + aObject.Name + "\",");
				sb.Append("\"ErrorCode\":\"" + tChan.ErrorCode + "\",");
				sb.Append("\"level\":1,");
				sb.Append("\"parent\":\"" + aObject.ParentGuid + "\",");
				sb.Append("\"isLeaf\":true,");
				sb.Append("\"loaded\":true");
			}
			else if (aObject is Bot)
			{
				Bot tBot = (Bot)aObject;

				sb.Append("\"Name\":\"" + ClearString(aObject.Name) + "\",");
				sb.Append("\"BotState\":\"" + tBot.BotState + "\",");
				sb.Append("\"Speed\":" + tBot.Speed.ToString("0.00").Replace(",", ".") + ",");
				sb.Append("\"QueQueuePosition\":" + tBot.QueuePosition + ",");
				sb.Append("\"QueueTime\":" + tBot.QueueTime + ",");
				sb.Append("\"InfoSpeedMax\":" + tBot.InfoSpeedMax.ToString().Replace(',', '.') + ",");
				sb.Append("\"InfoSpeedCurrent\":" + tBot.InfoSpeedCurrent.ToString().Replace(',', '.') + ",");
				sb.Append("\"InfoSlotTotal\":" + tBot.InfoSlotTotal + ",");
				sb.Append("\"InfoSlotCurrent\":" + tBot.InfoSlotCurrent + ",");
				sb.Append("\"InfoQueueTotal\":" + tBot.InfoQueueTotal + ",");
				sb.Append("\"InfoQueueCurrent\":" + tBot.InfoQueueCurrent + ",");
				sb.Append("\"LastMessage\":\"" + ClearString(tBot.LastMessage) + "\",");
				sb.Append("\"LastContact\":\"" + tBot.LastContact + "\"");
			}
			else if (aObject is Packet)
			{
				Packet tPack = (Packet)aObject;

				sb.Append("\"Id\":" + tPack.Id + ",");
				sb.Append("\"Name\":\"" + ClearString(tPack.RealName != "" ? tPack.RealName : tPack.Name) + "\",");
				sb.Append("\"Size\":" + (tPack.RealSize > 0 ? tPack.RealSize : tPack.Size) + ",");
				sb.Append("\"Speed\":" + (tPack.Part == null ? "0" : tPack.Part.Speed.ToString("0.00").Replace(",", ".")) + ",");
				sb.Append("\"TimeMissing\":" + (tPack.Part == null ? "0" : tPack.Part.TimeMissing.ToString()) + ",");
				sb.Append("\"StartSize\":" + (tPack.Part == null ? "0" : tPack.Part.StartSize.ToString()) + ",");
				sb.Append("\"StopSize\":" + (tPack.Part == null ? "0" : tPack.Part.StopSize.ToString()) + ",");
				sb.Append("\"CurrentSize\":" + (tPack.Part == null ? "0" : tPack.Part.CurrentSize.ToString()) + ",");
				sb.Append("\"IsChecked\":\"" + (tPack.Part == null ? "false" : tPack.Part.IsChecked ? "true" : "false") + "\",");
				sb.Append("\"Order\":\"" + (tPack.Parent.OldestActivePacket() != tPack ? "false" : "true") + "\",");
				sb.Append("\"LastUpdated\":\"" + tPack.LastUpdated + "\"");
			}

			sb.Append("}}");

			return sb.ToString();
		}

		string Statistic2Json()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{");

			List<StatisticType> types = new List<StatisticType>();
			// uncool, but works
			for (int a = (int)StatisticType.BytesLoaded; a <= (int)StatisticType.SpeedMax; a++)
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

		Regex myClearRegex = new Regex(@"[^A-Za-z0-9äÄöÖüÜß _.\[\]\{\}\(\)-]");
		string ClearString(string aString)
		{
			string str = myClearRegex.Replace(aString, "");
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
