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

using Fleck;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using XG.Core;
using XG.Server.Plugin.General.Webserver.Websocket.Response;

using log4net;

namespace XG.Server.Plugin.General.Webserver.Websocket
{
	public class Server : SaltedPassword
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		WebSocketServer _webSocket;
		static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
		{
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
		};

		readonly List<User> _users = new List<User>();

		#endregion

		#region AWorker

		protected override void StartRun ()
		{
			_webSocket = new WebSocketServer("ws://localhost:" + (Settings.Instance.WebServerPort + 1));

			_webSocket.Start (socket =>
			{
				socket.OnOpen = () => OnOpen(socket);
				socket.OnClose = () => OnClose(socket);
				socket.OnMessage = message => OnMessage(socket, message);
			});
		}

		protected override void StopRun()
		{
			//_webSocket.Stop();
		}

		#endregion

		#region REPOSITORY EVENTS

		protected override void ObjectAdded(AObject aParent, AObject aObj)
		{
			var response = new Response.Object
			{
				Type = Base.Types.ObjectAdded,
				Data = aObj,
				DataType = aObj.GetType()
			};
			Broadcast(response);
		}

		protected override void ObjectRemoved(AObject aParent, AObject aObj)
		{
			var response = new Response.Object
			{
				Type = Base.Types.ObjectRemoved,
				Data = aObj,
				DataType = aObj.GetType()
			};
			Broadcast(response);
		}

		protected override void ObjectChanged(AObject aObj)
		{
			var response = new Response.Object
			{
				Type = Base.Types.ObjectChanged,
				Data = aObj,
				DataType = aObj.GetType()
			};
			Broadcast(response);
		}

		protected override void ObjectEnabledChanged(AObject aObj)
		{
			ObjectChanged(aObj);
		}

		protected override void FileAdded(AObject aParent, AObject aObj)
		{
			ObjectAdded(aParent, aObj);
		}

		protected override void FileRemoved(AObject aParent, AObject aObj)
		{
			ObjectRemoved(aParent, aObj);
		}

		protected override void FileChanged(AObject aObj)
		{
			ObjectChanged(aObj);
		}

		protected override void SearchAdded(AObject aParent, AObject aObj)
		{
			ObjectAdded(aParent, aObj);
		}

		protected override void SearchRemoved(AObject aParent, AObject aObj)
		{
			ObjectRemoved(aParent, aObj);
		}

		protected override void SearchChanged(AObject aObj)
		{
			ObjectChanged(aObj);
		}

		protected override void SnapshotAdded(Core.Snapshot aSnap)
		{
			var response = new Response.Snapshots
			{
				Type = Base.Types.Snapshots,
				Data = Snapshots2Flot(Snapshots),
				DataType = aSnap.GetType()
			};
			Broadcast(response);
		}

		#endregion

		#region WebSocket

		void OnOpen(IWebSocketConnection aContext)
		{
			var user = new User
			{
				Connection = aContext,
				LastSearch = Guid.Empty
			};

			_users.Add(user);
		}

		void OnClose(IWebSocketConnection aContext)
		{
			foreach (var user in _users.ToArray())
			{
				if (user.Connection == aContext)
				{
					_users.Remove(user);
				}
			}
		}

		void OnMessage(IWebSocketConnection aContext, string aMessage)
		{
			var currentUser = (from user in _users where user.Connection == aContext select user).SingleOrDefault();
			var request = JsonConvert.DeserializeObject<Request>(aMessage);
#if !UNSAFE
			try
			{
#endif
				// no pass, no way
				if (request.Password != Password)
				{
					// exit
					return;
				}

				currentUser.LastRequest = request.Type;

				switch (request.Type)
				{
					case Request.Types.AddServer:
						AddServer(request.Name);
						break;

					case Request.Types.RemoveServer:
						RemoveServer(request.Guid);
						break;

					case Request.Types.AddChannel:
						AddChannel(request.Guid, request.Name);
						break;

					case Request.Types.RemoveChannel:
						RemoveChannel(request.Guid);
						break;

					case Request.Types.ActivateObject:
						ActivateObject(request.Guid);
						break;

					case Request.Types.DeactivateObject:
						DeactivateObject(request.Guid);
						break;

					case Request.Types.Search:
						currentUser.LastSearch = request.Guid;
						currentUser.IgnoreOfflineBots = request.IgnoreOfflineBots;

						IEnumerable<Packet> packets = FilteredPackets(AllPackets(request.IgnoreOfflineBots), request.Guid);
						Unicast(aContext, new Response.Objects
						{
							Type = Base.Types.SearchPacket,
							Data = packets
						});

						IEnumerable<Bot> bots = FilteredBots(packets);
						Unicast(aContext, new Response.Objects
						{
							Type = Base.Types.SearchBot,
							Data = bots
						});
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
						Searches.Remove(Searches.WithGuid(request.Guid));
						break;

					case Request.Types.Searches:
						var searches = new List<Core.Object>();
						searches.Add(new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "ODay Packets" });
						searches.Add(new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "OWeek Packets" });
						searches.Add(new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Downloads" });
						searches.Add(new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Enabled Packets" });
						searches.AddRange(Searches.All);

						Unicast(aContext, new Response.Objects
						{
							Type = Base.Types.Searches,
							Data = searches
						});
						break;

					case Request.Types.Servers:
						Unicast(aContext, new Response.Objects
						{
							Type = Base.Types.Servers,
							Data = Servers.All
						});
						break;

					case Request.Types.ChannelsFromServer:
						Unicast(aContext, new Response.Objects
						{
							Type = Base.Types.ChannelsFromServer,
							Data = from server in Servers.All from channel in server.Channels where channel.ParentGuid == request.Guid select channel
						});
						break;

					case Request.Types.PacketsFromBot:
						currentUser.LastSearch = request.Guid;

						Unicast(aContext, new Response.Objects
						{
							Type = Base.Types.PacketsFromBot,
							Data = from server in Servers.All
									from channel in server.Channels
									from bot in channel.Bots
									from packet in bot.Packets
									where packet.ParentGuid == request.Guid
									select packet
						});
						break;

					case Request.Types.Statistics:
						//response = Statistic2Json();
						break;

					case Request.Types.Snapshots:
						Unicast(aContext, new Response.Snapshots
						{
							Type = Base.Types.Snapshots,
							Data = Snapshots2Flot(Snapshots)
						});
						break;

					case Request.Types.Files:
						Unicast(aContext, new Response.Objects
						{
							Type = Base.Types.Files,
							Data = Files.All
						});
						break;

					case Request.Types.CloseServer:
						break;

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
				}
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("OnReceive()", ex);
			}
#endif
		}

		void Broadcast(Base aResponse)
		{
			foreach (var user in _users.ToArray())
			{
				if (user.LastSearch != Guid.Empty && aResponse is Response.Object)
				{
					var tResponse = aResponse as Response.Object;

					if (aResponse.DataType == typeof(Bot))
					{
						var bots = FilteredBots(FilteredPackets(AllPackets(user.IgnoreOfflineBots), user.LastSearch));
						if (!bots.Contains(tResponse.Data))
						{
							break;
						}
					}

					if (aResponse.DataType == typeof(Packet))
					{
						if (user.LastRequest == Request.Types.PacketsFromBot)
						{
							if (tResponse.Data.ParentGuid != user.LastSearch)
							{
								break;
							}
						}
						else
						{
							var packets = FilteredPackets(AllPackets(user.IgnoreOfflineBots), user.LastSearch);
							if (!packets.Contains(tResponse.Data))
							{
								break;
							}
						}
					}
				}

				Unicast(user.Connection, aResponse);
			}
		}

		void Unicast(IWebSocketConnection aConnection, Base aResponse)
		{
			string message = JsonConvert.SerializeObject(aResponse, JsonSerializerSettings);

			try
			{
				aConnection.Send(message);
			}
			catch (Exception ex)
			{
				Log.Fatal("Unicast()", ex);
			}
		}

		#endregion

		#region Object Searching

		IEnumerable<Bot> FilteredBots(IEnumerable<Packet> aPackets)
		{
			IEnumerable<Bot> tBots = (from s in Servers.All from c in s.Channels from b in c.Bots join p in aPackets on b.Guid equals p.Parent.Guid select b).Distinct();

			return tBots;
		}

		IEnumerable<Packet> AllPackets (bool ignoreOfflineBots)
		{
			IEnumerable<Bot> allBots = from server in Servers.All from channel in server.Channels from bot in channel.Bots select bot;
			if (ignoreOfflineBots)
			{
				allBots = from bot in allBots where !bot.Connected select bot;
			}
			IEnumerable<Packet> allPackets = from bot in allBots from packet in bot.Packets select packet;

			return allPackets;
		}

		IEnumerable<Packet> FilteredPackets (IEnumerable<Packet> aPackets, Guid aGuid)
		{
			DateTime init = DateTime.MinValue.ToUniversalTime();
			DateTime now = DateTime.Now;

			if (aGuid.ToString() == "00000000-0000-0000-0000-000000000001")
			{
				aPackets = from packet in aPackets
							where packet.LastUpdated != init && 0 <= (now - packet.LastUpdated).TotalSeconds && 86400 >= (now - packet.LastUpdated).TotalSeconds
							select packet;
			}
			else if (aGuid.ToString() == "00000000-0000-0000-0000-000000000002")
			{
				aPackets = from packet in aPackets
							where packet.LastUpdated != init && 0 <= (now - packet.LastUpdated).TotalSeconds && 604800 >= (now - packet.LastUpdated).TotalSeconds
							select packet;
			}
			else if (aGuid.ToString() == "00000000-0000-0000-0000-000000000003")
			{
				aPackets = from packet in aPackets where packet.Connected select packet;
			}
			else if (aGuid.ToString() == "00000000-0000-0000-0000-000000000004")
			{
				aPackets = from packet in aPackets where packet.Enabled select packet;
			}
			else 
			{
				var search = Searches.WithGuid(aGuid);
				if (search != null)
				{
					string[] searches = search.Name.ToLower().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
					foreach (string currentSearch in searches)
					{
						aPackets = (from packet in aPackets where packet.Name.ToLower().Contains(currentSearch.ToLower()) select packet).ToArray();
					}
				}
			}

			return aPackets;
		}

		Flot.Object[] Snapshots2Flot(Core.Snapshots aSnapshots)
		{
			var tObjects = new List<Flot.Object>();
			for (int a = 1; a <= 26; a++)
			{
				var value = (SnapshotValue) a;

				var obj = new Flot.Object();

				var list = new List<Int64[]>();
				foreach (Core.Snapshot snapshot in aSnapshots.All)
				{
					Int64[] data = {snapshot.Get(SnapshotValue.Timestamp) * 1000, snapshot.Get(value)};
					list.Add(data);
				}
				obj.Data = (list.ToArray());
				obj.Label = Enum.GetName(typeof (SnapshotValue), value);

				tObjects.Add(obj);
			}

			return tObjects.ToArray();
		}

		#endregion
	}
}

