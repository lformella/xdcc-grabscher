// 
//  PacketSearch.cs
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
using System.ComponentModel.DataAnnotations;

namespace XG.Plugin.Webserver.Nancy.Api.Request
{
	public class PacketSearch : ARequest
	{
		[Required(AllowEmptyStrings = false, ErrorMessage = "SearchTerm is neccesary")]
		public string SearchTerm { get; set; }

		public Int64 Size { get; set; }

		public bool ShowOfflineBots { get; set; }

		public int MaxResults { get; set; }

		public int Page { get; set; }

		[RegularExpression("Name|Id|Size", ErrorMessage = "sortBy just [Name|Id|Size] is allowed")]
		public string SortBy { get; set; }

		[RegularExpression("asc|desc", ErrorMessage = "Sort just [asc|desc] is allowed")]
		public string Sort { get; set; }
	}
}
