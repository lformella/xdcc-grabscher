//
//  AircParser.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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
using System.Net;
using System.Reflection;

using XG.Core;
using XG.Server.Helper;

using Meebey.SmartIrc4net;
using log4net;

namespace XG.Server.Plugin.Core.Irc.Parser
{
	public delegate void ServerBotIntDelegate(XG.Core.Server aServer, Bot aBot, int aInt);
	public delegate void ServerBotTextDelegate(XG.Core.Server aServer, Bot aBot, string aText);
	public delegate void DownloadPacketDelegate(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort);
	public delegate void DownloadFileDelegate(XG.Core.Server aServer, string aBot, IPAddress aIp, int aPort, Int64 size);
	public delegate void ServerDataTextTextDelegate(XG.Core.Server aServer, string aUser, string aData);

	public abstract class AParser : ANotificationSender
	{
		#region VARIABLES

		protected readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public FileActions FileActions { get; set; }

		#endregion

		#region EVENTS

		public event ServerBotDelegate OnUnRequestFromBot;
		protected void FireUnRequestFromBot(XG.Core.Server aServer, Bot aBot)
		{
			if (OnUnRequestFromBot != null)
			{
				OnUnRequestFromBot(aServer, aBot);
			}
		}

		public event ServerBotIntDelegate OnQueueRequestFromBot;
		protected void FireQueueRequestFromBot(XG.Core.Server aServer, Bot aBot, int aInt)
		{
			if (OnQueueRequestFromBot != null)
			{
				OnQueueRequestFromBot(aServer, aBot, aInt);
			}

		}

		public event ServerBotDelegate OnJoinChannelsFromBot;
		protected void FireJoinChannelsFromBot(XG.Core.Server aServer, Bot aBot)
		{
			if (OnJoinChannelsFromBot != null)
			{
				OnJoinChannelsFromBot(aServer, aBot);
			}
		}

		public event ServerBotDelegate OnRemoveDownload;
		protected void FireRemoveDownload(XG.Core.Server aServer, Bot aBot)
		{
			if (OnRemoveDownload != null)
			{
				OnRemoveDownload(aServer, aBot);
			}
		}

		public event ServerDataTextDelegate OnJoinChannel;
		protected void FireJoinChannel(XG.Core.Server aServer, string aData)
		{
			if (OnJoinChannel != null)
			{
				OnJoinChannel(aServer, aData);
			}
		}

		public event DownloadPacketDelegate OnAddDownload;
		protected void FireAddDownload(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort)
		{
			if (OnAddDownload != null)
			{
				OnAddDownload(aPack, aChunk, aIp, aPort);
			}
		}

		public event ServerBotTextDelegate OnSendPrivateMessage;
		protected void FireSendPrivateMessage(XG.Core.Server aServer, Bot aBot, string aData)
		{
			if (OnSendPrivateMessage != null)
			{
				OnSendPrivateMessage(aServer, aBot, aData);
			}
		}

		public event ServerDataTextDelegate OnSendData;
		protected void FireSendData(XG.Core.Server aServer, string aData)
		{
			if (OnSendData != null)
			{
				OnSendData(aServer, aData);
			}
		}

		#endregion

		#region PARSING

		public bool Parse(IrcConnection aConnection, string aMessage, IrcEventArgs aEvent)
		{
			return ParseInternal(aConnection, aMessage, aEvent);
		}

		protected abstract bool ParseInternal(IrcConnection aConnection, string aMessage, IrcEventArgs aEvent);

		#endregion
	}
}

