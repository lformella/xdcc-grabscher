//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Linq;

namespace XG.Core.Repository
{
	[Serializable()]
	public class File : XGObject
	{
		#region CHILDREN

		public IEnumerable<XGFile> Files
		{
			get { return base.Children.Cast<XGFile>(); }
		}

		public XGFile this[string tmpPath]
		{
			get
			{
				try
				{
					return this.Files.First(file => file.TmpPath == tmpPath);
				}
				catch {}
				return null;
			}
		}

		public void AddFile(XGFile aFile)
		{
			base.AddChild(aFile);
		}
		public void AddFile(string aName, Int64 aSize)
		{
			XGFile tFile = new XGFile(aName, aSize);
			if (this[tFile.TmpPath] == null)
			{
				this.AddFile(tFile);
			}
		}

		public void RemoveFile(XGFile aFile)
		{
			base.RemoveChild(aFile);
		}

		#endregion

		#region CONSTRUCTOR

		public File() : base()
		{
		}

		public void Clone(Object aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
		}

		#endregion
	}
}
