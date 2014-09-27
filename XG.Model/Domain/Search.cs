// 
//  Search.cs
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
using Db4objects.Db4o;

namespace XG.Model.Domain
{
	public class Search : AObject
	{
		public static readonly Guid SearchEnabled = Guid.Parse("00000000-0000-0000-0000-000000000001");
		public static readonly Guid SearchDownloads = Guid.Parse("00000000-0000-0000-0000-000000000002");

		#region VARIABLES

		public new Searches Parent
		{
			get { return base.Parent as Searches; }
			set { base.Parent = value; }
		}

		Int64 _size;

		public virtual Int64 Size
		{
			get { return GetProperty(ref _size); }
			set { SetProperty(ref _size, value, "Size"); }
		}

		[Transient]
		int _resultsOnline;

		public int ResultsOnline
		{
			get { return GetProperty(ref _resultsOnline); }
			set { SetProperty(ref _resultsOnline, value, "ResultsOnline"); }
		}

		[Transient]
		int _resultsOffline;

		public int ResultsOffline
		{
			get { return GetProperty(ref _resultsOffline); }
			set { SetProperty(ref _resultsOffline, value, "ResultsOffline"); }
		}

		#endregion

		#region HELPER

		public override string ToString()
		{
			return base.ToString() + "|" + Size;
		}

		#endregion
	}
}
