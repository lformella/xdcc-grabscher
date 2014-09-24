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
using log4net;
using Meebey.SmartIrc4net;
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser
{
	public abstract class AParser : ANotificationSender
	{
		#region VARIABLES

		protected readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<Bot>> OnUnRequestFromBot = delegate {};

		protected void FireUnRequestFromBot(object aSender, EventArgs<Bot> aEventArgs)
		{
			OnUnRequestFromBot(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Bot, int>> OnQueueRequestFromBot = delegate {};

		protected void FireQueueRequestFromBot(object aSender, EventArgs<Bot, int> aEventArgs)
		{
			OnQueueRequestFromBot(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Bot>> OnJoinChannelsFromBot = delegate {};

		protected void FireJoinChannelsFromBot(object aSender, EventArgs<Bot> aEventArgs)
		{
			OnJoinChannelsFromBot(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Bot>> OnRemoveDownload = delegate {};

		protected void FireRemoveDownload(object aSender, EventArgs<Bot> aEventArgs)
		{
			OnRemoveDownload(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Server, string>> OnJoinChannel = delegate {};

		protected void FireJoinChannel(object aSender, EventArgs<Server, string> aEventArgs)
		{
			OnJoinChannel(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Packet, Int64, IPAddress, int>> OnAddDownload = delegate {};

		protected void FireAddDownload(object aSender, EventArgs<Packet, Int64, IPAddress, int> aEventArgs)
		{
			OnAddDownload(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Server, SendType, string, string>> OnSendMessage = delegate {};

		protected void FireSendMessage(object aSender, EventArgs<Server, SendType, string, string> aEventArgs)
		{
			OnSendMessage(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Server, string>> OnWriteLine = delegate {};

		protected void FireWriteLine(object aSender, EventArgs<Server, string> aEventArgs)
		{
			OnWriteLine(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Model.Domain.Channel, string, string>> OnXdccList = delegate {};

		protected void FireXdccList(object aSender, EventArgs<Model.Domain.Channel, string, string> aEventArgs)
		{
			OnXdccList(aSender, aEventArgs);
		}

		public event EventHandler<EventArgs<Server, string, Int64, IPAddress, int>> OnDownloadXdccList = delegate {};

		protected void FireDownloadXdccList(object aSender, EventArgs<Server, string, Int64, IPAddress, int> aEventArgs)
		{
			OnDownloadXdccList(aSender, aEventArgs);
		}

		#endregion

		#region PARSING

		public abstract bool Parse(Message aMessage);

		#endregion
	}
}

