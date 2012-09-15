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
using System.Runtime.Serialization;

using XG.Core;

namespace XG.Server.Plugin.General.Webserver.JQGrid
{
	[DataContract]
	public class Objects
	{
		[DataMember (Name = "page")]
		public int Page { get; set; }

		[DataMember (Name = "total")]
		public int Total
		{
			get
			{
				return (int)Math.Ceiling((double)_gridObjects.Count() / (double)Rows);
			}
			private set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}
		
		public int Rows { get; set; }

		[DataMember (Name = "records")]
		public int Records
		{
			get { return _gridObjects.Count(); }
			private set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}

		[DataMember (Name = "rows")]
		public IEnumerable<Object> RowObjects
		{
			get
			{
				int start = (Page - 1) * Rows;
				int count = Records < start + Rows ? Records - start : Rows;
				return _gridObjects.GetRange(start, count);
			}
			private set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}

		List<Object> _gridObjects;
		public void SetObjects(IEnumerable<AObject> aObjects)
		{
			_gridObjects = new List<Object>();
			foreach (AObject tObject in aObjects)
			{
				_gridObjects.Add(new Object(tObject));
			}
		}
	}
}

