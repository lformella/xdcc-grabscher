// 
//  Channel.cs
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
using Db4objects.Db4o;

namespace XG.Model.Domain
{
	public class Channel : AObjects
	{
		#region VARIABLES

		public override bool Connected
		{
			get { return base.Connected; }
			set
			{
				if (!value)
				{
					foreach (AObject obj in Children)
					{
						obj.Connected = false;
						obj.Commit();
					}
				}
				base.Connected = value;
			}
		}

		public new Server Parent
		{
			get { return base.Parent as Server; }
			set { base.Parent = value; }
		}

		[Transient]
		int _errorCode;

		public int ErrorCode
		{
			get { return GetProperty(ref _errorCode); }
			set { SetProperty(ref _errorCode, value, "ErrorCode"); }
		}

		string _topic;

		public string Topic
		{
			get { return GetProperty(ref _topic); }
			set { SetProperty(ref _topic, value, "Topic"); }
		}

		int _userCount;

		public int UserCount
		{
			get { return GetProperty(ref _userCount); }
			set { SetProperty(ref _userCount, value, "UserCount"); }
		}

		bool _askForVersion;

		public bool AskForVersion
		{
			get { return GetProperty(ref _askForVersion); }
			set { SetProperty(ref _askForVersion, value, "AskForVersion"); }
		}

		string _messageAfterConnect;

		public string MessageAfterConnect
		{
			get { return GetProperty(ref _messageAfterConnect); }
			set { SetProperty(ref _messageAfterConnect, value, "MessageAfterConnect"); }
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Bot> Bots
		{
			get { return Children.Cast<Bot>(); }
		}

		public Bot Bot(string aName)
		{
			return base.Named(aName) as Bot;
		}

		public bool AddBot(Bot aBot)
		{
			return Add(aBot);
		}

		public bool RemoveBot(Bot aBot)
		{
			return Remove(aBot);
		}

		protected override bool DuplicateChildExists(AObject aObject)
		{
			return Bot((aObject as Bot).Name) != null;
		}

		#endregion
	}
}
