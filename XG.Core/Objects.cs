// 
//  Objects.cs
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

namespace XG.Core
{
	[Serializable()]
	public class Objects : AObjects
	{
		public new IEnumerable<Object> All
		{
			get { return base.All.Cast<Object>(); }
		}

		public void Add(Object aObject)
		{
			base.Add(aObject);
		}

		public void Remove(Object aObject)
		{
			base.Remove(aObject);
		}

		public new Object Named(string aName)
		{
			AObject tObject = base.Named(aName);
			return tObject != null ? (Object)tObject : null;
		}
	}
}
