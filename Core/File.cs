// 
//  File.cs
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

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

namespace XG.Core
{
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	public class File : AObjects
	{
		#region VARIABLES

		[NonSerialized]
		public object Lock = new object();

		[JsonProperty]
		[MySql]
		public override string Name
		{
			get { return base.Name; }
			set { throw new NotSupportedException("You can not set this Property."); }
		}

		readonly string _tmpPath;

		[JsonProperty]
		[MySql]
		public string TmpPath
		{
			get { return _tmpPath; }
			private set { throw new NotSupportedException("You can not set this Property."); }
		}

		readonly Int64 _size;

		[JsonProperty]
		[MySql]
		public Int64 Size
		{
			get { return _size; }
			private set { throw new NotSupportedException("You can not set this Property."); }
		}

		#endregion

		#region CHILDREN

		[JsonProperty]
		public List<FilePart> Parts
		{
			get { return All.Cast<FilePart>().ToList(); }
			private set { throw new NotSupportedException("You can not set this Property."); }
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

		File() {}

		public File(string aName, Int64 aSize) : this()
		{
			base.Name = aName;
			_size = aSize;
			_tmpPath = Helper.ShrinkFileName(aName, aSize);
		}

		#endregion
	}
}
