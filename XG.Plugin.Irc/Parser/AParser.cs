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
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser
{
	public abstract class AParser : ANotificationSender
	{
		#region VARIABLES

		protected readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<Bot>> OnUnRequestFromBot;
		protected void FireUnRequestFromBot(object aSender, EventArgs<Bot> aEventArgs)
		{
			if (OnUnRequestFromBot != null)
			{
				OnUnRequestFromBot(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<Bot, int>> OnQueueRequestFromBot;
		protected void FireQueueRequestFromBot(object aSender, EventArgs<Bot, int> aEventArgs)
		{
			if (OnQueueRequestFromBot != null)
			{
				OnQueueRequestFromBot(aSender, aEventArgs);
			}

		}

		public event EventHandler<EventArgs<Bot>> OnJoinChannelsFromBot;
		protected void FireJoinChannelsFromBot(object aSender, EventArgs<Bot> aEventArgs)
		{
			if (OnJoinChannelsFromBot != null)
			{
				OnJoinChannelsFromBot(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<Bot>> OnRemoveDownload;
		protected void FireRemoveDownload(object aSender, EventArgs<Bot> aEventArgs)
		{
			if (OnRemoveDownload != null)
			{
				OnRemoveDownload(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<Server, string>> OnJoinChannel;
		protected void FireJoinChannel(object aSender, EventArgs<Server, string> aEventArgs)
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

		public event EventHandler<EventArgs<Server, SendType, string, string>> OnSendMessage;
		protected void FireSendMessage(object aSender, EventArgs<Server, SendType, string, string> aEventArgs)
		{
			if (OnSendMessage != null)
			{
				OnSendMessage(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<Server, string>> OnWriteLine;
		protected void FireWriteLine(object aSender, EventArgs<Server, string> aEventArgs)
		{
			if (OnWriteLine != null)
			{
				OnWriteLine(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<Model.Domain.Channel, string, string>> OnXdccList;
		protected void FireXdccList(object aSender, EventArgs<Model.Domain.Channel, string, string> aEventArgs)
		{
			if (OnXdccList != null)
			{
				OnXdccList(aSender, aEventArgs);
			}
		}

		public event EventHandler<EventArgs<Server, string, Int64, IPAddress, int>> OnDownloadXdccList;
		protected void FireDownloadXdccList(object aSender, EventArgs<Server, string, Int64, IPAddress, int> aEventArgs)
		{
			if (OnDownloadXdccList != null)
			{
				OnDownloadXdccList(aSender, aEventArgs);
			}
		}

		#endregion

		#region PARSING

		public abstract bool Parse(Model.Domain.Channel aChannel, string aNick, string aMessage);

		#endregion
	}
}

