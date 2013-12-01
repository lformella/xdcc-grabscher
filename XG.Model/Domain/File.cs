// 
//  File.cs
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
	public class File : AObjects
	{
		#region VARIABLES

		public override string Name
		{
			get { return base.Name; }
			//protected set { base.Name = value; }
		}

		string _tmpPath;

		public virtual string TmpPath
		{
			get { return _tmpPath; }
			protected set { _tmpPath = value; }
		}

		Int64 _size;

		public virtual Int64 Size
		{
			get { return _size; }
			protected set { _size = value; }
		}

		public virtual Int64 CurrentSize
		{
			get
			{
				try
				{
					return (from part in Parts select part.CurrentSize).Sum();
				}
				catch
				{
					return 0;
				}
			}
		}

		public virtual Int64 TimeMissing
		{
			get
			{
				try
				{
					return (from part in Parts select part.TimeMissing).Max();
				}
				catch
				{
					return 0;
				}
			}
		}

		public virtual Int64 Speed
		{
			get
			{
				try
				{
					return (from part in Parts select part.Speed).Sum();
				}
				catch
				{
					return 0;
				}
			}
		}

		#endregion

		#region CHILDREN

		public virtual IEnumerable<FilePart> Parts
		{
			get { return All.Cast<FilePart>(); }
		}

		public virtual bool Add(FilePart aPart)
		{
			return base.Add(aPart);
		}

		public virtual bool Remove(FilePart aPart)
		{
			return base.Remove(aPart);
		}

		public override bool DuplicateChildExists(AObject aObject)
		{
			return false;
		}

		#endregion

		#region CONSTRUCTOR

		protected  File ()
		{}

		public File(string aName, Int64 aSize)
		{
			base.Name = aName;
			_size = aSize;
			_tmpPath = Helper.ShrinkFileName(aName, aSize);
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return base.ToString() + "|" + Size + "|" + TmpPath;
		}

		#endregion
	}
}
