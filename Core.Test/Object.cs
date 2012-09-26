// 
//  Object.cs
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

#if !WINDOWS
using NUnit.Framework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace XG.Core.Test
{
#if !WINDOWS
	[TestFixture()]
#else
    [TestClass]
#endif
	public class Object
	{
		bool _modified = false;

#if !WINDOWS
        [Test()]
#else
        [TestMethod]
#endif
		public void Test ()
		{
			Core.Object obj = new Core.Object();
			obj.Changed += delegate {
				_modified = true;
			};
			AssertModified(obj, false);

			obj.Name = "Test";
			AssertModified(obj, true);

			obj.Guid = Guid.Empty;
			AssertModified(obj, false);

			obj.Connected = true;
			AssertModified(obj, true);

			Core.Object parent = new Core.Object();
			parent.Guid = Guid.NewGuid();

			Assert.AreEqual(Guid.Empty, obj.ParentGuid);
			obj.Parent = parent;
			Assert.AreEqual(parent.Guid, obj.ParentGuid);
			obj.Parent = null;
			Assert.AreEqual(Guid.Empty, obj.ParentGuid);

			AssertModified(obj, false);
		}

		void AssertModified(AObject aObject, bool modified)
		{
			aObject.Commit();
			Assert.AreEqual(this._modified, modified);
			this._modified = false;
		}
	}
}

