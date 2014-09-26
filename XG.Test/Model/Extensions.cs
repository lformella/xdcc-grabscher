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

using NUnit.Framework;
using XG.Extensions;

namespace XG.Test.Model
{
	[TestFixture]
	public class Extensions
	{
		[Test]
		public void DifferenceTest()
		{
			string name1 = "F.Scott.Fitzgerald.-.The.Great.Gatsby.epub.ebook.rar";
			string name2;

			name2 = "F.Scott.Fitzgerald.-.The.Great.Gatsby.epub.ebook.rar";
			Assert.AreEqual(0.00, name1.Difference(name2));

			name2 = "F.Scott.Fitzgerald.-.The.Great.Gatsby.epub.ebook";
			Assert.AreEqual(0.08, name1.Difference(name2));

			name2 = "F Scott Fitzgerald - The Great Gatsby epub ebook";
			Assert.AreEqual(0.23, name1.Difference(name2));

			name2 = "[ebook] F Scott Fitzgerald - The Great Gatsby";
			Assert.AreEqual(0.56, name1.Difference(name2));
		}
	}
}
