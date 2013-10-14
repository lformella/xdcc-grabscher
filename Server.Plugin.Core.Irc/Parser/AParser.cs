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
	public abstract class AParser : ANotificationSender
	{
		#region VARIABLES

		protected readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public FileActions FileActions { get; set; }

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<XG.Core.Server, Bot>> OnUnRequestFromBot;
		protected void FireUnRequestFromBot(object aSender, EventArgs<XG.Core.Server, Bot> aEventArgs)
		{
			if (OnUnRequestFromBot != null)
			{
				OnUnRequestFromBot(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<XG.Core.Server, Bot, int>> OnQueueRequestFromBot;
		protected void FireQueueRequestFromBot(object aSender, EventArgs<XG.Core.Server, Bot, int> aEventArgs)
		{
			if (OnQueueRequestFromBot != null)
			{
				OnQueueRequestFromBot(aSender, aEventArgs);
			}

		}

		public event EventHandler<EventArgs<XG.Core.Server, Bot>> OnJoinChannelsFromBot;
		protected void FireJoinChannelsFromBot(object aSender, EventArgs<XG.Core.Server, Bot> aEventArgs)
		{
			if (OnJoinChannelsFromBot != null)
			{
				OnJoinChannelsFromBot(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<XG.Core.Server, Bot>> OnRemoveDownload;
		protected void FireRemoveDownload(object aSender, EventArgs<XG.Core.Server, Bot> aEventArgs)
		{
			if (OnRemoveDownload != null)
			{
				OnRemoveDownload(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<XG.Core.Server, string>> OnJoinChannel;
		protected void FireJoinChannel(object aSender, EventArgs<XG.Core.Server, string> aEventArgs)
		{
			if (OnJoinChannel != null)
			{
				OnJoinChannel(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<Packet, Int64, IPAddress, int>> OnAddDownload;
		protected void FireAddDownload(object aSender, EventArgs<Packet, Int64, IPAddress, int> aEventArgs)
		{
			if (OnAddDownload != null)
			{
				OnAddDownload(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<XG.Core.Server, Bot, string>> OnSendPrivateMessage;
		protected void FireSendPrivateMessage(object aSender, EventArgs<XG.Core.Server, Bot, string> aEventArgs)
		{
			if (OnSendPrivateMessage != null)
			{
				OnSendPrivateMessage(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<XG.Core.Server, string>> OnSendData;
		protected void FireSendData(object aSender, EventArgs<XG.Core.Server, string> aEventArgs)
		{
			if (OnSendData != null)
			{
				OnSendData(aSender, aEventArgs);
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

