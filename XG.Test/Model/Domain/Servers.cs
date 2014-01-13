// 
//  Servers.cs
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
using NUnit.Framework;

namespace XG.Test.Model.Domain
{
	[TestFixture]
	public class Servers
	{
		bool _childAdded;

		[Test]
		public void ServersTest()
		{
			var parent = new XG.Model.Domain.Servers();
			parent.OnAdded += delegate { _childAdded = true; };
			parent.Guid = Guid.NewGuid();

			AssertChildAdded(false);

			var obj = new XG.Model.Domain.Server();
			Assert.AreEqual(Guid.Empty, obj.ParentGuid);
			parent.Add(obj);

			AssertChildAdded(true);
			Assert.AreEqual(parent.Guid, obj.ParentGuid);
		}

		void AssertChildAdded(bool childAdded)
		{
			Assert.AreEqual(_childAdded, childAdded);
			_childAdded = false;
		}
	}
}
