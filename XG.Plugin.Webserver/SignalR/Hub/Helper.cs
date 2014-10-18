// 
//  Helper.cs
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

using System.Linq;
using SharpRobin.Core;
using XG.Model.Domain;
using System.Collections.Generic;

namespace XG.Plugin.Webserver.SignalR.Hub
{
	internal static class Helper
	{
		public static Servers Servers { get; set; }
		public static Files Files { get; set; }
		public static Searches Searches { get; set; }
		public static Notifications Notifications { get; set; }
		public static RrdDb RrdDb { get; set; }
		public static ApiKeys ApiKeys { get; set; }
		public static string PasswortHash { get; set; }
		public static RemoteSettings RemoteSettings { get; set; }

		public static IEnumerable<SignalR.Hub.Model.Domain.AObject> XgObjectsToHubObjects(IEnumerable<AObject> aObjects)
		{
			var list = new HashSet<SignalR.Hub.Model.Domain.AObject>();

			foreach (var obj in aObjects)
			{
				var convertedObj = XgObjectToHubObject(obj);
				if (convertedObj != null)
				{
					list.Add(convertedObj);
				}
			}

			return list;
		}

		public static SignalR.Hub.Model.Domain.AObject XgObjectToHubObject(AObject aObject)
		{
			SignalR.Hub.Model.Domain.AObject myObj = null;

			if (aObject is Server)
			{
				myObj = new SignalR.Hub.Model.Domain.Server { Object = aObject as Server };
			}
			else if (aObject is Channel)
			{
				myObj = new SignalR.Hub.Model.Domain.Channel { Object = aObject as Channel };
			}
			else if (aObject is Bot)
			{
				myObj = new SignalR.Hub.Model.Domain.Bot { Object = aObject as Bot };
			}
			else if (aObject is Packet)
			{
				myObj = new SignalR.Hub.Model.Domain.Packet { Object = aObject as Packet };
			}
			else if (aObject is XG.Model.Domain.Search)
			{
				myObj = new SignalR.Hub.Model.Domain.Search { Object = aObject as XG.Model.Domain.Search };
			}
			else if (aObject is Notification)
			{
				myObj = new SignalR.Hub.Model.Domain.Notification { Object = aObject as Notification };
			}
			else if (aObject is File)
			{
				myObj = new SignalR.Hub.Model.Domain.File { Object = aObject as File };
			}
			else if (aObject is ApiKey)
			{
				myObj = new SignalR.Hub.Model.Domain.ApiKey { Object = aObject as ApiKey };
			}

			return myObj;
		}

		public static IEnumerable<T> FilterAndLoadObjects<T>(IEnumerable<AObject> aObjects, int aCount, int aPage, string aSortBy, string aSort, out int aLength)
		{
			aPage--;
			var objects = Helper.XgObjectsToHubObjects(aObjects).Cast<T>();

			if (string.IsNullOrWhiteSpace(aSortBy))
			{
				aSortBy = "Name";
			}
			var prop = typeof(T).GetProperty(aSortBy);
			if (aSort == "desc")
			{
				objects = objects.OrderByDescending(o => prop.GetValue(o, null));
			}
			else
			{
				objects = objects.OrderBy(o => prop.GetValue(o, null));
			}

			aLength = objects.Count();
			return objects.Skip(aPage * aCount).Take(aCount).ToArray();
		}
	}
}
