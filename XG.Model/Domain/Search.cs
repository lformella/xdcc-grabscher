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

namespace XG.Model.Domain
{
	public class Search : AObject
	{
		public static readonly Guid SearchEnabled = Guid.Parse("00000000-0000-0000-0000-000000000001");
		public static readonly Guid SearchDownloads = Guid.Parse("00000000-0000-0000-0000-000000000002");

		#region VARIABLES

		public virtual new Searches Parent
		{
			get { return base.Parent as Searches; }
			set { base.Parent = value; }
		}

		Int64 _resultsOnline;

		public virtual Int64 ResultsOnline
		{
			get { return _resultsOnline; }
			set { SetProperty(ref _resultsOnline, value, "ResultsOnline"); }
		}

		Int64 _resultsOffline;

		public virtual Int64 ResultsOffline
		{
			get { return _resultsOffline; }
			set { SetProperty(ref _resultsOffline, value, "ResultsOffline"); }
		}

		#endregion

		#region FUNCTIONS

		public virtual bool IsVisible(Packet aPacket)
		{
			if (Guid == SearchDownloads)
			{
				return aPacket.Connected;
			}

			if (Guid == SearchEnabled)
			{
				return aPacket.Enabled;
			}

			return aPacket.Name.ContainsAll(Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
		}

		#endregion
	}
}
