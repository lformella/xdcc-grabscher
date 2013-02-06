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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

using XG.Core;
using XG.Server.Plugin.General.Webserver.Object;

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
			DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
		};

		readonly List<User> _users = new List<User>();
		
		static readonly Core.Object _search0Day = new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "ODay Packets" };
		static readonly Core.Object _search0Week = new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "OWeek Packets" };
		static readonly Core.Object _searchDownloads = new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Downloads" };
		static readonly Core.Object _searchEnabled = new Core.Object{ Guid = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Enabled Packets" };

		#endregion

		#region AWorker

		protected override void StartRun ()
		{
			_webSocket = new WebSocketServer("ws://localhost:" + (Settings.Instance.WebServerPort + 1));
			//FleckLog.Level = LogLevel.Debug;

			_webSocket.Start (socket =>
			{
				socket.OnOpen = () => OnOpen(socket);
				socket.OnClose = () => OnClose(socket);
				socket.OnMessage = message => OnMessage(socket, message);
				socket.OnError = exception => OnError(socket, exception);
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
			var response = new Response
			{
				Type = Response.Types.ObjectAdded,
				Data = aObj
			};
			Broadcast(response);
		}

		protected override void ObjectRemoved(AObject aParent, AObject aObj)
		{
			var response = new Response
			{
				Type = Response.Types.ObjectRemoved,
				Data = aObj
			};
			Broadcast(response);
		}

		protected override void ObjectChanged(AObject aObj)
		{
			var response = new Response
			{
				Type = Response.Types.ObjectChanged,
				Data = aObj
			};
			Broadcast(response);

			// if a part changed dispatch the file, packet and bot, too
			if (aObj is FilePart)
			{
				var part = aObj as FilePart;
				ObjectChanged(part.Parent);
				if (part.Packet != null)
				{
					ObjectChanged(part.Packet);
					ObjectChanged(part.Packet.Parent);
				}
			}
		}

		protected override void ObjectEnabledChanged(AObject aObj)
		{
			ObjectChanged(aObj);

			// if a packet changed dispatch the bot, too
			if (aObj is Packet)
			{
				var part = aObj as Packet;
				ObjectChanged(part.Parent);
			}
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

		protected override void SnapshotAdded(Snapshot aSnap)
		{
			var response = new Response
			{
				Type = Response.Types.ObjectAdded,
				Data = Snapshots2Flot(Snapshots)
			};
			Broadcast(response);
		}

		#endregion

		#region WebSocket

		void OnOpen(IWebSocketConnection aContext)
		{
			Log.Info("OnOpen(" + aContext.ConnectionInfo.ClientIpAddress + ")");

			var user = new User
			{
				Connection = aContext,
				LoadedObjects = new List<AObject>()
			};

			_users.Add(user);
		}

		void OnClose(IWebSocketConnection aContext)
		{
			Log.Info("OnClose(" + aContext.ConnectionInfo.ClientIpAddress + ")");

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
			Log.Info("OnMessage(" + aContext.ConnectionInfo.ClientIpAddress + ", " + aMessage + ")");

			var currentUser = (from user in _users where user.Connection == aContext select user).SingleOrDefault();
			var request = JsonConvert.DeserializeObject<Request>(aMessage);
#if !UNSAFE
			try
			{
#endif
				// no pass, no way
				if (request.Password != Password)
				{
					Log.Error("OnMessage(" + aContext.ConnectionInfo.ClientIpAddress + ") bad password");
					// exit
					return;
				}

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
						var packets = FilteredPackets(request.Guid);
						UnicastOnRequest(currentUser, packets);

						var bots = DistinctBots(packets);
						UnicastOnRequest(currentUser, bots);
						break;

					case Request.Types.SearchExternal:
						var searchExternal = Searches.WithGuid(request.Guid);
						if (searchExternal != null)
						{
							ExternalSearch[] results = new ExternalSearch[0];
							try
							{
								var uri = new Uri("http://xg.bitpir.at/index.php?show=search&action=external&search=" + searchExternal.Name + "&xg=" + Settings.Instance.XgVersion);
								var req = HttpWebRequest.Create(uri);

								var response = req.GetResponse();
								StreamReader sr = new StreamReader(response.GetResponseStream());
								string text = sr.ReadToEnd();
								response.Close();

								results = JsonConvert.DeserializeObject<ExternalSearch[]>(text, JsonSerializerSettings);
							}
							catch (Exception ex)
							{
								Log.Fatal("OnMessage() cant load external search for " + searchExternal.Name, ex);
							}

							UnicastOnRequest(currentUser, results);
						}
						break;

					case Request.Types.AddSearch:
						string name = request.Name;
						var obj = Searches.Named(name);
						if (obj == null)
						{
							obj = new Core.Object {Name = name};
							Searches.Add(obj);
						}
						break;

					case Request.Types.RemoveSearch:
						var search = Searches.WithGuid(request.Guid);
						if (search != null)
						{
							Searches.Remove(search);
						}
						break;

					case Request.Types.Searches:
						var searches = new List<Core.Object>();
						searches.Add(_search0Day);
						searches.Add(_search0Week);
						searches.Add(_searchDownloads);
						searches.Add(_searchEnabled);
						searches.AddRange(Searches.All);

						Unicast(currentUser, new Response
						{
							Type = Response.Types.Searches,
							Data = searches
						});
						break;

					case Request.Types.Servers:
						UnicastOnRequest(currentUser, Servers.All);
						break;

					case Request.Types.ChannelsFromServer:
						var channels = (from server in Servers.All from channel in server.Channels where channel.ParentGuid == request.Guid select channel).ToList();
						UnicastOnRequest(currentUser, channels);
						break;

					case Request.Types.PacketsFromBot:
						var botPackets = (from server in Servers.All
										from channel in server.Channels
										from bot in channel.Bots
										from packet in bot.Packets
										where packet.ParentGuid == request.Guid
										select packet).ToList();
						UnicastOnRequest(currentUser, botPackets);
						break;

					case Request.Types.Statistics:
						//response = Statistic2Json();
						break;

					case Request.Types.Snapshots:
						Unicast(currentUser, new Response
						{
							Type = Response.Types.Snapshots,
							Data = Snapshots2Flot(Snapshots)
						});
						break;

					case Request.Types.Files:
						UnicastOnRequest(currentUser, Files.All);
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
				Log.Fatal("OnMessage(" + aContext.ConnectionInfo.ClientIpAddress + ", " + aMessage + ")", ex);
			}
#endif
		}

		void OnError(IWebSocketConnection aContext, Exception aException)
		{
			Log.Info("OnError(" + aContext.ConnectionInfo.ClientIpAddress + ")", aException);

			OnClose(aContext);
		}

		void UnicastOnRequest(User aUser, IEnumerable<AObject> aObjects)
		{
			foreach (var obj in aObjects)
			{
				var response = new Response
				{
					Type = Response.Types.ObjectAdded,
					Data = obj
				};
				Unicast(aUser, response);
			}
		}

		void Broadcast(Response aResponse)
		{
			foreach (var user in _users.ToArray())
			{
				Unicast(user, aResponse);
			}
		}

		void Unicast(User aUser, Response aResponse)
		{
			if (aResponse.Data.GetType().IsSubclassOf(typeof(AObject)))
			{
				// lock loaded objcts to prevent sending out the same object more than once
				lock (aUser.LoadedObjects)
				{
					switch (aResponse.Type)
					{
						case Response.Types.ObjectAdded:
							if (aUser.LoadedObjects.Contains(aResponse.Data))
							{
								return;
							}
							aUser.LoadedObjects.Add((AObject)aResponse.Data);
							break;

						case Response.Types.ObjectChanged:
							if (!aUser.LoadedObjects.Contains(aResponse.Data))
							{
								aResponse.Type = Response.Types.ObjectAdded;
							}
							break;

						case Response.Types.ObjectRemoved:
							if (!aUser.LoadedObjects.Contains(aResponse.Data))
							{
								return;
							}
							aUser.LoadedObjects.Remove((AObject)aResponse.Data);
							break;
					}
				}
			}

			string message = JsonConvert.SerializeObject(aResponse, JsonSerializerSettings);
			
#if !UNSAFE
			try
			{
#endif
				aUser.Connection.Send(message);
				Log.Info("Unicast(" + aUser.Connection.ConnectionInfo.ClientIpAddress + ", " + message + ")");
#if !UNSAFE
			}
			catch (Exception ex)
			{
				Log.Fatal("Unicast(" + aUser.Connection.ConnectionInfo.ClientIpAddress + ", " + message + ")", ex);
			}
#endif
		}

		#endregion

		#region Object Searching

		List<Bot> DistinctBots (List<Packet> aPackets)
		{
			var tBots = (from s in Servers.All from c in s.Channels from b in c.Bots join p in aPackets on b.Guid equals p.Parent.Guid select b).Distinct();

			return tBots.ToList();
		}

		List<Packet> FilteredPackets (Guid aGuid)
		{
			var allBots = from server in Servers.All from channel in server.Channels from bot in channel.Bots select bot;
			var allPackets = (from bot in allBots from packet in bot.Packets select packet).ToList();

			DateTime init = DateTime.MinValue.ToUniversalTime();
			DateTime now = DateTime.Now;

			if (aGuid == _search0Day.Guid)
			{
				allPackets = (from packet in allPackets
							where packet.LastUpdated != init && 0 <= (now - packet.LastUpdated).TotalSeconds && 86400 >= (now - packet.LastUpdated).TotalSeconds
							select packet).ToList();
			}
			else if (aGuid == _search0Week.Guid)
			{
				allPackets = (from packet in allPackets
							where packet.LastUpdated != init && 0 <= (now - packet.LastUpdated).TotalSeconds && 604800 >= (now - packet.LastUpdated).TotalSeconds
							select packet).ToList();
			}
			else if (aGuid == _searchDownloads.Guid)
			{
				allPackets = (from packet in allPackets where packet.Connected select packet).ToList();
			}
			else if (aGuid == _searchEnabled.Guid)
			{
				allPackets = (from packet in allPackets where packet.Enabled select packet).ToList();
			}
			else 
			{
				var search = Searches.WithGuid(aGuid);
				if (search != null)
				{
					string[] searches = search.Name.ToLower().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
					foreach (string currentSearch in searches)
					{
						allPackets = (from packet in allPackets where packet.Name.ToLower().Contains(currentSearch.ToLower()) select packet).ToList();
					}
				}
				else
				{
					allPackets.Clear();
				}
			}

			return allPackets;
		}

		Flot[] Snapshots2Flot(Snapshots aSnapshots)
		{
			var tObjects = new List<Flot>();
			for (int a = 1; a <= 26; a++)
			{
				var value = (SnapshotValue) a;

				var obj = new Flot();

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

		public Int64[][] FilterDuplicateEntries(Int64[][] aEntries)
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

