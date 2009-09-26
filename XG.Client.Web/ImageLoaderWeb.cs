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
using System.IO;

namespace XG.Client.Web
{
	public class ImageLoaderWeb : ImageLoader
	{
		public static ImageLoaderWeb Instance
		{
			get { return Nested.instance; }
		}
		class Nested
		{
			static Nested() {}
			internal static readonly ImageLoaderWeb instance = new ImageLoaderWeb();
		}

		private ImageLoaderWeb()
		{
			this.pbClient = this.LoadImage(base.Client);

			this.pbServer = this.LoadImage(base.Server);
			this.pbServerConnected = this.LoadImage(base.ServerConnected);
			this.pbServerDisconnected = this.LoadImage(base.ServerDisconnected);

			this.pbChannel = this.LoadImage(base.Channel);
			this.pbChannelConnected = this.LoadImage(base.ChannelConnected);
			this.pbChannelDisconnected = this.LoadImage(base.ChannelDisconnected);

			this.pbBot = this.LoadImage(base.Bot);
			this.pbBotOff = this.LoadImage(base.BotOff);
			this.pbBotQueued = this.LoadImage(base.BotQueued);
			this.pbBotFree = this.LoadImage(base.BotFree);
			this.pbBotFull = this.LoadImage(base.BotFull);
			this.pbBotDL0 = this.LoadImage(base.BotDL0);
			this.pbBotDL1 = this.LoadImage(base.BotDL1);
			this.pbBotDL2 = this.LoadImage(base.BotDL2);
			this.pbBotDL3 = this.LoadImage(base.BotDL3);
			this.pbBotDL4 = this.LoadImage(base.BotDL4);
			this.pbBotDL5 = this.LoadImage(base.BotDL5);

			this.pbPacket = this.LoadImage(base.Packet);
			this.pbPacketDisabled = this.LoadImage(base.PacketDisabled);
			this.pbPacketQueued = this.LoadImage(base.PacketQueued);
			this.pbPacketBroken = this.LoadImage(base.PacketBroken);
			this.pbPacketReady0 = this.LoadImage(base.PacketReady0);
			this.pbPacketReady1 = this.LoadImage(base.PacketReady1);
			this.pbPacketNew = this.LoadImage(base.PacketNew);
			this.pbPacketDL0 = this.LoadImage(base.PacketDL0);
			this.pbPacketDL1 = this.LoadImage(base.PacketDL1);
			this.pbPacketDL2 = this.LoadImage(base.PacketDL2);
			this.pbPacketDL3 = this.LoadImage(base.PacketDL3);
			this.pbPacketDL4 = this.LoadImage(base.PacketDL4);
			this.pbPacketDL5 = this.LoadImage(base.PacketDL5);

			this.pbBlind = this.LoadImage(base.Blind);
			this.pbSearch = this.LoadImage(base.Search);
			this.pbSearchSlots = this.LoadImage(base.SearchSlots);
			this.pbODay = this.LoadImage(base.ODay);
			this.pbOWeek = this.LoadImage(base.OWeek);
			
			this.pbAdd = this.LoadImage(base.Add);
			this.pbRemove = this.LoadImage(base.Remove);
			this.pbConnect = this.LoadImage(base.Connect);
			this.pbDisconnect = this.LoadImage(base.Disconnect);
			this.pbOk = this.LoadImage(base.Ok);
			this.pbNo = this.LoadImage(base.No);
		}

		private byte[] LoadImage(Stream aStream)
		{
			byte[] data = new byte[aStream.Length];
			int offset = 0;
			int remaining = data.Length;
			while (remaining > 0)
			{
				int read = aStream.Read(data, offset, remaining);
				if (read <= 0)
					throw new EndOfStreamException (String.Format("End of stream reached with {0} bytes left to read", remaining));
				remaining -= read;
				offset += read;
			}
			return data;
		}
		
		public byte[] GetImage(string aName)
		{
			switch(aName)
			{
				case "Client":
					return this.pbClient;

				case "Server":
					return this.pbServer;

				case "ServerConnected":
					return this.pbServerConnected;

				case "ServerDisconnected":
					return this.pbServerDisconnected;					

				case "Channel":
					return this.pbChannel;

				case "ChannelConnected":
					return this.pbChannelConnected;

				case "ChannelDisconnected":
					return this.pbChannelDisconnected;					

				case "Bot":
					return this.pbBot;

				case "BotOff":
					return this.pbBotOff;

				case "BotQueued":
					return this.pbBotQueued;

				case "BotFree":
					return this.pbBotFree;

				case "BotFull":
					return this.pbBotFull;

				case "BotDL0":
					return this.pbBotDL0;

				case "BotDL1":
					return this.pbBotDL1;

				case "BotDL2":
					return this.pbBotDL2;

				case "BotDL3":
					return this.pbBotDL3;

				case "BotDL4":
					return this.pbBotDL4;

				case "BotDL5":
					return this.pbBotDL5;					

				case "Packet":
					return this.pbPacket;

				case "PacketDisabled":
					return this.pbPacketDisabled;

				case "PacketQueued":
					return this.pbPacketQueued;

				case "PacketBroken":
					return this.pbPacketBroken;

				case "PacketReady0":
					return this.pbPacketReady0;

				case "PacketReady1":
					return this.pbPacketReady1;

				case "PacketNew":
					return this.pbPacketNew;

				case "":
					return this.pbPacketDL0;

				case "PacketDL1":
					return this.pbPacketDL1;

				case "PacketDL2":
					return this.pbPacketDL2;

				case "PacketDL3":
					return this.pbPacketDL3;

				case "PacketDL4":
					return this.pbPacketDL4;

				case "PacketDL5":
					return this.pbPacketDL5;					

				case "Blind":
					return this.pbBlind;

				case "Search":
					return this.pbSearch;

				case "SearchSlots":
					return this.pbSearchSlots;

				case "ODay":
					return this.pbODay;

				case "OWeek":
					return this.pbOWeek;					

				case "Add":
					return this.pbAdd;

				case "Remove":
					return this.pbRemove;

				case "Connect":
					return this.pbConnect;

				case "Disconnect":
					return this.pbDisconnect;

				case "Ok":
					return this.pbOk;

				case "No":
					return this.pbNo;
			}
			return null;
		}

		private byte[] pbClient;

		private byte[] pbServer;
		private byte[] pbServerConnected;
		private byte[] pbServerDisconnected;

		private byte[] pbChannel;
		private byte[] pbChannelConnected;
		private byte[] pbChannelDisconnected;

		private byte[] pbBot;
		private byte[] pbBotOff;
		private byte[] pbBotQueued;
		private byte[] pbBotFree;
		private byte[] pbBotFull;
		private byte[] pbBotDL0;
		private byte[] pbBotDL1;
		private byte[] pbBotDL2;
		private byte[] pbBotDL3;
		private byte[] pbBotDL4;
		private byte[] pbBotDL5;

		private byte[] pbPacket;
		private byte[] pbPacketDisabled;
		private byte[] pbPacketQueued;
		private byte[] pbPacketBroken;
		private byte[] pbPacketReady0;
		private byte[] pbPacketReady1;
		private byte[] pbPacketNew;
		private byte[] pbPacketDL0;
		private byte[] pbPacketDL1;
		private byte[] pbPacketDL2;
		private byte[] pbPacketDL3;
		private byte[] pbPacketDL4;
		private byte[] pbPacketDL5;

		private byte[] pbBlind;
		private byte[] pbSearch;
		private byte[] pbSearchSlots;
		private byte[] pbODay;
		private byte[] pbOWeek;

		private byte[] pbAdd;
		private byte[] pbRemove;
		private byte[] pbConnect;
		private byte[] pbDisconnect;
		private byte[] pbOk;
		private byte[] pbNo;
	}
}
