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

namespace XG.Client.GTK
{
	public class ImageLoaderGTK : ImageLoader
	{
		public static ImageLoaderGTK Instance
		{
			get { return Nested.instance; }
		}
		class Nested
		{
			static Nested() {}
			internal static readonly ImageLoaderGTK instance = new ImageLoaderGTK();
		}

		private ImageLoaderGTK()
		{
			this.pbClient = new Gdk.Pixbuf(base.Client);

			this.pbServer = new Gdk.Pixbuf(base.Server);
			this.pbServerConnected = new Gdk.Pixbuf(base.ServerConnected);
			this.pbServerDisabled = new Gdk.Pixbuf(base.ServerDisabled);

			this.pbChannel = new Gdk.Pixbuf(base.Channel);
			this.pbChannelConnected = new Gdk.Pixbuf(base.ChannelConnected);
			this.pbChannelDisabled = new Gdk.Pixbuf(base.ChannelDisabled);

			this.pbBot = new Gdk.Pixbuf(base.Bot);
			this.pbBotOff = new Gdk.Pixbuf(base.BotOff);
			this.pbBotQueued = new Gdk.Pixbuf(base.BotQueued);
			this.pbBotFree = new Gdk.Pixbuf(base.BotFree);
			this.pbBotFull = new Gdk.Pixbuf(base.BotFull);
			this.pbBotDL0 = new Gdk.Pixbuf(base.BotDL0);
			this.pbBotDL1 = new Gdk.Pixbuf(base.BotDL1);
			this.pbBotDL2 = new Gdk.Pixbuf(base.BotDL2);
			this.pbBotDL3 = new Gdk.Pixbuf(base.BotDL3);
			this.pbBotDL4 = new Gdk.Pixbuf(base.BotDL4);
			this.pbBotDL5 = new Gdk.Pixbuf(base.BotDL5);

			this.pbPacket = new Gdk.Pixbuf(base.Packet);
			this.pbPacketDisabled = new Gdk.Pixbuf(base.PacketDisabled);
			this.pbPacketQueued = new Gdk.Pixbuf(base.PacketQueued);
			this.pbPacketBroken = new Gdk.Pixbuf(base.PacketBroken);
			this.pbPacketReady0 = new Gdk.Pixbuf(base.PacketReady0);
			this.pbPacketReady1 = new Gdk.Pixbuf(base.PacketReady1);
			this.pbPacketNew = new Gdk.Pixbuf(base.PacketNew);
			this.pbPacketDL0 = new Gdk.Pixbuf(base.PacketDL0);
			this.pbPacketDL1 = new Gdk.Pixbuf(base.PacketDL1);
			this.pbPacketDL2 = new Gdk.Pixbuf(base.PacketDL2);
			this.pbPacketDL3 = new Gdk.Pixbuf(base.PacketDL3);
			this.pbPacketDL4 = new Gdk.Pixbuf(base.PacketDL4);
			this.pbPacketDL5 = new Gdk.Pixbuf(base.PacketDL5);

			this.pbBlind = new Gdk.Pixbuf(base.Blind);
			this.pbSearch = new Gdk.Pixbuf(base.Search);
			this.pbSearchSlots = new Gdk.Pixbuf(base.SearchSlots);
			this.pbODay = new Gdk.Pixbuf(base.ODay);
			this.pbOWeek = new Gdk.Pixbuf(base.OWeek);
			
			this.pbAdd = new Gdk.Pixbuf(base.Add);
			this.pbRemove = new Gdk.Pixbuf(base.Remove);
			this.pbConnect = new Gdk.Pixbuf(base.Connect);
			this.pbDisconnect = new Gdk.Pixbuf(base.Disconnect);
			this.pbOk = new Gdk.Pixbuf(base.Ok);
			this.pbNo = new Gdk.Pixbuf(base.No);
		}

		public readonly Gdk.Pixbuf pbClient;

		public readonly Gdk.Pixbuf pbServer;
		public readonly Gdk.Pixbuf pbServerConnected;
		public readonly Gdk.Pixbuf pbServerDisabled;

		public readonly Gdk.Pixbuf pbChannel;
		public readonly Gdk.Pixbuf pbChannelConnected;
		public readonly Gdk.Pixbuf pbChannelDisabled;

		public readonly Gdk.Pixbuf pbBot;
		public readonly Gdk.Pixbuf pbBotOff;
		public readonly Gdk.Pixbuf pbBotQueued;
		public readonly Gdk.Pixbuf pbBotFree;
		public readonly Gdk.Pixbuf pbBotFull;
		public readonly Gdk.Pixbuf pbBotDL0;
		public readonly Gdk.Pixbuf pbBotDL1;
		public readonly Gdk.Pixbuf pbBotDL2;
		public readonly Gdk.Pixbuf pbBotDL3;
		public readonly Gdk.Pixbuf pbBotDL4;
		public readonly Gdk.Pixbuf pbBotDL5;

		public readonly Gdk.Pixbuf pbPacket;
		public readonly Gdk.Pixbuf pbPacketDisabled;
		public readonly Gdk.Pixbuf pbPacketQueued;
		public readonly Gdk.Pixbuf pbPacketBroken;
		public readonly Gdk.Pixbuf pbPacketReady0;
		public readonly Gdk.Pixbuf pbPacketReady1;
		public readonly Gdk.Pixbuf pbPacketNew;
		public readonly Gdk.Pixbuf pbPacketDL0;
		public readonly Gdk.Pixbuf pbPacketDL1;
		public readonly Gdk.Pixbuf pbPacketDL2;
		public readonly Gdk.Pixbuf pbPacketDL3;
		public readonly Gdk.Pixbuf pbPacketDL4;
		public readonly Gdk.Pixbuf pbPacketDL5;

		public readonly Gdk.Pixbuf pbBlind;
		public readonly Gdk.Pixbuf pbSearch;
		public readonly Gdk.Pixbuf pbSearchSlots;
		public readonly Gdk.Pixbuf pbODay;
		public readonly Gdk.Pixbuf pbOWeek;

		public readonly Gdk.Pixbuf pbAdd;
		public readonly Gdk.Pixbuf pbRemove;
		public readonly Gdk.Pixbuf pbConnect;
		public readonly Gdk.Pixbuf pbDisconnect;
		public readonly Gdk.Pixbuf pbOk;
		public readonly Gdk.Pixbuf pbNo;
	}
}
