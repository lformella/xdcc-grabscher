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

		public XGBot this[string name]
		{
			get
			{
				name = name.ToLower();
				foreach (XGBot bot in base.Children)
				{
					//if (bot.Name.ToLower() == name) { return bot; }
					if (bot.Name.ToLower().CompareTo(name) == 0) { return bot; }
				}
				return null;
			}
		}

		public void addBot(XGBot aBot)
		{
			base.addChild(aBot);
		}
		public void removeBot(XGBot aBot)
		{
			base.removeChild(aBot);
		}

		public XGChannel()
			: base()
		{
		}
		public XGChannel(XGServer parent)
			: this()
		{
			this.Parent = parent;
			this.Parent.addChannel(this);
		}

		public void Clone(XGChannel aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
		}
	}
}
