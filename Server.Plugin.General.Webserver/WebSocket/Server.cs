//
//  Server.cs
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

using Alchemy;
using Alchemy.Classes;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using XG.Core;

using log4net;

namespace XG.Server.Plugin.General.Webserver.WebSocket
{
	public class Server : APlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		WebSocketServer _webSocket;

		public string Salt;

		#endregion

		#region AWorker

		protected override void StartRun ()
		{
			_webSocket = new WebSocketServer(Settings.Instance.WebServerPort + 1, IPAddress.Any)
			{
				OnReceive = OnReceive,
				OnConnect = OnConnect,
				OnDisconnect = OnDisconnect,
				TimeOut = new TimeSpan(0, 5, 0)
			};
		}

		protected override void StopRun()
		{
			_webSocket.Stop();
		}

		#endregion

		#region REPOSITORY EVENTS

		protected override void ObjectAdded(AObject aParent, AObject aObj) {}

		protected override void ObjectRemoved(AObject aParent, AObject aObj) {}

		protected override void ObjectChanged(AObject aObj) {}

		protected override void ObjectEnabledChanged(AObject aObj) {}

		protected override void FileAdded(AObject aParent, AObject aObj) {}

		protected override void FileRemoved(AObject aParent, AObject aObj) {}

		protected override void FileChanged(AObject aObj) {}

		protected override void SearchAdded(AObject aParent, AObject aObj) {}

		protected override void SearchRemoved(AObject aParent, AObject aObj) {}

		protected override void SearchChanged(AObject aObj) {}

		protected override void SnapshotAdded(Snapshot aSnap) {}

		#endregion

		#region WebSocket

		void OnConnect(UserContext aContext)
		{
		}

		void OnDisconnect(UserContext aContext)
		{
		}

		void OnReceive(UserContext aContext)
		{
			var request = new Request();
#if !UNSAFE
			try
			{
#endif
				// no pass, no way
				byte[] inputBytes = Encoding.UTF8.GetBytes(Salt + Settings.Instance.Password + Salt);
				byte[] hashedBytes = new SHA256Managed().ComputeHash(inputBytes);
				string passwortHash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
				if (request.Password != passwortHash)
				{
					// exit
					return;
				}

				#region DATA HANDLING

				string response = "";

				switch (request.Type)
				{
						# region VERSION

					case Request.Types.Version:
						response = Settings.Instance.XgVersion;
						break;

						#endregion

						# region SERVER

					case Request.Types.AddServer:
						AddServer(request.Name);
						break;

					case Request.Types.RemoveServer:
						RemoveServer(request.Guid);
						break;

						#endregion

						# region CHANNEL

					case Request.Types.AddChannel:
						AddChannel(request.Guid, request.Name);
						break;

					case Request.Types.RemoveChannel:
						RemoveChannel(request.Guid);
						break;

						#endregion

						# region OBJECT

					case Request.Types.ActivateObject:
						ActivateObject(request.Guid);
						break;

					case Request.Types.DeactivateObject:
						DeactivateObject(request.Guid);
						break;

						#endregion

						# region SEARCH

					case Request.Types.SearchPacket:
						response = Objects2Json(Packets(request), request);
						break;

					case Request.Types.SearchBot:
						response = Objects2Json(Bots(request), request);
						break;

					case Request.Types.AddSearch:
						string name = request.Name;
						Core.Object obj = Searches.Named(name);
						if (obj == null)
						{
							obj = new Core.Object {Name = name};
							Searches.Add(obj);
						}
						break;

					case Request.Types.RemoveSearch:
						Searches.Remove(Searches.Named(request.Name));
						break;

					case Request.Types.Searches:
						response = Searches2Json(Searches);
						break;

						#endregion

						# region GET

					case Request.Types.Object:
						response = Object2Json(Servers.WithGuid(request.Guid));
						break;

					case Request.Types.Servers:
						response = Objects2Json(SortedObjects(Servers.All, request), request);
						break;

					case Request.Types.ChannelsFromServer:
						response =
							Objects2Json(
							             SortedObjects(from server in Servers.All from channel in server.Channels where channel.ParentGuid == request.Guid select channel,
							                           request), request);
						break;

					case Request.Types.BotsFromChannel:
						response =
							Objects2Json(
							             SortedBots(
							                        from server in Servers.All
							                        from channel in server.Channels
							                        from bot in channel.Bots
							                        where bot.ParentGuid == request.Guid
							                        select bot, request), request);
						break;

					case Request.Types.PacketsFromBot:
						response = Objects2Json(SortedPackets(from server in Servers.All
						                                      from channel in server.Channels
						                                      from bot in channel.Bots
						                                      from packet in bot.Packets
						                                      where packet.ParentGuid == request.Guid
						                                      select packet, request), request);
						break;

					case Request.Types.Statistics:
						response = Statistic2Json();
						break;

					case Request.Types.GetSnapshots:
						response = Snapshots2Json(Snapshots);
						break;

					case Request.Types.Files:
						response = Objects2Json(SortedFiles(Files.All, request), request);
						break;

						#endregion

						# region COMMANDS

					case Request.Types.CloseServer:
						break;

						#endregion

						# region XDCC Link

					case Request.Types.ParseXdccLink:
						string[] link = request.Name.Substring(7).Split('/');
						string serverName = link[0];
						string channelName = link[2];
						string botName = link[3];
						int packetId = int.Parse(link[4].Substring(1));

						// checking server
						Core.Server serv = Servers.Server(serverName);
						if (serv == null)
						{
							Servers.Add(serverName);
							serv = Servers.Server(serverName);
						}
						serv.Enabled = true;

						// checking channel
						Channel chan = serv.Channel(channelName);
						if (chan == null)
						{
							serv.AddChannel(channelName);
							chan = serv.Channel(channelName);
						}
						chan.Enabled = true;

						// checking bot
						Bot tBot = chan.Bot(botName);
						if (tBot == null)
						{
							tBot = new Bot {Name = botName};
							chan.AddBot(tBot);
						}

						// checking packet
						Packet pack = tBot.Packet(packetId);
						if (pack == null)
						{
							pack = new Packet {Id = packetId, Name = link[5]};
							tBot.AddPacket(pack);
						}
						pack.Enabled = true;
						break;

						#endregion
				}

				aContext.Send(response);

				#endregion
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("OnReceive()", ex);
			}
#endif
		}

		#endregion

		#region JSON

		IEnumerable<Bot> Bots(Request aRequest)
		{
			IEnumerable<Packet> tPacketList = Packets(aRequest);
			IEnumerable<Bot> tBots =
				(from s in Servers.All from c in s.Channels from b in c.Bots join p in tPacketList on b.Guid equals p.Parent.Guid select b).Distinct();

			return SortedBots(tBots, aRequest);
		}

		IEnumerable<Bot> SortedBots(IEnumerable<Bot> aBots, Request aRequest)
		{
			switch (aRequest.SortBy)
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
					aBots = from bot in aBots orderby bot.InfoSpeedCurrent , bot.InfoSpeedMax select bot;
					break;

				case "InfoSlotTotal":
					aBots = from bot in aBots orderby bot.InfoSlotCurrent , bot.InfoSlotTotal select bot;
					break;

				case "InfoQueueTotal":
					aBots = from bot in aBots orderby bot.InfoQueueCurrent , bot.InfoQueueTotal select bot;
					break;
			}

			if (aRequest.SortMode == Request.SortModes.Desc)
			{
				aBots = aBots.Reverse();
			}

			return aBots;
		}

		IEnumerable<Packet> Packets(Request aRequest)
		{
			IEnumerable<Bot> bots = from server in Servers.All from channel in server.Channels from bot in channel.Bots select bot;
			if (aRequest.IgnoreOfflineBots)
			{
				bots = from bot in bots where !bot.Connected select bot;
			}
			IEnumerable<Packet> tPackets = from bot in bots from packet in bot.Packets select packet;

			switch (aRequest.SearchBy)
			{
				case "name":
					string[] searches = aRequest.Search.ToLower().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
					foreach (string currentSearch in searches)
					{
						tPackets = (from packet in tPackets where packet.Name.ToLower().Contains(currentSearch.ToLower()) select packet).ToArray();
					}
					break;

				case "time":
					string[] search = aRequest.Search.Split('-');
					int start = int.Parse(search[0]);
					int stop = int.Parse(search[1]);
					DateTime init = DateTime.MinValue.ToUniversalTime();
					DateTime now = DateTime.Now;

					tPackets = from packet in tPackets
					           where
						           packet.LastUpdated != init && start <= (now - packet.LastUpdated).TotalSeconds && stop >= (now - packet.LastUpdated).TotalSeconds
					           select packet;
					break;

				case "connected":
					tPackets = from packet in tPackets where packet.Connected select packet;
					break;

				case "enabled":
					tPackets = from packet in tPackets where packet.Enabled select packet;
					break;
			}

			return SortedPackets(tPackets, aRequest);
		}

		IEnumerable<Packet> SortedPackets(IEnumerable<Packet> aPackets, Request aRequest)
		{
			switch (aRequest.SortBy)
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

			if (aRequest.SortMode == Request.SortModes.Desc)
			{
				aPackets = aPackets.Reverse();
			}

			return aPackets;
		}

		IEnumerable<File> SortedFiles(IEnumerable<File> aFiles, Request aRequest)
		{
			switch (aRequest.SortBy)
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

			if (aRequest.SortMode == Request.SortModes.Desc)
			{
				aFiles = aFiles.Reverse();
			}

			return aFiles;
		}

		IEnumerable<AObject> SortedObjects(IEnumerable<AObject> aObjects, Request aRequest)
		{
			switch (aRequest.SortBy)
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

			if (aRequest.SortMode == Request.SortModes.Desc)
			{
				aObjects = aObjects.Reverse();
			}

			return aObjects;
		}

		string Searches2Json(Objects aObjects)
		{
			var sb = new StringBuilder();

			sb.Append("{");
			sb.Append("\"Searches\":[");

			int count = 0;
			foreach (var obj in aObjects.All)
			{
				count++;
				sb.Append("{\"Search\": \"" + obj.Name + "\"}");
				if (count < aObjects.All.Count())
				{
					sb.Append(",");
				}
				sb.Append("");
			}

			sb.Append("]}");

			return sb.ToString();
		}

		string Objects2Json(IEnumerable<AObject> aObjects, Request aRequest)
		{
			var tObjects = new JQGrid.Objects {Page = aRequest.Page, Rows = aRequest.Rows};
			tObjects.SetObjects(aObjects);
			return Json.Serialize(tObjects);
		}

		string Object2Json(AObject aObject)
		{
			if (aObject == null)
			{
				return "";
			}

			var tObject = new JQGrid.Object(aObject);
			return Json.Serialize(tObject);
		}

		string Statistic2Json()
		{
			var sb = new StringBuilder();
			sb.Append("{");

			var types = new List<StatisticType>();
			// uncool, but works
			for (int a = (int) StatisticType.BytesLoaded; a <= (int) StatisticType.SpeedMax; a++)
			{
				types.Add((StatisticType) a);
			}

			int count = 0;
			foreach (StatisticType type in types)
			{
				count++;
				double val = Statistic.Instance.Get(type);
				sb.Append("\"" + type + "\":" + val.ToString(CultureInfo.InvariantCulture).Replace(",", "."));
				if (count < types.Count)
				{
					sb.Append(",");
				}
				sb.Append("");
			}

			sb.Append("}");
			return sb.ToString();
		}

		string Snapshots2Json(Snapshots aSnapshots)
		{
			var tObjects = new List<Flot.Object>();
			for (int a = 1; a <= 26; a++)
			{
				var value = (SnapshotValue) a;

				var obj = new Flot.Object();

				var list = new List<Int64[]>();
				foreach (Snapshot snapshot in aSnapshots.All)
				{
					Int64[] data = {snapshot.Get(SnapshotValue.Timestamp) * 1000, snapshot.Get(value)};
					list.Add(data);
				}
				obj.Data = (list.ToArray());
				obj.Label = Enum.GetName(typeof (SnapshotValue), value);

				tObjects.Add(obj);
			}

			return Json.Serialize(tObjects.ToArray());
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

