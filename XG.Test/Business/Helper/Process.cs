// 
//  Process.cs
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
using System.IO;
using NUnit.Framework;

namespace XG.Test.Business.Helper
{
	[TestFixture]
	public class Process
	{
		[Test]
		public void ProcessRunTest()
		{
			const string file = "test.txt";
			const string archive = "test.zip";
			File.Create(file).Close();

			Assert.AreEqual(true, Compress(file, archive, "test"));
			Assert.AreEqual(false, DeCompress(archive));
			File.Delete(archive);

			Assert.AreEqual(true, Compress(file, archive, ""));
			File.Delete(file);
			Assert.AreEqual(true, DeCompress(archive));
			File.Delete(archive);

			File.Delete(file);
		}

		bool Compress(string file, string archive, string password)
		{
			var p = new XG.Business.Helper.Process {Command = "zip", Arguments = (String.IsNullOrEmpty(password) ? "" : "-e" + password + " ") + archive + " " + file};

			return p.Run();
		}

		bool DeCompress(string archive)
		{
			var p = new XG.Business.Helper.Process { Command = "unzip", Arguments = "-x " + archive };

			return p.Run();
		}
	}
}
