// 
//  JQGridObject.cs
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
		[DataMember]
		public int page { get; set; }

		[DataMember]
		public int total { get; set; }

		[DataMember]
		public int records
		{
			get { return rows.Count(); }
			set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}

		IEnumerable<Object> _rows;
		[DataMember]
		public IEnumerable<Object> rows
		{
			get
			{
				return _rows.ToArray();
			}
			set
			{
				throw new NotSupportedException("You can not set this Property.");
			}
		}

		public IEnumerable<AObject> objects
		{
			set
			{
				List<Object> gridObjects = new List<Object>();
				foreach (AObject tObject in value)
				{
					Object gridObject = new Object();
					gridObject.cell = tObject;
					gridObjects.Add(gridObject);
				}
				_rows = gridObjects;
			}
		}
	}
}

