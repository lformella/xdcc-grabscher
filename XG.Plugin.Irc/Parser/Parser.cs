//
//  Parser.cs
// This file is part of XG - XDCC Grabscher
// http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
//
//  Copyright (c) 2013 Lars Formella
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
using System.Threading;

namespace XG.Plugin.Irc.Parser
{
	public class Parser : AParser
	{
		readonly Queue<Message> _messages = new Queue<Message>();

		AutoResetEvent _waitHandle = new AutoResetEvent(false);

		bool _allowRunning = true;

		readonly List<AParser> _ircParsers = new List<AParser>();

		public void Initialize()
		{
			AddParser(new Types.Nickserv());
			AddParser(new Types.XdccList());
			AddParser(new Types.Dcc.DownloadFromBot());
			AddParser(new Types.Dcc.Version());
			AddParser(new Types.Dcc.XdccListSend());
			AddParser(new Types.Info.Bandwitdh());
			AddParser(new Types.Info.Join());
			AddParser(new Types.Info.Packet());
			AddParser(new Types.Info.Status());
			AddParser(new Types.Xdcc.AllSlotsFull());
			AddParser(new Types.Xdcc.AlreadyReceiving());
			AddParser(new Types.Xdcc.AutoIgnore());
			AddParser(new Types.Xdcc.ClosingConnection());
			AddParser(new Types.Xdcc.DccPending());
			AddParser(new Types.Xdcc.InvalidPacketNumber());
			AddParser(new Types.Xdcc.NotInQueue());
			AddParser(new Types.Xdcc.OwnerRequest());
			AddParser(new Types.Xdcc.PacketAlreadyQueued());
			AddParser(new Types.Xdcc.PacketAlreadyRequested());
			AddParser(new Types.Xdcc.Queued());
			AddParser(new Types.Xdcc.QueueFull());
			AddParser(new Types.Xdcc.RemoveFromQueue());
			AddParser(new Types.Xdcc.TransferLimit());
			AddParser(new Types.Xdcc.XdccDenied());
			AddParser(new Types.Xdcc.XdccDown());
			AddParser(new Types.Xdcc.XdccSending());

			new Thread(new ThreadStart(ParseThread)).Start();
		}

		public void DeInitialize()
		{
			_allowRunning = false;
			_waitHandle.Set();
			foreach (var parser in _ircParsers)
			{
				parser.OnAddDownload -= FireAddDownload;
				parser.OnDownloadXdccList -= FireDownloadXdccList;
				parser.OnJoinChannel -= FireJoinChannel;
				parser.OnJoinChannelsFromBot -= FireJoinChannelsFromBot;
				parser.OnNotificationAdded -= FireNotificationAdded;
				parser.OnQueueRequestFromBot -= FireQueueRequestFromBot;
				parser.OnRemoveDownload -= FireRemoveDownload;
				parser.OnSendMessage -= FireSendMessage;
				parser.OnUnRequestFromBot -= FireUnRequestFromBot;
				parser.OnWriteLine -= FireWriteLine;
				parser.OnXdccList -= FireXdccList;
			}
		}

		public override bool Parse(Message aMessage)
		{
			_messages.Enqueue(aMessage);
			return _waitHandle.Set();
		}

		protected void ParseThread()
		{
			Message tMessage = null;
			while (true)
			{
				if (_messages.Count == 0)
				{
					_waitHandle.WaitOne();
				}
				if (!_allowRunning)
				{
					break;
				}

				try
				{
					tMessage = _messages.Dequeue();
				}
				catch (Exception) {}
				if (tMessage == null)
				{
					continue;
				}

				tMessage.Text = Helper.RemoveSpecialIrcChars(tMessage.Text);
				Log.Debug("Parse() " + _messages.Count + " " + tMessage.Nick + " " + tMessage);

				foreach (var parser in _ircParsers)
				{
					if (parser.Parse(tMessage))
					{
						continue;
					}
				}
			}
		}

		void AddParser(AParser aParser)
		{
			aParser.OnAddDownload += FireAddDownload;
			aParser.OnDownloadXdccList += FireDownloadXdccList;
			aParser.OnJoinChannel += FireJoinChannel;
			aParser.OnJoinChannelsFromBot += FireJoinChannelsFromBot;
			aParser.OnNotificationAdded += FireNotificationAdded;
			aParser.OnQueueRequestFromBot += FireQueueRequestFromBot;
			aParser.OnRemoveDownload += FireRemoveDownload;
			aParser.OnSendMessage += FireSendMessage;
			aParser.OnUnRequestFromBot += FireUnRequestFromBot;
			aParser.OnWriteLine += FireWriteLine;
			aParser.OnXdccList += FireXdccList;

			_ircParsers.Add(aParser);
		}
	}
}
