//
//  Server.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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
using System.Linq;
using System.Net;
using System.Reflection;
using Fleck;
using log4net;
using Newtonsoft.Json;
using SharpRobin.Core;
using XG.Model;
using XG.Model.Domain;
using XG.Plugin.Webserver.Object;
using XG.Config.Properties;

namespace XG.Plugin.Webserver.Websocket
{
	public class Server : ASaltedPassword
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		WebSocketServer _webSocket;
		JsonSerializerSettings _jsonSerializerSettings;

		readonly HashSet<User> _users = new HashSet<User>();

		static readonly Model.Domain.Search _searchEnabled = new Model.Domain.Search { Guid = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Enabled Packets" };
		static readonly Model.Domain.Search _searchDownloads = new Model.Domain.Search { Guid = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Downloads" };

		public RrdDb RrdDB { get; set; }

		#endregion

		public Server()
		{
			_jsonSerializerSettings = new JsonSerializerSettings
			{
				DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
				DateParseHandling = DateParseHandling.DateTime,
				DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
			};
			_jsonSerializerSettings.Converters.Add(new DoubleConverter());
		}

		#region AWorker

		protected override void StartRun ()
		{
			_webSocket = new WebSocketServer("ws://localhost:" + (Settings.Default.WebserverPort + 1));
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

		protected override void ObjectAdded(object aSender, EventArgs<Model.Domain.AObject, Model.Domain.AObject> aEventArgs)
		{
			Broadcast(Response.Types.ObjectAdded, aEventArgs.Value2, true);
		}

		protected override void ObjectRemoved(object aSender, EventArgs<Model.Domain.AObject, Model.Domain.AObject> aEventArgs)
		{
			Broadcast(Response.Types.ObjectRemoved, aEventArgs.Value2, true);
		}

		protected override void ObjectChanged(object aSender, EventArgs<Model.Domain.AObject, string[]> aEventArgs)
		{
			Broadcast(Response.Types.ObjectChanged, aEventArgs.Value1, true);

			HashSet<string> fields = new HashSet<string>(aEventArgs.Value2);

			// if a bot changed dispatch the packets, too
			if (aEventArgs.Value1 is Model.Domain.Bot)
			{
				if (fields.Contains("Connected"))
				{
					foreach (var pack in (aEventArgs.Value1 as Model.Domain.Bot).Packets)
					{
						Broadcast(Response.Types.ObjectChanged, pack, true);
					}
				}
			}
			// if a part changed dispatch the file, packet and bot, too
			else if (aEventArgs.Value1 is FilePart)
			{
				var part = aEventArgs.Value1 as FilePart;
				Broadcast(Response.Types.ObjectChanged, part.Parent, false);

				if (part.Packet != null)
				{
					if (fields.Contains("Speed") || fields.Contains("CurrentSize") || fields.Contains("TimeMissing"))
					{
						Broadcast(Response.Types.ObjectChanged, part.Packet, false);
					}
					if (fields.Contains("Speed"))
					{
						Broadcast(Response.Types.ObjectChanged, part.Packet.Parent, false);
					}
				}
			}
		}

		protected override void ObjectEnabledChanged(object aSender, EventArgs<Model.Domain.AObject> aEventArgs)
		{
			Broadcast(Response.Types.ObjectChanged, aEventArgs.Value1, false);

			// if a packet changed dispatch the bot, too
			if (aEventArgs.Value1 is Model.Domain.Packet)
			{
				var part = aEventArgs.Value1 as Model.Domain.Packet;
				Broadcast(Response.Types.ObjectChanged, part.Parent, false);
			}
		}

		protected override void FileAdded(object aSender, EventArgs<Model.Domain.AObject, Model.Domain.AObject> aEventArgs)
		{
			ObjectAdded(aSender, aEventArgs);
		}

		protected override void FileRemoved(object aSender, EventArgs<Model.Domain.AObject, Model.Domain.AObject> aEventArgs)
		{
			ObjectRemoved(aSender, aEventArgs);
		}

		protected override void FileChanged(object aSender, EventArgs<Model.Domain.AObject, string[]> aEventArgs)
		{
			ObjectChanged(aSender, aEventArgs);
		}

		protected override void SearchAdded(object aSender, EventArgs<Model.Domain.AObject, Model.Domain.AObject> aEventArgs)
		{
			ObjectAdded(aSender, aEventArgs);
		}

		protected override void SearchRemoved(object aSender, EventArgs<Model.Domain.AObject, Model.Domain.AObject> aEventArgs)
		{
			ObjectRemoved(aSender, aEventArgs);
		}

		protected override void SearchChanged(object aSender, EventArgs<Model.Domain.AObject, string[]> aEventArgs)
		{
			ObjectChanged(aSender, aEventArgs);
		}

		protected override void NotificationAdded(object aSender, EventArgs<Model.Domain.Notification> aEventArgs)
		{
			ObjectAdded(aSender, new EventArgs<Model.Domain.AObject, Model.Domain.AObject>(null, aEventArgs.Value1));
		}

		#endregion

		#region WebSocket

		void OnOpen(IWebSocketConnection aContext)
		{
			Log.Info("OnOpen(" + aContext.ConnectionInfo.ClientIpAddress + ")");

			var user = new User
			{
				Connection = aContext,
				LoadedObjects = new HashSet<Guid>(),
				LastSearchRequest = null
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
			try
			{
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
						OnAddServer(request.Name);
						break;

					case Request.Types.RemoveServer:
						OnRemoveServer(request.Guid);
						break;

					case Request.Types.AddChannel:
						OnAddChannel(request.Guid, request.Name);
						break;

					case Request.Types.RemoveChannel:
						OnRemoveChannel(request.Guid);
						break;

					case Request.Types.ActivateObject:
						OnActivateObject(request.Guid);
						break;

					case Request.Types.DeactivateObject:
						OnDeactivateObject(request.Guid);
						break;

					case Request.Types.Search:
					case Request.Types.PacketsFromBot:
						OnSearch(currentUser, request);
						break;

					case Request.Types.SearchExternal:
						OnSearchExternal(currentUser, request);
						break;

					case Request.Types.AddSearch:
						OnAddSearch(request.Name);
						break;

					case Request.Types.RemoveSearch:
						OnRemoveSearch(request.Guid);
						break;

					case Request.Types.Searches:
						UnicastAdded(currentUser, Searches.All);
						break;

					case Request.Types.Servers:
						UnicastAdded(currentUser, Servers.All);
						break;

					case Request.Types.ChannelsFromServer:
						OnChannelsFromServer(currentUser, request);
						break;

					case Request.Types.LiveSnapshot:
						OnLiveSnapshot(currentUser);
						break;

					case Request.Types.Snapshots:
						OnSnapshots(currentUser, request);
						break;

					case Request.Types.Files:
						UnicastAdded(currentUser, Files.All);
						break;

					case Request.Types.CloseServer:
						break;

					case Request.Types.ParseXdccLink:
						OnParseXdccLink(request.Name);
						break;
				}
			}
			catch (Exception ex)
			{
				Log.Fatal("OnMessage(" + aContext.ConnectionInfo.ClientIpAddress + ", " + aMessage + ")", ex);
			}
		}

		void OnError(IWebSocketConnection aContext, Exception aException)
		{
			Log.Info("OnError(" + aContext.ConnectionInfo.ClientIpAddress + ")", aException);

			OnClose(aContext);
		}

		#endregion

		#region Websocket Write

		void Broadcast(Response.Types aType, Model.Domain.AObject aObject, bool aOnlySendVisibleObjects)
		{
			var response = new Response
			{
				Type = aType,
				Data = aObject
			};

			foreach (var user in _users.ToArray())
			{
				Unicast(user, response, aOnlySendVisibleObjects);
			}
		}

		void UnicastAdded(User aUser, IEnumerable<object> aObjects)
		{
			foreach (var obj in aObjects)
			{
				var response = new Response
				{
					Type = Response.Types.ObjectAdded,
					Data = obj
				};
				Unicast(aUser, response, false);
			}
		}

		void Unicast(User aUser, Response aResponse, bool aOnlySendVisibleObjects)
		{
			if (!ShouldResponseSended(aUser, aResponse, aOnlySendVisibleObjects))
			{
				return;
			}

			Object.AObject myObj = null;

			if (aResponse.Data is Model.Domain.Server)
			{
				myObj = new Object.Server { Object = aResponse.Data as Model.Domain.Server };
			}
			if (aResponse.Data is Model.Domain.Channel)
			{
				myObj = new Object.Channel { Object = aResponse.Data as Model.Domain.Channel };
			}
			if (aResponse.Data is Model.Domain.Bot)
			{
				myObj = new Object.Bot { Object = aResponse.Data as Model.Domain.Bot };
			}
			if (aResponse.Data is Model.Domain.Packet)
			{
				myObj = new Object.Packet { Object = aResponse.Data as Model.Domain.Packet };
			}
			if (aResponse.Data is Model.Domain.Search)
			{
				var search = aResponse.Data as Model.Domain.Search;
				var request = new Request {
					Type = Request.Types.Search,
					Guid = search.Guid,
					Name = search.Name
				};
				var results = from server in Servers.All from channel in server.Channels from bot in channel.Bots from packet in bot.Packets where IsVisible(packet, request) select packet;
				myObj = new Object.Search
				{
					Object = aResponse.Data as Model.Domain.Search,
					ResultsOnline = (from obj in results where obj is Model.Domain.Packet && obj.Parent.Connected select obj).Count(),
					ResultsOffline = (from obj in results where obj is Model.Domain.Packet && !obj.Parent.Connected select obj).Count()
				};
			}
			if (aResponse.Data is Model.Domain.Notification)
			{
				myObj = new Object.Notification { Object = aResponse.Data as Model.Domain.Notification };
			}
			if (aResponse.Data is Model.Domain.File)
			{
				myObj = new Object.File { Object = aResponse.Data as Model.Domain.File };
			}
			if (aResponse.Data is FilePart)
			{
				return;
			}

			if (myObj != null)
			{
				aResponse.Data = myObj;
			}

			string message = null;
			try
			{
				message = JsonConvert.SerializeObject(aResponse, _jsonSerializerSettings);
			}
			catch (Exception ex)
			{
				Log.Fatal("Unicast(" + aUser.Connection.ConnectionInfo.ClientIpAddress + ", " + aResponse.Type + "|" + aResponse.DataType + ")", ex);
			}

			if (message != null)
			{
				try
				{
					aUser.Connection.Send(message);
					Log.Info("Unicast(" + aUser.Connection.ConnectionInfo.ClientIpAddress + ", " + message + ")");
				}
				catch (Exception ex)
				{
					Log.Fatal("Unicast(" + aUser.Connection.ConnectionInfo.ClientIpAddress + ", " + message + ")", ex);
				}
			}
		}

		#endregion

		#region Functions

		bool ShouldResponseSended(User aUser, Response aResponse, bool aOnlySendVisibleObjects)
		{
			if (aOnlySendVisibleObjects)
			{
				if (aResponse.Data is Model.Domain.Bot || aResponse.Data is Model.Domain.Packet)
				{
					switch (aResponse.Type)
					{
						case Response.Types.ObjectAdded:
						case Response.Types.ObjectChanged:
							var bot = aResponse.Data as Model.Domain.Bot;
							if (bot != null && !IsVisible(bot, aUser.LastSearchRequest))
							{
								return false;
							}
							var packet = aResponse.Data as Model.Domain.Packet;
							if (packet != null && !IsVisible(packet, aUser.LastSearchRequest))
							{
								return false;
							}
							break;
					}
				}
			}

			if (aResponse.Data.GetType().IsSubclassOf(typeof(Model.Domain.AObject)))
			{
				Model.Domain.AObject data = (Model.Domain.AObject)aResponse.Data;
				// lock loaded objects to prevent sending out the same object more than once
				lock (aUser.LoadedObjects)
				{
					switch (aResponse.Type)
					{
						case Response.Types.ObjectAdded:
							if (aUser.LoadedObjects.Contains(data.Guid))
							{
								return false;
							}
							aUser.LoadedObjects.Add(data.Guid);
							break;

						case Response.Types.ObjectChanged:
							if (!aUser.LoadedObjects.Contains(data.Guid))
							{
								return false;
							}
							break;

						case Response.Types.ObjectRemoved:
							if (!aUser.LoadedObjects.Contains(data.Guid))
							{
								return false;
							}
							aUser.LoadedObjects.Remove(data.Guid);
							break;
					}
				}
			}

			return true;
		}

		void OnLiveSnapshot(User currentUser)
		{
			Unicast(currentUser, new Response
			{
				Type = Response.Types.LiveSnapshot,
				Data = GetFlotSnapshot()
			}, false);
		}

		void OnSnapshots(User currentUser, Request request)
		{
			var startTime = DateTime.Now.AddDays(int.Parse(request.Name));
			var data = GetFlotData(startTime, DateTime.Now);

			Unicast(currentUser, new Response
			{
				Type = Response.Types.Snapshots,
				Data = data
			}, false);
		}

		void OnChannelsFromServer(User currentUser, Request request)
		{
			var channels = (from server in Servers.All from channel in server.Channels where channel.ParentGuid == request.Guid select channel).ToList();
			UnicastAdded(currentUser, channels);
			var tServer = Servers.WithGuid(request.Guid);
			if (tServer != null)
			{
				Unicast(currentUser, new Response
				{
					Type = Response.Types.ObjectChanged,
					Data = tServer
				}, false);
			}
		}

		void OnAddSearch(string aSearch)
		{
			var obj = Searches.Named(aSearch);
			if (obj == null)
			{
				obj = new Model.Domain.Search { Name = aSearch };
				Searches.Add(obj);
			}
		}

		void OnRemoveSearch(Guid aGuid)
		{
			var search = Searches.WithGuid(aGuid);
			if (search != null)
			{
				Searches.Remove(search);
			}
		}

		void OnSearchExternal(User currentUser, Request request)
		{
			var searchExternal = Searches.WithGuid(request.Guid);
			if (searchExternal != null)
			{
				request.Name = searchExternal.Name;
			}

			var results = SearchExternal(request.Name);
			foreach (var result in results)
			{
				var currentResponse = new Response
				{
					Type = Response.Types.ObjectAdded,
					Data = result
				};
				Unicast(currentUser, currentResponse, false);
			}

			Unicast(currentUser, new Response
			{
				Type = Response.Types.SearchComplete,
				Data = request.Type
			}, false);
		}

		void OnAddServer(String aName)
		{
			string serverString = aName;
			int port = 6667;
			if (serverString.Contains(":"))
			{
				string[] serverArray = serverString.Split(':');
				serverString = serverArray[0];
				port = int.Parse(serverArray[1]);
			}

			Servers.Add(serverString, port);
		}

		public void OnRemoveServer(Guid aGuid)
		{
			Model.Domain.AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				Servers.Remove(tObj as Model.Domain.Server);
			}
		}

		public void OnAddChannel(Guid aGuid, string aString)
		{
			var tServ = Servers.WithGuid(aGuid) as Model.Domain.Server;
			if (tServ != null)
			{
				tServ.AddChannel(aString);
			}
		}

		public void OnRemoveChannel(Guid aGuid)
		{
			var tChan = Servers.WithGuid(aGuid) as Model.Domain.Channel;
			if (tChan != null)
			{
				tChan.Parent.RemoveChannel(tChan);
			}
		}

		public void OnActivateObject(Guid aGuid)
		{
			Model.Domain.AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = true;
			}
		}

		public void OnDeactivateObject(Guid aGuid)
		{
			Model.Domain.AObject tObj = Servers.WithGuid(aGuid);
			if (tObj != null)
			{
				tObj.Enabled = false;
			}
			else
			{
				var file = Files.WithGuid(aGuid) as Model.Domain.File;
				if (file != null)
				{
					Files.Remove(file);
				}
			}
		}

		void OnSearch(User currentUser, Request request)
		{
			currentUser.LastSearchRequest = request;

			var allPackets = from server in Servers.All from channel in server.Channels from bot in channel.Bots from packet in bot.Packets where IsVisible(packet, request) select packet;
			var all = new List<Model.Domain.AObject>();
			all.AddRange(allPackets);
			all.AddRange(from packet in allPackets select packet.Parent);

			UnicastAdded(currentUser, all);

			Model.Domain.AObject update = null;
			if (request.Type == Request.Types.Search)
			{
				Unicast(currentUser, new Response
				{
					Type = Response.Types.SearchComplete,
					Data = request.Type
				}, false);
				// send search again to update search results
				update = Searches.WithGuid(request.Guid);
			}
			else if (request.Type == Request.Types.PacketsFromBot)
			{
				update = Servers.WithGuid(request.Guid) as Model.Domain.Bot;
			}

			if (update != null)
			{
				Unicast(currentUser, new Response
				{
					Type = Response.Types.ObjectChanged,
					Data = update
				}, false);
			}
		}

		void OnParseXdccLink(String aLink)
		{
			string[] link = aLink.Substring(7).Split('/');
			string serverName = link[0];
			string channelName = link[2];
			string botName = link[3];
			int packetId = int.Parse(link[4].Substring(1));

			// checking server
			Model.Domain.Server serv = Servers.Server(serverName);
			if (serv == null)
			{
				Servers.Add(serverName);
				serv = Servers.Server(serverName);
			}
			serv.Enabled = true;

			// checking channel
			Model.Domain.Channel chan = serv.Channel(channelName);
			if (chan == null)
			{
				serv.AddChannel(channelName);
				chan = serv.Channel(channelName);
			}
			chan.Enabled = true;

			// checking bot
			Model.Domain.Bot tBot = chan.Bot(botName);
			if (tBot == null)
			{
				tBot = new Model.Domain.Bot { Name = botName };
				chan.AddBot(tBot);
			}

			// checking packet
			Model.Domain.Packet pack = tBot.Packet(packetId);
			if (pack == null)
			{
				pack = new Model.Domain.Packet { Id = packetId, Name = link[5] };
				tBot.AddPacket(pack);
			}
			pack.Enabled = true;
		}

		bool IsVisible(Model.Domain.Bot aBot, Request aRequest)
		{
			if (aRequest == null)
			{
				return false;
			}

			foreach (var packet in aBot.Packets)
			{
				if (IsVisible(packet, aRequest))
				{
					return true;
				}
			}

			return false;
		}

		bool IsVisible(Model.Domain.Packet aPacket, Request aRequest)
		{
			if (aRequest == null)
			{
				return false;
			}

			if (aRequest.Type == Request.Types.Search)
			{
				if (aRequest.Guid == Server._searchDownloads.Guid)
				{
					return aPacket.Connected;
				}

				if (aRequest.Guid == Server._searchEnabled.Guid)
				{
					return aPacket.Enabled;
				}

				var str = aRequest.Name;

				var search = Searches.WithGuid(aRequest.Guid);
				if (search != null)
				{
					str = search.Name;
				}

				return aPacket.Name.ContainsAll(str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
			}

			if (aRequest.Type == Request.Types.PacketsFromBot)
			{
				return aPacket.ParentGuid == aRequest.Guid;
			}

			return false;
		}

		IEnumerable<Flot> GetFlotSnapshot ()
		{
			var tObjects = new List<Flot>();

			var snapshot = CollectSnapshot();
			for (int a = 1; a <= Snapshot.SnapshotCount; a++)
			{
				var value = (SnapshotValue) a;
				var obj = new Flot();
				obj.Data = new double[][]{ new double[]{snapshot.Get(SnapshotValue.Timestamp), snapshot.Get(value)}};
				obj.Label = Enum.GetName(typeof (SnapshotValue), value);

				tObjects.Add(obj);
			}

			return tObjects.ToArray();
		}

		IEnumerable<ExternalSearch> SearchExternal (string search)
		{
			var objects = new List<ExternalSearch>();

			int start = 0;
			int limit = 25;
			do
			{
				try
				{
					var uri = new Uri("http://xg.bitpir.at/index.php?show=search&action=external&xg=" + Settings.Default.XgVersion + "&start=" + start + "&limit=" + limit + "&search=" + search);
					var req = HttpWebRequest.Create(uri);

					var response = req.GetResponse();
					StreamReader sr = new StreamReader(response.GetResponseStream());
					string text = sr.ReadToEnd();
					response.Close();

					ExternalSearch[] results = JsonConvert.DeserializeObject<ExternalSearch[]>(text, _jsonSerializerSettings);

					if (results.Length > 0)
					{
						objects.AddRange(results);
					}

					if (results.Length == 0 || results.Length < limit)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					Log.Fatal("OnSearchExternal(" + search + ") cant load external search", ex);
					break;
				}
				start += limit;
			} while (true);

			return objects;
		}

		IEnumerable<Flot> GetFlotData(DateTime aStart, DateTime aEnd)
		{
			var tObjects = new List<Flot>();

			FetchData data = RrdDB.createFetchRequest(ConsolFuns.CF_AVERAGE, aStart.ToTimestamp(), aEnd.ToTimestamp(), 1).fetchData();
			Int64[] times = data.getTimestamps();
			double[][] values = data.getValues();

			for (int a = 1; a <= Snapshot.SnapshotCount; a++)
			{
				var value = (SnapshotValue) a;
				var obj = new Flot();

				var list = new List<double[]>();
				for (int b = 0; b < times.Length; b++)
				{
					double[] current = { times[b] * 1000, values[a][b] };
					list.Add(current);
				}
				obj.Data = list.ToArray();
				obj.Label = Enum.GetName(typeof (SnapshotValue), value);

				tObjects.Add(obj);
			}

			return tObjects.ToArray();
		}

		#endregion
	}
}
