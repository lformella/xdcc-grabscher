// 
//  Plugin.cs
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
using System.Reflection;
using Nest;
using XG.Config.Properties;
using XG.Extensions;
using XG.Model.Domain;
using log4net;

namespace XG.Plugin.ElasticSearch
{
	public class Plugin : APlugin
	{
		#region VARIABLES
		
		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		ElasticClient _client;
		string _index = "xg";

		#endregion
		
		#region AWorker

		protected override void StartRun()
		{
			var uri = new Uri("http://" + Settings.Default.ElasticSearchHost + ":" + Settings.Default.ElasticSearchPort + "/");
			var setting = new ConnectionSettings(uri);
			_client = new ElasticClient(setting);

			// reindex all
			foreach (Server s in Servers.All)
			{
				Index(s);

				foreach (Channel c in s.Channels)
				{
					Index(c);

					foreach (Bot b in c.Bots)
					{
						Index(b);

						foreach (Packet p in b.Packets)
						{
							Index(p);
						}
					}
				}
			}
		}

		protected override void StopRun()
		{
			_client = null;
		}

		#endregion

		#region EVENTHANDLER

		protected override void ObjectAdded(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			Index(aEventArgs.Value2);

			// reindex all parents if a child is added
			if (aEventArgs.Value2 is Channel)
			{
				Index (aEventArgs.Value2.Parent);
			}
			if (aEventArgs.Value2 is Bot)
			{
				Index (aEventArgs.Value2.Parent);
				Index (aEventArgs.Value2.Parent.Parent);
			}
			if (aEventArgs.Value2 is Packet)
			{
				Index (aEventArgs.Value2.Parent);
				Index (aEventArgs.Value2.Parent.Parent);
				Index (aEventArgs.Value2.Parent.Parent.Parent);
			}
		}

		protected override void ObjectChanged(object aSender, EventArgs<AObject, string[]> aEventArgs)
		{
			Index(aEventArgs.Value1);

			// reindex all packets if a bot is changed
			if (aEventArgs.Value1 is Bot)
			{
				HashSet<string> fields = new HashSet<string>(aEventArgs.Value2);
				if (fields.Contains("Name") || fields.Contains("InfoSpeedCurrent") || fields.Contains("Connected") || fields.Contains("InfoSlotCurrent") || fields.Contains("InfoSlotCurrent") || fields.Contains("InfoQueueCurrent"))
				{
					foreach (var p in (aEventArgs.Value1 as Bot).Packets)
					{
						Index(p);
					}
				}
			}
		}

		protected override void ObjectRemoved(object aSender, EventArgs<AObject, AObject> aEventArgs)
		{
			Remove(aEventArgs.Value2);

			// reindex parent object
			Index(aEventArgs.Value1);

			// drop all children
			if (aEventArgs.Value2 is Server)
			{
				foreach (var channel in (aEventArgs.Value2 as Server).Channels)
				{
					Remove(channel);
					foreach (var bot in channel.Bots)
					{
						Remove(bot);
						foreach (var packet in bot.Packets)
						{
							Remove(packet);
						}
					}
				}
			}
			if (aEventArgs.Value2 is Channel)
			{
				foreach (var bot in (aEventArgs.Value2 as Channel).Bots)
				{
					Remove(bot);
					foreach (var packet in bot.Packets)
					{
						Remove(packet);
					}
				}
			}
			if (aEventArgs.Value2 is Bot)
			{
				foreach (var packet in (aEventArgs.Value2 as Bot).Packets)
				{
					Remove(packet);
				}
			}
		}

		#endregion

		#region FUNCTIONS

		void Index (AObject aObj)
		{
			Object.AObject myObj = null;

			if (aObj is Server)
			{
				myObj = new Object.Server { Object = aObj as Server };
			}
			else if (aObj is Channel)
			{
				myObj = new Object.Channel { Object = aObj as Channel };
			}
			else if (aObj is Bot)
			{
				myObj = new Object.Bot { Object = aObj as Bot };
			}
			else if (aObj is Packet)
			{
				myObj = new Object.Packet { Object = aObj as Packet };
			}

			if (_client != null && myObj != null)
			{
				string type = myObj.GetType().Name.ToLower();

				try
				{
					_client.Index(myObj, i => i.Index(_index).Type(type).Id(myObj.Guid.ToString()));
				}
				catch (Exception ex)
				{
					Log.Fatal("Index(" + aObj + ")", ex);
				}
			}
		}

		void Remove (AObject aObj)
		{
			if (_client != null && (aObj is Server || aObj is Channel || aObj is Bot || aObj is Packet))
			{
				string type = aObj.GetType().Name.ToLower();

				try
				{
					_client.Delete(aObj.Guid.ToString(), i => i.Index(_index).Type(type));
				}
				catch (Exception ex)
				{
					Log.Fatal("Remove(" + aObj + ")", ex);
				}
			}
		}

		#endregion
	}
}
