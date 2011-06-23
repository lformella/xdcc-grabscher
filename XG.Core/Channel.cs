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

namespace XG.Core
{
	[Serializable()]
	public class XGChannel : XGObject
	{
		public new XGServer Parent
		{
			get { return base.Parent as XGServer; }
			set { base.Parent = value; }
		}

		public new bool Connected
		{
			get { return base.Connected; }
			set
			{
				if (!value)
				{
					foreach (XGObject tObj in base.Children)
					{
						tObj.Connected = value;
					}
				}
				base.Connected = value;
			}
		}

		private int errorCode = 0;
		public int ErrorCode
		{
			get { return this.errorCode; }
			set
			{
				if (this.errorCode != value)
				{
					this.errorCode = value;
					this.Modified = true;
				}
			}
		}

		public XGBot this[string name]
		{
			get
			{
				foreach (XGBot bot in base.Children)
				{
					if (bot.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) { return bot; }
				}
				return null;
			}
		}

		public void AddBot(XGBot aBot)
		{
			base.AddChild(aBot);
		}
		public void RemoveBot(XGBot aBot)
		{
			base.RemoveChild(aBot);
		}

		public XGChannel() : base()
		{
		}
		public XGChannel(XGServer parent) : this()
		{
			this.Parent = parent;
			this.Parent.AddChannel(this);
		}

		public void Clone(XGChannel aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
		}
	}
}
