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

using System;
using System.Collections.Generic;
using System.Linq;
using XG.Model.Domain;

namespace XG.Plugin.Webserver.Nancy
{
	internal static class Helper
	{
		#region EVENTS

		public static event EventHandler OnShutdown = delegate {};

		public static void FireShutdown(object aSender, EventArgs aEventArgs)
		{
			OnShutdown(aSender, aEventArgs);
		}

		#endregion

		public static Servers Servers { get; set; }
		public static Files Files { get; set; }
		public static Searches Searches { get; set; }
		public static ApiKeys ApiKeys { get; set; }
		public static string Salt { get; set; }
		public static string PasswortHash { get; set; }
		public static RemoteSettings RemoteSettings { get; set; }

		public static IEnumerable<Nancy.Api.Model.Domain.AObject> XgObjectsToNancyObjects(IEnumerable<AObject> aObjects)
		{
			var list = new HashSet<Nancy.Api.Model.Domain.AObject>();

			foreach (var obj in aObjects)
			{
				var convertedObj = XgObjectToNancyObject(obj);
				if (convertedObj != null)
				{
					list.Add(convertedObj);
				}
			}

			return list;
		}

		public static Nancy.Api.Model.Domain.AObject XgObjectToNancyObject(AObject aObject)
		{
			Nancy.Api.Model.Domain.AObject myObj = null;

			if (aObject is Server)
			{
				myObj = new Nancy.Api.Model.Domain.Server { Object = aObject as Server };
			}
			else if (aObject is Channel)
			{
				myObj = new Nancy.Api.Model.Domain.Channel { Object = aObject as Channel };
			}
			else if (aObject is Bot)
			{
				myObj = new Nancy.Api.Model.Domain.Bot { Object = aObject as Bot };
			}
			else if (aObject is Packet)
			{
				myObj = new Nancy.Api.Model.Domain.Packet { Object = aObject as Packet };
			}
			else if (aObject is XG.Model.Domain.Search)
			{
				myObj = new Nancy.Api.Model.Domain.Search { Object = aObject as XG.Model.Domain.Search };
			}
			else if (aObject is File)
			{
				myObj = new Nancy.Api.Model.Domain.File { Object = aObject as File };
			}

			return myObj;
		}

		public static IEnumerable<T> FilterAndLoadObjects<T>(IEnumerable<AObject> aObjects, int aCount, int aPage, string aSortBy, string aSort, out int aLength)
		{
			aPage--;
			var objects = Helper.XgObjectsToNancyObjects(aObjects).Cast<T>();

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
