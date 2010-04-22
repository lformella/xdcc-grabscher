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
using System.Text.RegularExpressions;
using System.Threading;
using XG.Core;

namespace XG.Server
{
	/// <summary>
	/// This class describes the connection to a single irc server
	/// it does the following things
	/// - parsing all messages comming from the server, channel and bot
	/// - creating and removing bots on the fly
	/// - creating and removing packets on the fly (if the bot posts them into the channel)
	/// - communicate with the bot to handle downloads
	/// </summary>	
	public class ServerConnect
	{
		private ServerHandler myParent;

		private XGServer myServer;
		private Connection myCon;

		private bool myIsRunning = false;
		public bool IsRunning { get { return this.myIsRunning; } }

		private Dictionary<XGObject, DateTime> myTimedObjects;
		private Dictionary<string, DateTime> myLatestPacketRequests;

		#region EVENTS

		public event DownloadDelegate NewDownloadEvent;
		public event BotDelegate KillDownloadEvent;
		public event ServerDelegate ConnectedEvent;
		public event ServerDelegate DisconnectedEvent;
		public event DataTextDelegate ParsingErrorEvent;

		public event ObjectDelegate ObjectChangedEvent;
		public event ObjectObjectDelegate ObjectAddedEvent;
		public event ObjectObjectDelegate ObjectRemovedEvent;

		#endregion

		#region INIT

		public ServerConnect(ServerHandler aParent)
		{
			this.myParent = aParent;
		}

		#endregion

		#region CONNECTION

		public void Connect(XGServer aServer)
		{
			this.myServer = aServer;
			this.myServer.ChannelAddedEvent += new ServerChannelDelegate(server_ChannelAddedEventHandler);
			this.myServer.ChannelRemovedEvent += new ServerChannelDelegate(server_ChannelRemovedEventHandler);

			this.myCon = new Connection();
			this.myCon.ConnectedEvent += new EmptyDelegate(con_ConnectedEventHandler);
			this.myCon.DisconnectedEvent += new EmptyDelegate(con_DisconnectedEventHandler);
			this.myCon.DataTextReceivedEvent += new DataTextDelegate(con_DataReceivedEventHandler);

			this.myCon.Connect(this.myServer.Name, this.myServer.Port);
		}
		private void con_ConnectedEventHandler()
		{
			this.myCon.SendData("NICK " + Settings.Instance.IRCName);
			this.myCon.SendData("USER " + Settings.Instance.IRCName + " " + Settings.Instance.IRCName + " " + this.myServer.Name + " :root");

			foreach (XGChannel tChan in this.myServer.Children)
			{
				tChan.EnabledChangedEvent += new ObjectDelegate(channel_ObjectStateChangedEventHandler);
				foreach (XGBot tBot in tChan.Children)
				{
					foreach (XGPacket tPack in tBot.Children)
					{
						tPack.EnabledChangedEvent += new ObjectDelegate(packet_ObjectStateChangedEventHandler);
					}
				}
			}

			this.myTimedObjects = new Dictionary<XGObject, DateTime>();
			this.myLatestPacketRequests = new Dictionary<string, DateTime>();
			this.myIsRunning = true;

			this.ConnectedEvent(this.myServer);
		}

		public void Disconnect()
		{
			this.myCon.SendData("QUIT : thank you for using (XG) XdccGrabscher");
			this.myCon.Disconnect();
		}
		private void con_DisconnectedEventHandler()
		{
			this.myIsRunning = false;

			this.myServer.Connected = false;
			this.ObjectChange(this.myServer);

			//if (this.myWatchDogThread != null) { this.myWatchDogThread.Abort(); }
			//if (this.myTimerThread != null) { this.myTimerThread.Abort(); }

			this.myCon.ConnectedEvent -= new EmptyDelegate(con_ConnectedEventHandler);
			this.myCon.DisconnectedEvent -= new EmptyDelegate(con_DisconnectedEventHandler);
			this.myCon.DataTextReceivedEvent -= new DataTextDelegate(con_DataReceivedEventHandler);
			this.myCon = null;

			this.myServer.ChannelAddedEvent -= new ServerChannelDelegate(server_ChannelAddedEventHandler);
			this.myServer.ChannelRemovedEvent -= new ServerChannelDelegate(server_ChannelRemovedEventHandler);

			foreach (XGChannel tChan in this.myServer.Children)
			{
				tChan.EnabledChangedEvent -= new ObjectDelegate(channel_ObjectStateChangedEventHandler);
				foreach (XGBot tBot in tChan.Children)
				{
					foreach (XGPacket tPack in tBot.Children)
					{
						tPack.EnabledChangedEvent -= new ObjectDelegate(packet_ObjectStateChangedEventHandler);
					}
				}
			}

			if (this.myTimedObjects != null) { this.myTimedObjects.Clear(); }
			if (this.myLatestPacketRequests != null) { this.myLatestPacketRequests.Clear(); }

			this.DisconnectedEvent(this.myServer);
		}

		#endregion

		#region DATA HANDLING

		private void con_DataReceivedEventHandler(string aData)
		{
			this.Log("con_DataReceived(" + aData + ")", LogLevel.Traffic);

			if (aData.StartsWith(":"))
			{
				int tSplit = aData.IndexOf(':', 1);
				if (tSplit != -1)
				{
					string[] tCommandList = aData.Split(':')[1].Split(' ');
					string tData = Regex.Replace(aData.Substring(tSplit + 1), "(\u0001|\u0002)", "");

					string tUserName = tCommandList[0].Split('!')[0];
					string tComCodeStr = tCommandList[1];
					string tChannelName = tCommandList[2];

					XGChannel tChan = this.myServer[tChannelName];
					XGBot tBot = this.myServer.getBot(tUserName);

					#region PRIVMSG

					if (tComCodeStr == "PRIVMSG")
					{
						#region VERSION

						if (tData == "VERSION")
						{
							this.Log("con_DataReceived() VERSION: " + Settings.Instance.IrcVersion, LogLevel.Info);
							this.myCon.SendData("NOTICE " + tUserName + " :\u0001VERSION " + Settings.Instance.IrcVersion + "\u0001");
							return;
						}

						#endregion

						#region DCC DOWNLOAD MESSAGE

						else if (tData.StartsWith("DCC") && tBot != null)
						{
							XGPacket tPacket = tBot.getOldestActivePacket();
							if (tPacket != null)
							{
								bool isOk = false;

								int tPort = 0;
								Int64 tChunk = 0;

								string[] tDataList = tData.Split(' ');
								if (tDataList[1] == "SEND")
								{
									this.Log("con_DataReceived() DCC from " + tBot.Name, LogLevel.Notice);
									#region IP CALCULATING
									try
									{
										// this works not in mono?!
										tBot.IP = IPAddress.Parse(tDataList[3]);
									}
									catch (FormatException)
									{
										#region WTF - FLIP THE IP BECAUSE ITS REVERSED?!
										string ip = new IPAddress(long.Parse(tDataList[3])).ToString();
										int pos = 0;
										string realIp = "";
										pos = ip.LastIndexOf('.');
										realIp += ip.Substring(pos + 1) + ".";
										ip = ip.Substring(0, pos);
										pos = ip.LastIndexOf('.');
										realIp += ip.Substring(pos + 1) + ".";
										ip = ip.Substring(0, pos);
										pos = ip.LastIndexOf('.');
										realIp += ip.Substring(pos + 1) + ".";
										ip = ip.Substring(0, pos);
										pos = ip.LastIndexOf('.');
										realIp += ip.Substring(pos + 1);

										this.Log("con_DataReceived() IP parsing failed, using this: " + realIp, LogLevel.Notice);
										tBot.IP = IPAddress.Parse(realIp);
										#endregion
									}
									#endregion
									tPort = int.Parse(tDataList[4]);
									tPacket.RealName = tDataList[2];
									tPacket.RealSize = Int64.Parse(tDataList[5]);

									tChunk = this.myParent.GetNextAvailablePartSize(tPacket.RealName, tPacket.RealSize);
									if (tChunk < 0)
									{
										this.Log("con_DataReceived() file from " + tBot.Name + " already in use", LogLevel.Error);
										tPacket.Enabled = false;
										this.ObjectChange(tPacket);
										//this.CreateTimer(tBot, Settings.Instance.CommandWaitTime);
										this.UnregisterFromBot(tBot);
									}
									else if (tChunk > 0)
									{
										this.Log("con_DataReceived() try resume from " + tBot.Name + " for " + tPacket.RealName + " @ " + tChunk, LogLevel.Notice);
										this.myCon.SendData("PRIVMSG " + tBot.Name + " :\u0001DCC RESUME " + tPacket.RealName + " " + tPort + " " + tChunk + "\u0001");
									}
									else { isOk = true; }
								}

								else if (tDataList[1] == "ACCEPT")
								{
									this.Log("con_DataReceived() DCC resume accepted from " + tBot.Name, LogLevel.Notice);
									tPort = int.Parse(tDataList[3]);
									tChunk = Int64.Parse(tDataList[4]);
									isOk = true;
								}

								if (isOk)
								{
									this.Log("con_DataReceived() downloading from " + tBot.Name + " - Starting: " + tChunk + " - Size: " + tPacket.RealSize, LogLevel.Notice);
									this.NewDownloadEvent(tPacket, tChunk, tBot.IP, tPort);
								}
							}
							else { this.Log("con_DataReceived() DCC not activated from " + tBot.Name, LogLevel.Error); }
						}

						#endregion

						#region DCC INFO MESSAGE

						else if (tChan != null)
						{
							bool insertBot = false;
							if (tBot == null)
							{
								insertBot = true;
								tBot = new XGBot(tChan);
								tBot.Name = tUserName;
								tBot.Connected = true;
								tBot.LastMessage = "initial creation";
							}

							bool isParsed = false;
							Match tMatch = null;
							int valueInt = 0;
							double valueDouble = 0;

							#region PACKET /SLOT / QUEUE INFO

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "(\\*){2,3} ([0-9]*) (pack(s|)|Pa(c|)ket(e|)) (\\*){2,3}\\s*(?<slot_cur>[0-9]*) (of|von) (?<slot_total>[0-9]*) (slot(s|)|Pl(a|�|.)tz(e|)) (open|free|frei|in use|offen)(, ((Queue|Warteschlange): (?<queue_cur>[0-9]*)(\\/| of )(?<queue_cur>[0-9]*),|).*(Record: (?<record>[0-9.]*)(K|)B\\/s|)|)", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;

									if (int.TryParse(tMatch.Groups["slot_cur"].ToString(), out valueInt)) { tBot.InfoSlotCurrent = valueInt; }
									if (int.TryParse(tMatch.Groups["slot_total"].ToString(), out valueInt)) { tBot.InfoSlotTotal = valueInt; }
									if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt)) { tBot.InfoQueueCurrent = valueInt; }
									if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }
									// this is not the all over record speed!
									//if (double.TryParse(tMatch.Groups["record"].ToString(), out valueDouble)) { tBot.InfoSpeedMax = valueDouble; }

									// uhm, there is a free slot and we are still waiting?
									if (tBot.InfoSlotCurrent > 0 && tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
										this.CreateTimer(tBot, 0);
									}
								}
							}

							#endregion

							#region BANDWIDTH

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "((\\*){2,3}|) ((Bandwidth Usage|Bandbreite) ((\\*){2,3}|)|)\\s*(Current|Derzeit): (?<speed_cur>[0-9.]*)(K|)(i|)B(\\/s|s)(,|)(.*Record: (?<speed_max>[0-9.]*)(K|)(i|)B(\\/s|s)|)", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;

									if (int.TryParse(tMatch.Groups["speed_cur"].ToString(), out valueInt)) { tBot.InfoSpeedCurrent = valueInt; }
									if (double.TryParse(tMatch.Groups["speed_max"].ToString(), out valueDouble)) { tBot.InfoSpeedMax = valueDouble; }
								}
							}

							#endregion

							#region PACKET INFO

							XGPacket newPacket = null;
							if (!isParsed)
							{ // what is this damn char \240 and how to rip it off ???
								tMatch = Regex.Match(tData, "#(?<pack_id>\\d+)(\u0240|�|)\\s+(\\d*)x\\s+\\[\\s*(�|)(?<pack_size>[\\<\\>\\d.]+)(?<pack_add>[BbGgiKMs]+)\\]\\s+(?<pack_name>.*)", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;

									try
									{
										int tPacketId = int.Parse(tMatch.Groups["pack_id"].ToString());

										XGPacket tPack = tBot[tPacketId];
										if (tPack == null)
										{
											tPack = new XGPacket(tBot);
											newPacket = tPack;
											tPack.EnabledChangedEvent += new ObjectDelegate(packet_ObjectStateChangedEventHandler);
											tPack.Id = tPacketId;
										}
										else
										{
											tPack.Modified = false;
										}

										string name = this.ClearString(tMatch.Groups["pack_name"].ToString());
										if (tPack.Name != name && tPack.Name != "")
										{
											//this.Log(this, "The Packet " + tPack.Id + "(" + tPacketId + ") name changed from '" + tPack.Name + "' to '" + name + "' maybee they changed the content", LogLevel.Warning);
											tPack.Enabled = false;
											if (!tPack.Connected)
											{
												tPack.RealName = "";
												tPack.RealSize = 0;
											}
										}
										tPack.Name = name;

										double tPacketSizeFormated = 0;
										double.TryParse(tMatch.Groups["pack_size"].ToString().Replace(".", ",").Replace("<", "").Replace(">", ""), out tPacketSizeFormated);

										string tPacketAdd = tMatch.Groups["pack_add"].ToString().ToLower();

										if (tPacketAdd == "k" || tPacketAdd == "kb") { tPack.Size = (Int64)(tPacketSizeFormated * 1024); }
										else if (tPacketAdd == "m" || tPacketAdd == "mb") { tPack.Size = (Int64)(tPacketSizeFormated * 1024 * 1024); }
										else if (tPacketAdd == "g" || tPacketAdd == "gb") { tPack.Size = (Int64)(tPacketSizeFormated * 1024 * 1024 * 1024); }

										if (tPack.Modified)
										{
											this.ObjectChange(tPack);
											this.Log("con_DataReceived() updated packet #" + tPack.Id + " from " + tBot.Name, LogLevel.Info);
										}
									}
									catch (FormatException) { }
								}
							}

							#endregion

							// insert bot if ok
							if (insertBot)
							{
								if (isParsed)
								{
									this.ObjectAddedEvent(tBot.Parent, tBot);
									this.Log("con_DataReceived() inserted bot " + tBot.Name, LogLevel.Info);
								}
								else
								{
									tBot.Parent.removeBot(tBot);
									tBot.Modified = false;
								}
							}
							// and insert packet _AFTER_ this
							if (newPacket != null)
							{
								this.ObjectAddedEvent(tBot, newPacket);
								this.Log("con_DataReceived() inserted packet #" + newPacket.Id + " into " + tBot.Name, LogLevel.Info);
							}

#if DEBUG
							#region NOT NEEDED INFOS

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "(\\*){2,3} To request .* type .*", RegexOptions.IgnoreCase);
								if (tMatch.Success) { return; }
								tMatch = Regex.Match(tData, ".*\\/(msg|ctcp) .* xdcc (info|send) .*", RegexOptions.IgnoreCase);
								if (tMatch.Success) { return; }
								tMatch = Regex.Match(tData, "(\\*){2,3} To list a group, type .*", RegexOptions.IgnoreCase);
								if (tMatch.Success) { return; }
								tMatch = Regex.Match(tData, "Total offered(\\!|): (\\[|)[0-9.]*\\s*[BeGgiKMsTty]+(\\]|)\\s*Total transfer(r|)ed: (\\[|)[0-9.]*\\s*[BeGgiKMsTty]+(\\]|)", RegexOptions.IgnoreCase);
								if (tMatch.Success) { return; }
								tMatch = Regex.Match(tData, ".* (brought to you|powered|sp(o|0)ns(o|0)red) by .*", RegexOptions.IgnoreCase);
								if (tMatch.Success) { return; }
								tMatch = Regex.Match(tData, "(\\*){2,3} .*" + tChan.Name + " (\\*){2,3}", RegexOptions.IgnoreCase);
								if (tMatch.Success) { return; }
							}

							#endregion

							#region COULD NOT PARSE

							// maybee delete this because it is flooding the logfile
							if (!isParsed && tBot.Children.Length > 0)
							{
								this.ParsingErrorEvent("[DCC Info] " + tBot.Name + " : " + this.ClearString(tData));
							}

							#endregion
#endif
						}

						#endregion
					}

					#endregion

					#region NOTICE

					else if (tComCodeStr == "NOTICE")
					{
						if (tBot != null)
						{
							bool isParsed = false;
							Match tMatch = null;
							Match tMatch1 = null;
							Match tMatch2 = null;
							Match tMatch3 = null;
							Match tMatch4 = null;
							Match tMatch5 = null;

							int valueInt = 0;
							tData = this.ClearString(tData);
							//double valueDouble = 0;

							#region ALL SLOTS FULL / ADDING TO QUEUE

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "((\\*){2,3} All Slots Full, |)Added you to the main queue (for pack ([0-9]+) \\(\".*\"\\) |).*in positi(o|0)n (?<queue_cur>[0-9]+)\\. To Remove you(r|)self at a later time .*", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "Queueing you for pack [0-9]+ \\(.*\\) in slot (?<queue_cur>[0-9]+)/(?<queue_total>[0-9]+)\\. To remove you(r|)self from the queue, type: .*\\. To check your position in the queue, type: .*\\. Estimated time remaining in queue: (?<queue_d>[0-9]+) days, (?<queue_h>[0-9]+) hours, (?<queue_m>[0-9]+) minutes", RegexOptions.IgnoreCase);
								tMatch3 = Regex.Match(tData, "[(\\*){2,3} |]Es laufen bereits genug .bertragungen, Du bist jetzt in der Warteschlange f.r Datei [0-9]+ \\(.*\\) in Position (?<queue_cur>[0-9]+)\\. Wenn Du sp.ter Abbrechen willst schreibe .*", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success || tMatch3.Success)
								{
									tMatch = tMatch1.Success ? tMatch1 : tMatch2;
									tMatch = tMatch.Success ? tMatch : tMatch3;
									isParsed = true;
									if (tBot.BotState == BotState.Idle)
									{
										tBot.BotState = BotState.Waiting;
									}

									tBot.InfoSlotCurrent = 0;
									if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt))
									{
										tBot.QueuePosition = valueInt;
										tBot.InfoQueueCurrent = tBot.QueuePosition;
									}

									if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }
									else if(tBot.InfoQueueTotal < tBot.InfoQueueCurrent) { tBot.InfoQueueTotal = tBot.InfoQueueCurrent; }

									int time = 0;
									if (int.TryParse(tMatch.Groups["queue_m"].ToString(), out valueInt)) { time += valueInt * 60; }
									if (int.TryParse(tMatch.Groups["queue_h"].ToString(), out valueInt)) { time += valueInt * 60 * 60; }
									if (int.TryParse(tMatch.Groups["queue_d"].ToString(), out valueInt)) { time += valueInt * 60 * 60 * 24; }
									tBot.QueueTime = time;
								}
							}

							#endregion

							#region REMOVE FROM QUEUE

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "(\\*){2,3} Removed From Queue: .*", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
									}
									this.CreateTimer(tBot, Settings.Instance.CommandWaitTime);
								}
							}

							#endregion

							#region INVALID PACKET NUMBER

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "(\\*){2,3} Die Nummer der Datei ist ung.ltig", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "(\\*){2,3} Invalid Pack Number, Try Again", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success)
								{
									tMatch = tMatch1.Success ? tMatch1 : tMatch2;
									isParsed = true;
									XGPacket tPack = tBot.getOldestActivePacket();
									if (tPack != null)
									{
										tPack.Enabled = false;
										tBot.removePacket(tPack);
										this.ObjectRemovedEvent(tBot, tPack);
									}
									this.Log("con_DataReceived() invalid packetnumber from " + tBot.Name, LogLevel.Error);
								}
							}

							#endregion

							#region PACK ALREADY REQUESTED

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "(\\*){2,3} You already requested that pack(.*|)", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "(\\*){2,3} Du hast diese Datei bereits angefordert(.*|)", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Idle)
									{
										tBot.BotState = BotState.Waiting;
									}
								}
							}

							#endregion

							#region ALREADY QUEUED / RECEIVING

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "Denied, You already have ([0-9]+) item(s|) queued, Try Again Later", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "(\\*){2,3} All Slots Full, Denied, You already have that item queued\\.", RegexOptions.IgnoreCase);
								tMatch3 = Regex.Match(tData, "You are already receiving or are queued for the maximum number of packs .*", RegexOptions.IgnoreCase);
								tMatch4 = Regex.Match(tData, "Du hast max\\. ([0-9]+) transfer auf einmal, Du bist jetzt in der Warteschlange f.r Datei .*", RegexOptions.IgnoreCase);
								tMatch5 = Regex.Match(tData, "Es laufen bereits genug .bertragungen, abgewiesen, Du hast diese Datei bereits in der Warteschlange\\.", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success || tMatch3.Success || tMatch4.Success || tMatch5.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Idle)
									{
										tBot.BotState = BotState.Waiting;
									}
								}
							}

							#endregion

							#region DCC PENDING

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "(\\*){2,3} You have a DCC pending, Set your client to receive the transfer\\. ((Type .*|Send XDCC CANCEL) to abort the transfer\\. |)\\((?<time>[0-9]+) seconds remaining until timeout\\)", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "(\\*){2,3} Du hast eine .bertragung schwebend, Du mu.t den Download jetzt annehmen\\. ((Schreibe .*|Sende XDCC CANCEL)            an den Bot um die .bertragung abzubrechen\\. |)\\((?<time>[0-9]+) Sekunden bis zum Abbruch\\)", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success)
								{
									tMatch = tMatch1.Success ? tMatch1 : tMatch2;
									isParsed = true;
									if (int.TryParse(tMatch.Groups["time"].ToString(), out valueInt))
									{
										if (valueInt == 30 && tBot.BotState != BotState.Active)
										{
											tBot.BotState = BotState.Idle;
										}
										this.CreateTimer(tBot, (valueInt + 2) * 1000);
									}
								}
							}

							#endregion

							#region ALL SLOTS AND QUEUE FULL

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "(\\*){2,3} All Slots Full, Main queue of size (?<queue_total>[0-9]+) is Full, Try Again Later", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "(\\*){2,3} Es laufen bereits genug .bertragungen, abgewiesen, die Warteschlange ist voll, max\\. (?<queue_total>[0-9]+) Dateien, Versuche es sp.ter nochmal", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success)
								{
									tMatch = tMatch1.Success ? tMatch1 : tMatch2;
									isParsed = true;
									if (tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
									}
									tBot.InfoSlotCurrent = 0;
									tBot.InfoQueueCurrent = 0;
									if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }

									this.CreateTimer(tBot, Settings.Instance.BotWaitTime);
								}
							}

							#endregion

							#region TRANSFER LIMIT

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "(\\*){2,3} You can only have ([0-9]+) transfer(s|) at a time,.*", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Idle)
									{
										tBot.BotState = BotState.Waiting;
									}
								}
							}

							#endregion

							#region OWNER REQUEST

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "(\\*){2,3} The Owner Has Requested That No New Connections Are Made In The Next (?<time>[0-9]+) Minute(s|)", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
									}

									if (int.TryParse(tMatch.Groups["time"].ToString(), out valueInt))
									{
										this.CreateTimer(tBot, (valueInt * 60 + 1) * 1000);
									}
								}
							}

							#endregion

							#region XDCC DOWN

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "The XDCC is down, try again later.*", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
									}
									this.CreateTimer(tBot, Settings.Instance.BotWaitTime);
								}
							}

							#endregion

							#region XDCC DENIED

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "(\\*){2,3} XDCC SEND denied, (?<info>.*)", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;
									string info = tMatch.Groups["info"].ToString().ToLower();
									if (info.StartsWith("you must be on a known channel to request a pack"))
									{
										this.myCon.SendData("WHOIS " + tBot.Name);
									}
									else if (info.StartsWith("i don't send transfers to"))
									{
										foreach (XGPacket tPacket in tBot.Children)
										{
											if (tPacket.Enabled)
											{
												tPacket.Enabled = false;
												this.ObjectChange(tPacket);
											}
										}
									}
									else
									{
										if (tBot.BotState == BotState.Waiting)
										{
											tBot.BotState = BotState.Idle;
										}
										this.CreateTimer(tBot, Settings.Instance.CommandWaitTime);
										this.Log("con_DataReceived() XDCC denied from " + tBot.Name + ": " + info, LogLevel.Error);
									}
								}
							}

							#endregion

							#region XDCC SENDING

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "(\\*){2,3} Sending You (Your Queued |)Pack .*", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "(\\*){2,3} Sende dir jetzt die Datei .*", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
									}
								}
							}

							#endregion

							#region QUEUED

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "Queued ([0-9]+)h([0-9]+)m for .*, in position (?<queue_cur>[0-9]+) of (?<queue_total>[0-9]+). (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m or .* remaining\\.", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "In der Warteschlange seit  ([0-9]+)h([0-9]+)m f.r .*, in Position (?<queue_cur>[0-9]+) von (?<queue_total>[0-9]+). Ungef.hr (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m oder .*", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success)
								{
									tMatch = tMatch1.Success ? tMatch1 : tMatch2;
									isParsed = true;
									if (tBot.BotState == BotState.Idle)
									{
										tBot.BotState = BotState.Waiting;
									}
									
									tBot.InfoSlotCurrent = 0;
									if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt)) { tBot.QueuePosition = valueInt; }
									if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt)) { tBot.InfoQueueTotal = valueInt; }
									else if(tBot.InfoQueueTotal < tBot.QueuePosition) { tBot.InfoQueueTotal = tBot.QueuePosition; }

									int time = 0;
									if (int.TryParse(tMatch.Groups["queue_m"].ToString(), out valueInt)) { time += valueInt * 60; }
									if (int.TryParse(tMatch.Groups["queue_h"].ToString(), out valueInt)) { time += valueInt * 60 * 60; }
									tBot.QueueTime = time;
								}
							}

							#endregion

							#region CLOSING CONNECTION

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "(\\*){2,3} (Closing Connection:|Transfer Completed).*", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "(\\*){2,3} (Schliese Verbindung:).*", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success)
								{
									isParsed = true;
									if (tBot.BotState != BotState.Active)
									{
										tBot.BotState = BotState.Idle;
									}
									else
									{
										// kill that connection if the bot sends a close message , but our real bot 
										// connection is still alive and hangs for some crapy reason - maybe because 
										// some admins do some network fu to stop my downloads (happend to me)
										this.KillDownloadEvent(tBot);
									}
									this.CreateTimer(tBot, Settings.Instance.CommandWaitTime);
								}
							}

							#endregion

							#region YOU ARE NOT IN QUEUE

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, "(You Don't Appear To Be In A Queue|Removed you from the queue for.*)", RegexOptions.IgnoreCase);
								if (tMatch.Success)
								{
									isParsed = true;
									if (tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
									}
									tBot.QueuePosition = 0;
									this.CreateTimer(tBot, Settings.Instance.CommandWaitTime);
								}
							}

							#endregion

							#region PUNISH / AUTO IGNORE

							if (!isParsed)
							{
								tMatch1 = Regex.Match(tData, "Punish-ignore activated for .* \\(.*\\) (?<time_m>[0-9]*) minutes", RegexOptions.IgnoreCase);
								tMatch2 = Regex.Match(tData, "Auto-ignore activated for .* lasting (?<time_m>[0-9]*)m(?<time_s>[0-9]*)s\\. Further messages will increase duration\\.", RegexOptions.IgnoreCase);
								tMatch3 = Regex.Match(tData, "Zur Strafe wirst du .* \\(.*\\) f.r (?<time_m>[0-9]*) Minuten ignoriert(.|)", RegexOptions.IgnoreCase);
								tMatch4 = Regex.Match(tData, "Auto-ignore activated for .* \\(.*\\)", RegexOptions.IgnoreCase);
								if (tMatch1.Success || tMatch2.Success || tMatch3.Success)
								{
									tMatch = tMatch1.Success ? tMatch1 : tMatch2.Success ? tMatch2 : tMatch3;
									isParsed = true;
									if (tBot.BotState == BotState.Waiting)
									{
										tBot.BotState = BotState.Idle;
									}

									if (int.TryParse(tMatch.Groups["time_m"].ToString(), out valueInt))
									{
										int time = valueInt * 60 + 1;
										if (int.TryParse(tMatch.Groups["time_s"].ToString(), out valueInt))
										{
											time += valueInt;
										}
										this.CreateTimer(tBot, time * 1000, true);
									}
								}
							}

							#endregion

							#region NOT NEEDED INFOS

							if (!isParsed)
							{
								tMatch = Regex.Match(tData, ".* bandwidth limit .*", RegexOptions.IgnoreCase);
								if (tMatch.Success) { isParsed = true; }
							}

							#endregion

							if (!isParsed)
							{
								this.ParsingErrorEvent("[DCC Notice] " + tBot.Name + " : " + tData);
							}
							else
							{
								tBot.LastMessage = tData;
								this.Log("con_DataReceived() message from " + tBot.Name + ": " + tData, LogLevel.Notice);
							}
						}
					}

					#endregion

					#region NICK

					else if (tComCodeStr == "NICK")
					{
						if (tBot != null)
						{
							tBot.Name = tData;
							this.Log("con_DataReceived() bot " + tUserName + " renamed to " + tBot.Name, LogLevel.Info);
						}
					}

					#endregion

					#region KICK

					else if (tComCodeStr == "KICK")
					{
						if (tChan != null)
						{
							tUserName = tCommandList[3];
							if (tUserName == Settings.Instance.IRCName)
							{
								tChan.Connected = false;
								this.Log("con_DataReceived() kicked from " + tChan.Name + (tCommandList.Length >= 5 ? " (" + tCommandList[4] + ")" : "") + " - rejoining", LogLevel.Warning);
								this.Log("con_DataReceived() " + aData, LogLevel.Warning);
								this.JoinChannel(tChan);
							}
							else
							{
								tBot = this.myServer.getBot(tUserName);
								if (tBot != null)
								{
									tBot.Connected = false;
									tBot.LastMessage = "kicked from channel " + tChan.Name;
									this.Log("con_DataReceived() bot " + tBot.Name + " is offline", LogLevel.Info);
								}
							}
						}
					}

					#endregion

					#region JOIN

					else if (tComCodeStr == "JOIN")
					{
						tChannelName = tData;
						tChan = myServer[tChannelName];
						if (tChan != null)
						{
							if (tBot != null)
							{
								tBot.Connected = true;
								tBot.LastMessage = "joined channel " + tChan.Name;
								if (tBot.BotState != BotState.Active)
								{
									tBot.BotState = BotState.Idle;
								}
								this.Log("con_DataReceived() bot " + tUserName + " is online", LogLevel.Info);
								this.RequestFromBot(tBot);
							}
						}
					}

					#endregion

					#region PART

					else if (tComCodeStr == "PART")
					{
						if (tChan != null)
						{
							if (tBot != null)
							{
								tBot.Connected = true;
								tBot.LastMessage = "parted channel " + tChan.Name;
								this.Log("con_DataReceived() bot " + tBot.Name + " parted from " + tChan.Name, LogLevel.Info);
							}
						}
					}

					#endregion

					#region QUIT

					else if (tComCodeStr == "QUIT")
					{
						if (tBot != null)
						{
							tBot.Connected = false;
							tBot.LastMessage = "quited";
							this.Log("con_DataReceived() bot " + tBot.Name + " quited", LogLevel.Info);
						}
					}

					#endregion

					#region	MODE / TOPIC / WALLOP

					else if (tComCodeStr == "MODE" ||
							 tComCodeStr == "TOPIC" ||
							 tComCodeStr == "WALLOP")
					{
						// uhm, what to do now?!
					}

					#endregion

					#region	INVITE

					else if (tComCodeStr == "INVITE")
					{
						this.Log("con_DataReceived() received an invite for channel " + tData, LogLevel.Notice);

						// ok, lets do a silent auto join
						if (Settings.Instance.AutoJoinOnInvite)
						{
							this.Log("con_DataReceived() auto joining " + tData, LogLevel.Notice);
							this.myCon.SendData("JOIN " + tData);
						}
					}

					#endregion

					#region	INT VALUES

					else
					{
						int t_ComCode = 0;
						if (int.TryParse(tComCodeStr, out t_ComCode))
						{
							switch (t_ComCode)
							{
								case 4: // 
									this.myServer.Connected = true;
									this.ObjectChange(this.myServer);
									break;


								case 319: // RPL_WHOISCHANNELS
									tBot = this.myServer.getBot(tCommandList[3]);
									if (tBot != null)
									{
										string chanName = "";
										bool addChan = true;
										string[] tChannelList = aData.Split(':')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
										foreach (string chan in tChannelList)
										{
											chanName = "#" + chan.Split('#')[1];
											if (this.myServer[chanName] != null)
											{
												addChan = false;
												this.RequestFromBot(tBot);
												break;
											}
										}
										if (addChan)
										{
											this.Log("con_DataReceived() auto adding channel " + chanName, LogLevel.Notice);
											this.myServer.addChannel(chanName);
										}
									}
									break;


								case 353: // RPL_NAMREPLY
									tChannelName = tCommandList[4];
									tChan = myServer[tChannelName];
									if (tChan != null)
									{
										string[] tUsers = tData.Split(' ');
										foreach (string user in tUsers)
										{
											string tUser = Regex.Replace(user, "^(@|!|%|\\+){1}", "");
											tBot = tChan[tUser];
											if (tBot != null)
											{
												tBot.Connected = true;
												if (tBot.BotState != BotState.Active)
												{
													tBot.BotState = BotState.Idle;
												}
												this.Log("con_DataReceived() bot " + tBot.Name + " is online", LogLevel.Info);
												this.ObjectChange(tBot);
												this.RequestFromBot(tBot);
											}
										}
									}
									break;


								case 366: // RPL_ENDOFNAMES
									tChannelName = tCommandList[3];
									tChan = myServer[tChannelName];
									if (tChan != null)
									{
										tChan.Connected = true;
										this.Log("con_DataReceived() joined channel " + tChan.Name, LogLevel.Notice);
									}
									break;


								case 376: // RPL_ENDOFMOTD
								case 422: // ERR_NOMOTD
									this.Log("con_DataReceived() really connected", LogLevel.Notice);
									myServer.Connected = true;
									this.ObjectChange(myServer);
									foreach (XGChannel chan in myServer.Children)
									{
										if (chan.Enabled) { this.JoinChannel(chan); }
									}
									break;


								case 477: // ERR_NOCHANMODES
									this.Log("con_DataReceived() registering myself", LogLevel.Notice);
									this.myCon.SendData("nickserv register " + Settings.Instance.IrcRegisterPasswort + " " + Settings.Instance.IrcRegisterEmail);
									this.CreateTimer(tChan, Settings.Instance.ChannelWaitTime);
									break;


								case 471: // ERR_CHANNELISFULL
								case 473: // ERR_INVITEONLYCHAN
								case 474: // ERR_BANNEDFROMCHAN
								case 475: // ERR_BADCHANNELKEY
								case 485: // ERR_UNIQOPPRIVSNEEDED
									tChannelName = tCommandList[3];
									tChan = myServer[tChannelName];
									if (tChan != null)
									{
										tChan.Connected = false;
										this.Log("con_DataReceived() could not join channel " + tChan.Name + ": " + t_ComCode, LogLevel.Warning);
										this.CreateTimer(tChan, t_ComCode == 471 || t_ComCode == 485 ? Settings.Instance.ChannelWaitTime : Settings.Instance.ChannelWaitTimeLong);
									}
									break;
							}
						}
						else
						{
							this.Log("con_DataReceived() Irc code " + tComCodeStr + " could not be parsed. (" + aData + ")", LogLevel.Error);
						}
					}

					#endregion

					this.ObjectChange(tBot);
					this.ObjectChange(tChan);
				}
			}

			#region PING

			else if (aData.StartsWith("PING"))
			{
				this.Log("con_DataReceived() PING", LogLevel.Info);
				this.myCon.SendData("PONG " + aData.Split(':')[1]);
			}

			#endregion
		}

		#endregion

		#region HELPER

		private string ClearString(string aData)
		{ // |\u0031|\u0015)
			return Regex.Replace(aData, "\u0003(\\d+(,\\d+|)|)", "").Trim();
		}

		#endregion

		#region OBJECT

		private void ObjectChange(XGObject aObj)
		{
			if (aObj != null && aObj.Modified)
			{
				this.ObjectChangedEvent(aObj);
				aObj.Modified = false;
			}
		}

		#endregion

		#region BOT

		private void RequestFromBot(object aBot)
		{
			XGBot tBot = aBot as XGBot;
			if (tBot != null)
			{
				if (tBot.BotState == BotState.Idle)
				{
					// check if the packet is already downloaded, or active - than disable it and get the next one
					XGPacket tPacket = tBot.getOldestActivePacket();
					while (tPacket != null)
					{
						Int64 tChunk = this.myParent.GetNextAvailablePartSize(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, tPacket.RealSize != 0 ? tPacket.RealSize : tPacket.Size);
						if (tChunk == -1 || tChunk == -2)
						{
							this.Log("RequestFromBot(" + tBot.Name + ") packet #" + tPacket.Id + " (" + tPacket.Name + ") is already in use", LogLevel.Warning);
							tPacket.Enabled = false;
							this.ObjectChange(tPacket);
							tPacket = tBot.getOldestActivePacket();
						}
						else
						{
							string name = XGHelper.ShrinkFileName(tPacket.RealName != "" ? tPacket.RealName : tPacket.Name, 0);
							if (this.myLatestPacketRequests.ContainsKey(name))
							{
								double time = (this.myLatestPacketRequests[name] - DateTime.Now).TotalMilliseconds;
								if (time > 0)
								{
									this.Log("RequestFromBot(" + tBot.Name + ") packet name " + tPacket.Name + " is blocked for " + time + "ms", LogLevel.Warning);
									this.CreateTimer(tBot, (long)time + 1000);
									return;
								}
							}

							if (this.myServer.Connected)
							{
								this.Log("RequestFromBot(" + tBot.Name + ") requesting packet #" + tPacket.Id + " (" + tPacket.Name + ")", LogLevel.Notice);
								this.myCon.SendData("PRIVMSG " + tBot.Name + " :\u0001XDCC SEND " + tPacket.Id + "\u0001");

								if (this.myLatestPacketRequests.ContainsKey(name)) { this.myLatestPacketRequests.Remove(name); }
								this.myLatestPacketRequests.Add(name, DateTime.Now.AddMilliseconds(Settings.Instance.SamePacketRequestTime));
							}

							// create a timer to re request if the bot didnt recognized the privmsg
							this.CreateTimer(tBot, Settings.Instance.BotWaitTime);
							break;
						}
					}
				}
			}
		}

		private void UnregisterFromBot(XGBot aBot)
		{
			if (aBot != null && myServer[aBot.Name] != null)
			{
				this.Log("UnregisterFromBot(" + aBot.Name + ")", LogLevel.Notice);
				this.myCon.SendData("PRIVMSG " + aBot.Name + " :XDCC REMOVE");
				this.CreateTimer(aBot, Settings.Instance.CommandWaitTime);
			}
		}

		#endregion

		#region CHANNEL

		private void JoinChannel(object aChan)
		{
			XGChannel tChan = aChan as XGChannel;
			if (tChan != null && myServer[tChan.Name] != null)
			{
				this.Log("JoinChannel(" + tChan.Name + ")", LogLevel.Notice);
				this.myCon.SendData("JOIN " + tChan.Name);
			}
		}

		public void PartChannel(XGChannel aChan)
		{
			if (aChan != null)
			{
				this.Log("PartChannel(" + aChan.Name + ")", LogLevel.Notice);
				this.myCon.SendData("PART " + aChan.Name);
			}
		}

		#endregion

		#region EVENTS

		private void server_ChannelAddedEventHandler(XGServer aServer, XGChannel aChan)
		{
			aChan.EnabledChangedEvent += new ObjectDelegate(channel_ObjectStateChangedEventHandler);
			this.ObjectAddedEvent(aServer, aChan);
			foreach (XGBot tBot in aChan.Children)
			{
				foreach (XGPacket tPack in tBot.Children)
				{
					tPack.EnabledChangedEvent += new ObjectDelegate(packet_ObjectStateChangedEventHandler);
				}
			}
			if (aChan.Enabled) { this.JoinChannel(aChan); }
		}

		private void server_ChannelRemovedEventHandler(XGServer aServer, XGChannel aChan)
		{
			this.ObjectRemovedEvent(aServer, aChan);
			aChan.EnabledChangedEvent -= new ObjectDelegate(channel_ObjectStateChangedEventHandler);
			foreach (XGBot tBot in aChan.Children)
			{
				foreach (XGPacket tPack in tBot.Children)
				{
					tPack.Enabled = false;
					tPack.EnabledChangedEvent -= new ObjectDelegate(packet_ObjectStateChangedEventHandler);
				}
			}
			this.PartChannel(aChan);
		}

		private void channel_ObjectStateChangedEventHandler(XGObject aObj)
		{
			XGChannel tChan = aObj as XGChannel;

			if (tChan.Enabled) { this.JoinChannel(tChan); }
			else
			{
				bool exit = true;
				foreach (XGChannel chan in myServer.Children)
				{
					if (chan.Enabled)
					{
						exit = false;
						break;
					}
				}
				// just part the channel
				if (!exit) { this.PartChannel(tChan); }
				// nothing left, so we can disconnect
				else { this.Disconnect(); }
			}
		}

		private void packet_ObjectStateChangedEventHandler(XGObject aObj)
		{
			XGPacket tPack = aObj as XGPacket;
			XGBot tBot = tPack.Parent;
			if (tPack.Enabled)
			{
				if (tBot.getOldestActivePacket() == tPack) { this.RequestFromBot(tBot); }
			}
			else
			{
				if (tBot.BotState == BotState.Waiting || tBot.BotState == BotState.Active)
				{
					if (tBot.getOldestActivePacket(true) == tPack) { this.UnregisterFromBot(tBot); }
				}
			}
		}

		#endregion

		#region TIMER

		public void TriggerTimerRun()
		{
			List<XGObject> remove = new List<XGObject>();
			foreach (KeyValuePair<XGObject, DateTime> kvp in this.myTimedObjects)
			{
				DateTime time = kvp.Value;
				if ((time - DateTime.Now).TotalMilliseconds < 0) { remove.Add(kvp.Key); }
			}
			foreach (XGObject obj in remove)
			{
				this.myTimedObjects.Remove(obj);

				if (obj.GetType() == typeof(XGChannel)) { this.JoinChannel(obj as XGChannel); }
				else if (obj.GetType() == typeof(XGBot)) { this.RequestFromBot(obj as XGBot); }
			}
		}

		public void CreateTimer(XGObject aObject, Int64 aTime)
		{
			this.CreateTimer(aObject, aTime, false);
		}
		private void CreateTimer(XGObject aObject, Int64 aTime, bool aOverride)
		{
			if (aOverride && this.myTimedObjects.ContainsKey(aObject))
			{
				this.myTimedObjects.Remove(aObject);
			}

			if (!this.myTimedObjects.ContainsKey(aObject))
			{
				this.myTimedObjects.Add(aObject, DateTime.Now.AddMilliseconds(aTime));
			}
		}

		#endregion

		#region LOG

		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("ServerConnect(" + this.myServer.Name + ":" + this.myServer.Port + ")." + aData, aLevel);
		}

		#endregion
	}
}
