//
//  Server.cs
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
using NUnit.Framework;

using XG.Server.Plugin.General.Webserver.Websocket;
using System.Collections.Generic;

namespace XG.Server.Plugin.General.Webserver.Websocket.Test
{
	[TestFixture()]
	public class Server
	{
		[Test()]
		public void FilterDuplicateEntries ()
		{
			var server = new Websocket.Server();

			var oldList = new List<Int64[]>();
			oldList.Add(new Int64[]{11, 1});
			oldList.Add(new Int64[]{12, 1});
			oldList.Add(new Int64[]{13, 2});
			oldList.Add(new Int64[]{14, 2});
			oldList.Add(new Int64[]{15, 2});
			oldList.Add(new Int64[]{16, 2});
			oldList.Add(new Int64[]{17, 3});
			oldList.Add(new Int64[]{18, 3});
			oldList.Add(new Int64[]{19, 3});
			oldList.Add(new Int64[]{20, 1});
			oldList.Add(new Int64[]{21, 1});
			oldList.Add(new Int64[]{22, 2});
			oldList.Add(new Int64[]{23, 2});
			oldList.Add(new Int64[]{24, 3});
			oldList.Add(new Int64[]{25, 1});
			oldList.Add(new Int64[]{26, 1});
			oldList.Add(new Int64[]{27, 1});

			var newList = new List<Int64[]>();
			newList.Add(new Int64[]{11, 1});
			newList.Add(new Int64[]{12, 1});
			newList.Add(new Int64[]{13, 2});
			newList.Add(new Int64[]{16, 2});
			newList.Add(new Int64[]{17, 3});
			newList.Add(new Int64[]{19, 3});
			newList.Add(new Int64[]{20, 1});
			newList.Add(new Int64[]{21, 1});
			newList.Add(new Int64[]{22, 2});
			newList.Add(new Int64[]{23, 2});
			newList.Add(new Int64[]{24, 3});
			newList.Add(new Int64[]{25, 1});
			newList.Add(new Int64[]{27, 1});

			Assert.AreEqual(newList, server.FilterDuplicateEntries(oldList.ToArray()));
		}
	}
}

