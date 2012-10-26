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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;

using log4net;

using XG.Core;

namespace XG.Server.Plugin.General.Webserver
{
	public class BrowserConnection : APlugin
	{
		#region VARIABLES
		
		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public HttpListenerContext Context { get; set; }

		public FileLoader FileLoader;
		public string Salt;

		#endregion

		#region AWorker

		protected override void StartRun()
		{			
			Dictionary<string, string> tDic = new Dictionary<string, string>();
			string str = Context.Request.RawUrl;
			_log.Debug("StartRun() " + str);
			
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
					byte[] inputBytes = Encoding.UTF8.GetBytes(Salt + Settings.Instance.Password + Salt);
					byte[] hashedBytes = new SHA256Managed().ComputeHash(inputBytes);
					string passwortHash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
					if (!tDic.ContainsKey("password") || HttpUtility.UrlDecode(tDic["password"]) != passwortHash)
					{
						// check for cookie
						Cookie cookie = Context.Request.Cookies["xg.password"];
						if (cookie == null || cookie.Value != passwortHash)
						{
							Context.Response.AppendCookie(new Cookie("xg.password", ""));
							Context.Response.StatusCode = 403;
							Context.Response.Close();
							return;
						}
					}
					else
					{
						Context.Response.AppendCookie(new Cookie("xg.password", HttpUtility.UrlDecode(tDic["password"])));
					}
					
					ClientRequest tMessage = (ClientRequest)int.Parse(tDic["request"]);
					
					#region DATA HANDLING
					
					string response = "";
					Context.Response.AddHeader("Cache-Control", "no-cache");

					bool showOfflineBots = Context.Request.Cookies["xg.show_offline_bots"] == null ? true : 
						(
							Context.Request.Cookies["xg.show_offline_bots"].Value == "1" ? true : false
						);
					Guid guid = Guid.Empty;
					try
					{
						guid = new Guid(tDic["guid"]);
					}
					catch (FormatException) {}
					catch (KeyNotFoundException) {}

					switch (tMessage)
					{
						# region VERSION
						
						case ClientRequest.Version:
							Context.Response.ContentType = "text/plain";
							response = Settings.Instance.XgVersion;
							break;
							
						#endregion

						# region SERVER
							
						case ClientRequest.AddServer:
							AddServer(HttpUtility.UrlDecode(tDic["name"]));
							break;
							
						case ClientRequest.RemoveServer:
							RemoveServer(guid);
							break;
							
						#endregion
							
						# region CHANNEL
							
						case ClientRequest.AddChannel:
							AddChannel(guid, tDic["name"]);
							break;
							
						case ClientRequest.RemoveChannel:
							RemoveChannel(guid);
							break;
							
						#endregion
							
						# region OBJECT
							
						case ClientRequest.ActivateObject:
							ActivateObject(guid);
							break;
							
						case ClientRequest.DeactivateObject:
							DeactivateObject(guid);
							break;
							
						#endregion
							
						# region SEARCH
							
						case ClientRequest.SearchPacket:
							Context.Response.ContentType = "text/json";
							response = Objects2Json(
								Packets(showOfflineBots, tDic["searchBy"], HttpUtility.UrlDecode(tDic["name"]), tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
								);
							break;
							
						case ClientRequest.SearchBot:
							Context.Response.ContentType = "text/json";
							response = Objects2Json(
								Bots(showOfflineBots, tDic["searchBy"], HttpUtility.UrlDecode(tDic["name"]), tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
								);
							break;
							
						case ClientRequest.AddSearch:
							string name = HttpUtility.UrlDecode(tDic["name"]);
							Core.Object obj = Searches.Named(name);
							if(obj == null)
							{
								obj = new Core.Object();
								obj.Name = name;
								Searches.Add(obj);
							}
							break;
							
						case ClientRequest.RemoveSearch:
							Searches.Remove(Searches.Named(HttpUtility.UrlDecode(tDic["name"])));
							break;
							
						case ClientRequest.Searches:
							Context.Response.ContentType = "text/json";
							response = Searches2Json(Searches);
							break;
							
						#endregion
							
						# region GET
							
						case ClientRequest.Object:
							Context.Response.ContentType = "text/json";
							response = Object2Json(
								Servers.WithGuid(guid)
								);
							break;
							
						case ClientRequest.Servers:
							Context.Response.ContentType = "text/json";
							response = Objects2Json(
								SortedObjects(Servers.All, tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
								);
							break;
							
						case ClientRequest.ChannelsFromServer:
							Context.Response.ContentType = "text/json";
							response = Objects2Json(
								SortedObjects(
									from server in Servers.All
									from channel in server.Channels
									where channel.ParentGuid == guid select channel, tDic["sidx"], tDic["sord"]
								),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
								);
							break;
							
						case ClientRequest.BotsFromChannel:
							Context.Response.ContentType = "text/json";
							response = Objects2Json(
								SortedBots(
									from server in Servers.All
									from channel in server.Channels
									from bot in channel.Bots
									where bot.ParentGuid == guid select bot, tDic["sidx"], tDic["sord"]
								),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
								);
							break;
							
						case ClientRequest.PacketsFromBot:
							Context.Response.ContentType = "text/json";
							response = Objects2Json(
								SortedPackets(
									from server in Servers.All
									from channel in server.Channels
									from bot in channel.Bots
									from packet in bot.Packets
									where packet.ParentGuid == guid select packet, tDic["sidx"], tDic["sord"]
								),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
								);
							break;
							
						case ClientRequest.Statistics:
							Context.Response.ContentType = "text/json";
							response = Statistic2Json();
							break;
							
						case ClientRequest.GetSnapshots:
							Context.Response.ContentType = "text/json";
							response = Snapshots2Json(Snapshots);
							break;
							
						case ClientRequest.Files:
							Context.Response.ContentType = "text/json";
							response = Objects2Json(
								SortedFiles(Files.All, tDic["sidx"], tDic["sord"]),
								int.Parse(tDic["page"]),
								int.Parse(tDic["rows"])
								);
							break;
							
						#endregion
							
						# region COMMANDS
							
						case ClientRequest.CloseServer:
							break;
							
						#endregion
							
						# region XDCC Link
							
						case ClientRequest.ParseXdccLink:
							string[] link = HttpUtility.UrlDecode(tDic["name"]).Substring(7).Split('/');
							string serverName = link[0];
							string channelName = link[2];
							string botName = link[3];
							int packetId = int.Parse(link[4].Substring(1));
							
							// checking server
							Core.Server serv = Servers.Server(serverName);
							if(serv == null)
							{
								Servers.Add(serverName);
								serv = Servers.Server(serverName);
							}
							serv.Enabled = true;
							
							// checking channel
							Channel chan = serv.Channel(channelName);
							if(chan == null)
							{
								serv.AddChannel(channelName);
								chan = serv.Channel(channelName);
							}
							chan.Enabled = true;
							
							// checking bot
							Bot tBot = chan.Bot(botName);
							if (tBot == null)
							{
								tBot = new Bot();
								tBot.Name = botName;
								chan.AddBot(tBot);
							}
							
							// checking packet
							Packet pack = tBot.Packet(packetId);
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
					
					WriteToStream(response);
					
					#endregion
				}
				else
				{
					// serve the favicon
					if (str == "/favicon.ico")
					{
						WriteToStream(FileLoader.LoadImage(str));
					}
					// load a file
					else
					{
						if (str.Contains("?")) { str = str.Split('?')[0]; }
						if (str == "/") { str = "/index.html"; }
						
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
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				try
				{
					Context.Response.Close();
				}
				catch {}
				_log.Fatal("StartRun(" + str + ")", ex);
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
				using(MemoryStream ms = new MemoryStream())
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
		
		#region JSON
		
		IEnumerable<Bot> Bots(bool aShowOfflineBots, string aSearchBy, string aSearchString, string aSortBy, string aSortMode)
		{
			IEnumerable<Packet> tPacketList = Packets(aShowOfflineBots, aSearchBy, aSearchString, aSortBy, aSortMode);
			IEnumerable<Bot> tBots = (
				from s in Servers.All 
				from c in s.Channels from b in c.Bots join p in tPacketList on b.Guid equals p.ParentGuid select b
				).Distinct();
			
			return SortedBots(tBots, aSortBy, aSortMode);
		}
		
		IEnumerable<Bot> SortedBots(IEnumerable<Bot> aBots, string aSortBy, string aSortMode)
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
		
		IEnumerable<Packet> Packets(bool aShowOfflineBots, string aSearchBy, string aSearchString, string aSortBy, string aSortMode)
		{
			IEnumerable<Bot> bots = from server in Servers.All from channel in server.Channels from bot in channel.Bots select bot;
			if(aShowOfflineBots)
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
					int start = int.Parse(search[0]);
					int stop = int.Parse(search[1]);
					DateTime init = DateTime.MinValue;
					DateTime now = DateTime.Now;
					
					tPackets =
						from packet in tPackets where
							packet.LastUpdated != init &&
							start <= (now - packet.LastUpdated).TotalSeconds &&
							stop >= (now - packet.LastUpdated).TotalSeconds
							select packet;
					break;
					
				case "connected":
					tPackets = from packet in tPackets where packet.Connected select packet;
					break;
					
				case "enabled":
					tPackets = from packet in tPackets where packet.Enabled select packet;
					break;
			}
			
			return SortedPackets(tPackets, aSortBy, aSortMode);
		}
		
		IEnumerable<Packet> SortedPackets(IEnumerable<Packet> aPackets, string aSortBy, string aSortMode)
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
		
		IEnumerable<Core.File> SortedFiles(IEnumerable<Core.File> aFiles, string aSortBy, string aSortMode)
		{
			switch (aSortBy)
			{
				case "Name":
					aFiles = from packet in aFiles orderby packet.Name select packet;
					break;
					
				case "Connected":
					aFiles = from packet in aFiles orderby packet.Connected select packet;
					break;
					
				case "Enabled":
					aFiles = from packet in aFiles orderby packet.Enabled select packet;
					break;
					
				case "Size":
					aFiles = from packet in aFiles orderby packet.Size select packet;
					break;
			}
			
			if (aSortMode == "desc")
			{
				aFiles = aFiles.Reverse();
			}
			
			return aFiles;
		}
		
		IEnumerable<AObject> SortedObjects(IEnumerable<AObject> aObjects, string aSortBy, string aSortMode)
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
		
		string Searches2Json(Objects aObjects)
		{
			StringBuilder sb = new StringBuilder();
			
			sb.Append("{");
			sb.Append("\"Searches\":[");
			
			int count = 0;
			foreach (AObject obj in aObjects.All)
			{
				count++;
				sb.Append("{\"Search\": \"" + obj.Name + "\"}");
				if(count < aObjects.All.Count()) { sb.Append(","); }
				sb.Append("");
			}
			
			sb.Append("]}");
			
			return sb.ToString();
		}
		
		string Objects2Json(IEnumerable<AObject> aObjects, int aPage, int aRows)
		{
			var tObjects = new Server.Plugin.General.Webserver.JQGrid.Objects();
			tObjects.Page = aPage;
			tObjects.Rows = aRows;
			tObjects.SetObjects(aObjects);
			return Json.Serialize<Server.Plugin.General.Webserver.JQGrid.Objects>(tObjects);
		}
		
		string Object2Json(AObject aObject)
		{
			if(aObject == null)
			{
				return "";
			}
			
			var tObject = new Server.Plugin.General.Webserver.JQGrid.Object(aObject);
			return Json.Serialize<Server.Plugin.General.Webserver.JQGrid.Object>(tObject);
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

		string Snapshots2Json(Snapshots aSnapshots)
		{
			var tObjects = new List<Server.Plugin.General.Webserver.Flot.Object>();
			for (int a = 1; a <= 24; a++)
			{
				var value = (SnapshotValue)a;

				var obj = new Server.Plugin.General.Webserver.Flot.Object();
				
				var list = new List<Int64[]>();
				foreach (Snapshot snapshot in aSnapshots.All)
				{
					Int64[] data = {snapshot.Get(SnapshotValue.Timestamp) * 1000, snapshot.Get(value)};
					list.Add(data);
				}
				obj.Data = FilterDuplicateEntries(list.ToArray());
				obj.Label = Enum.GetName(typeof(SnapshotValue), value);

				tObjects.Add(obj);
			}
			
			return Json.Serialize<Server.Plugin.General.Webserver.Flot.Object[]>(tObjects.ToArray());
		}

		Int64[][] FilterDuplicateEntries(Int64[][] aEntries)
		{
			var list = new List<Int64[]>();

			Int64[] prev = {0, -1};
			Int64[] stack = null;
			foreach (Int64[] data in aEntries)
			{
				if (prev[1] != data[1])
				{
					if (stack != null)
					{
						list.Add(stack);
						stack = null;
					}
					list.Add(data);
					prev = data;
				}
				else
				{
					stack = data;
				}
			}
			if (stack != null)
			{
				list.Add(stack);
			}

			return list.ToArray();
		}
		
		#endregion
	}
}

