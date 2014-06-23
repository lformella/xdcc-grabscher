// 
//  Files.cs
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

namespace XG.Model.Domain
{
	public class Files : AObjects
	{
		public IEnumerable<File> All
		{
			get { return base.Children.Cast<File>(); }
		}

		public File File(string tmpName)
		{
			try
			{
				return All.FirstOrDefault(file => file.TmpName == tmpName);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public bool Add(File aFile)
		{
			return base.Add(aFile);
		}

		public bool Add(string aName, Int64 aSize)
		{
			var tFile = new File(aName, aSize);
			if (File(tFile.TmpName) == null)
			{
				return Add(tFile);
			}
			return false;
		}

		public bool Remove(File aFile)
		{
			return base.Remove(aFile);
		}

		protected override bool DuplicateChildExists(AObject aObject)
		{
			return File((aObject as File).TmpName) != null;
		}
	}
}
