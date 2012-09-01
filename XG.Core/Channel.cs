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
	public class Channel : AObjects
	{
		#region VARIABLES

		public new Server Parent
		{
			get { return base.Parent as Server; }
			set { base.Parent = value; }
		}

		int errorCode = 0;
		public int ErrorCode
		{
			get { return errorCode; }
			set
			{
				if (errorCode != value)
				{
					errorCode = value;
					Modified = true;
				}
			}
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Bot> Bots
		{
			get { return base.All.Cast<Bot>(); }
		}

		public Bot this[string name]
		{
			get
			{
				return (Bot)base.ByName(name);
			}
		}

		public void AddBot(Bot aBot)
		{
			base.Add(aBot);
		}
		
		public void RemoveBot(Bot aBot)
		{
			base.Remove(aBot);
		}

		#endregion
	}
}
