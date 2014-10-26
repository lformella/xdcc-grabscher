// 
//  Searches.cs
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
	public class Searches : AObjects
	{
		public IEnumerable<Search> All
		{
			get { return base.Children.Cast<Search>(); }
		}

		public bool Add(Search aObject)
		{
			return base.Add(aObject);
		}

		public bool Remove(Search aObject)
		{
			return base.Remove(aObject);
		}

		public Search WithParameters(string aName, Int64 aSize)
		{
			return (from search in All where search.Name == aName && search.Size == aSize select search).FirstOrDefault();
		}

		public new Search WithGuid(Guid aGuid)
		{
			AObject tObject = base.WithGuid(aGuid);
			return tObject != null ? (Search) tObject : null;
		}

		protected override bool DuplicateChildExists(AObject aObject)
		{
			return WithParameters(aObject.Name, (aObject as Search).Size) != null;
		}
	}
}
