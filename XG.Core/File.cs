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

namespace XG.Core
{
	[Serializable()]
	public class XGFile : XGObject
	{
		public new string Name
		{
			get { return base.Name; }
		}

		private string tmpPath;
		public string TmpPath
		{
			get { return this.tmpPath; }
		}

		private Int64 size;
		public Int64 Size
		{
			get { return this.size; }
		}

		public bool addPart(XGFilePart aPart)
		{
			return this.addChild(aPart);
		}
		public bool removePart(XGFilePart aPart)
		{
			return this.removeChild(aPart);
		}

		public XGFile()
			: base()
		{
		}
		public XGFile(string aName, Int64 aSize)
			: this()
		{
			base.Name = aName;
			this.size = aSize;
			this.tmpPath = XGHelper.ShrinkFileName(aName, aSize);
		}

		public void Clone(XGFile aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
			this.size = aCopy.size;
			this.tmpPath = aCopy.tmpPath;
		}
	}
}
