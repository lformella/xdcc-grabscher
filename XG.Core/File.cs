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

namespace XG.Core
{
	[Serializable()]
	public class File : AObjects
	{
		#region VARIABLES

		[field: NonSerialized()]
		public object Locked = new object();

		public override string Name
		{
			get { return base.Name; }
		}

		string _tmpPath;
		public string TmpPath
		{
			get { return _tmpPath; }
		}

		Int64 _size;
		public Int64 Size
		{
			get { return _size; }
		}

		#endregion

		#region CHILDREN

		public IEnumerable<FilePart> Parts
		{
			get { return base.All.Cast<FilePart>(); }
		}

		public bool Add(FilePart aPart)
		{
			return base.Add(aPart);
		}

		public bool Remove(FilePart aPart)
		{
			return base.Remove(aPart);
		}

		#endregion

		#region CONSTRUCTOR

		File() : base()
		{
		}

		public File(string aName, Int64 aSize) : this()
		{
			base.Name = aName;
			_size = aSize;
			_tmpPath = XGHelper.ShrinkFileName(aName, aSize);
		}

		#endregion
	}
}
