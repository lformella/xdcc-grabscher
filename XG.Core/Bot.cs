using System;
using System.Net;

namespace XG.Core
{
	[Serializable()]
	public class XGBot : XGObject
	{
		public new XGChannel Parent
		{
			get { return base.Parent as XGChannel; }
			set { base.Parent = value; }
		}

		private BotState botState;
		public BotState BotState
		{
			get { return botState; }
			set
			{
				if (botState != value)
				{
					botState = value;
					this.Modified = true;
				}
			}
		}

		[field: NonSerialized()]
		private IPAddress ip = IPAddress.None;
		public IPAddress IP
		{
			get { return ip; }
			set
			{
				if (ip != value)
				{
					ip = value;
					this.Modified = true;
				}
			}
		}

		private string lastMessage = "";
		public string LastMessage
		{
			get { return lastMessage; }
			set
			{
				if (lastMessage != value)
				{
					lastMessage = value;
					this.lastContact = DateTime.Now;
					this.Modified = true;
				}
			}
		}

		private DateTime lastContact = new DateTime(1, 1, 1, 0, 0, 0, 0);
		public DateTime LastContact
		{
			get { return lastContact; }
			set
			{
				if (lastContact != value)
				{
					lastContact = value;
					this.Modified = true;
				}
			}
		}

		private int queuePosition = 0;
		public int QueuePosition
		{
			get { return queuePosition; }
			set
			{
				if (queuePosition != value)
				{
					queuePosition = value;
					this.Modified = true;
				}
			}
		}

		private int queueTime = 0;
		public int QueueTime
		{
			get { return queueTime; }
			set
			{
				if (queueTime != value)
				{
					queueTime = value;
					this.Modified = true;
				}
			}
		}

		private double infoSpeedMax = 0;
		public double InfoSpeedMax
		{
			get { return infoSpeedMax; }
			set
			{
				if (infoSpeedMax != value)
				{
					infoSpeedMax = value;
					this.Modified = true;
				}
			}
		}
		private double infoSpeedCurrent = 0;
		public double InfoSpeedCurrent
		{
			get { return infoSpeedCurrent; }
			set
			{
				if (infoSpeedCurrent != value)
				{
					infoSpeedCurrent = value;
					this.Modified = true;
				}
			}
		}

		private int infoSlotTotal = 0;
		public int InfoSlotTotal
		{
			get { return infoSlotTotal; }
			set
			{
				if (infoSlotTotal != value)
				{
					infoSlotTotal = value;
					this.Modified = true;
				}
			}
		}
		private int infoSlotCurrent = 0;
		public int InfoSlotCurrent
		{
			get { return infoSlotCurrent; }
			set
			{
				if (infoSlotCurrent != value)
				{
					infoSlotCurrent = value;
					this.Modified = true;
				}
			}
		}

		private int infoQueueTotal = 0;
		public int InfoQueueTotal
		{
			get { return infoQueueTotal; }
			set
			{
				if (infoQueueTotal != value)
				{
					infoQueueTotal = value;
					this.Modified = true;
				}
			}
		}
		private int infoQueueCurrent = 0;
		public int InfoQueueCurrent
		{
			get { return infoQueueCurrent; }
			set
			{
				if (infoQueueCurrent != value)
				{
					infoQueueCurrent = value;
					this.Modified = true;
				}
			}
		}

		public XGPacket this[int id]
		{
			get
			{
				foreach (XGPacket pack in base.Children)
				{
					if (pack.Id == id) { return pack; }
				}
				return null;
			}
		}

		public void addPacket(XGPacket aPacket)
		{
			this.addChild(aPacket);
		}
		public void removePacket(XGPacket aPacket)
		{
			this.removeChild(aPacket);
		}

		public XGPacket getOldestActivePacket()
		{
			return this.getOldestActivePacket(false);
		}
		public XGPacket getOldestActivePacket(bool aAll)
		{
			XGPacket returnPacket = null;
			if (Children.Length > 0)
			{
				foreach (XGPacket tPack in base.Children)
				{
					if (aAll || tPack.Enabled)
					{
						if (returnPacket == null)
						{
							returnPacket = tPack;
						}
						else if (tPack.LastModified < returnPacket.LastModified)
						{
							returnPacket = tPack;
						}
					}
				}
			}
			return returnPacket;
		}

		public XGBot()
			: base()
		{
			this.botState = BotState.Idle;
		}
		public XGBot(XGChannel parent)
			: this()
		{
			this.Parent = parent;
			this.Parent.addBot(this);
		}

		public void Clone(XGBot aCopy, bool aFull)
		{
			base.Clone(aCopy, aFull);
			this.infoQueueCurrent = aCopy.infoQueueCurrent;
			this.infoQueueTotal = aCopy.infoQueueTotal;
			this.infoSlotCurrent = aCopy.infoSlotCurrent;
			this.infoSlotTotal = aCopy.infoSlotTotal;
			this.infoSpeedCurrent = aCopy.infoSpeedCurrent;
			this.infoSpeedMax = aCopy.infoSpeedMax;
			this.lastContact = aCopy.lastContact;
			if (aFull)
			{
				this.botState = aCopy.botState;
				this.lastMessage = aCopy.lastMessage;
				this.queuePosition = aCopy.queuePosition;
				this.queueTime = aCopy.queueTime;
			}
		}
	}
}
