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

namespace XG.Test.Model.Domain
{
	[TestFixture]
	public class Helper
	{
		[Test]
		public void ShrinkFileNameTest()
		{
			const string fileName = "This_(is).-an_Evil) (F1le-_-name_[T3st].txt";
			const long fileSize = 440044;
			string result = XG.Model.Domain.Helper.ShrinkFileName(fileName, fileSize);

			Assert.AreEqual("thisisanevilf1lenamet3sttxt.440044", result);
		}

		[Test]
		public void BadCharsInFileNameTest()
		{
			const string fileName = "I/con:?:t:ain?\\bad#cha-rs.txt";
			string result = XG.Model.Domain.Helper.RemoveBadCharsFromFileName(fileName);

			Assert.AreEqual("Icontainbad#cha-rs.txt", result);
		}
	}
}
