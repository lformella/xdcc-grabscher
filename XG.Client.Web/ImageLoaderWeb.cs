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
			static Nested() { }
			internal static readonly ImageLoaderWeb instance = new ImageLoaderWeb();
		}

		private ImageLoaderWeb()
		{
			this.pbClient = this.LoadImage(base.Client);

			this.pbServer = this.LoadImage(base.Server);
			this.pbServerDisabled = this.LoadImage(base.ServerDisabled);

			this.pbChannel = this.LoadImage(base.Channel);
			this.pbChannelDisabled = this.LoadImage(base.ChannelDisabled);

			this.pbBot = this.LoadImage(base.Bot);
			this.pbBotOff = this.LoadImage(base.BotOff);

			this.pbPacket = this.LoadImage(base.Packet);
			this.pbPacketDisabled = this.LoadImage(base.PacketDisabled);

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

			this.pbLanguageDe = this.LoadImage(base.LanguageDe);

			this.pbExtAudio = this.LoadImage(base.ExtAudio);
			this.pbExtCompressed = this.LoadImage(base.ExtCompressed);
			this.pbExtDefault = this.LoadImage(base.ExtDefault);
			this.pbExtVideo = this.LoadImage(base.ExtVideo);
			
			this.pbOverActive = this.LoadImage(base.OverActive);
			this.pbOverAttention = this.LoadImage(base.OverAttention);
			this.pbOverChecked0 = this.LoadImage(base.OverChecked0);
			this.pbOverChecked1 = this.LoadImage(base.OverChecked1);
			this.pbOverDisabled = this.LoadImage(base.OverDisabled);
			this.pbOverDL0 = this.LoadImage(base.OverDL0);
			this.pbOverDL1 = this.LoadImage(base.OverDL1);
			this.pbOverDL2 = this.LoadImage(base.OverDL2);
			this.pbOverDL3 = this.LoadImage(base.OverDL3);
			this.pbOverDL4 = this.LoadImage(base.OverDL4);
			this.pbOverDL5 = this.LoadImage(base.OverDL5);
			this.pbOverDL6 = this.LoadImage(base.OverDL6);
			this.pbOverDL7 = this.LoadImage(base.OverDL7);
			this.pbOverDL8 = this.LoadImage(base.OverDL8);
			this.pbOverDL9 = this.LoadImage(base.OverDL9);
			this.pbOverWaiting = this.LoadImage(base.OverWaiting);
		}

		private byte[] LoadImage(Stream aStream)
		{
			byte[] data = new byte[aStream.Length];
			int offset = 0;
			int remaining = data.Length;
			while (remaining > 0)
			{
				int read = aStream.Read(data, offset, remaining);
				if (read <= 0) { throw new EndOfStreamException(String.Format("End of stream reached with {0} bytes left to read", remaining)); }
				remaining -= read;
				offset += read;
			}
			return data;
		}

		public byte[] GetImage(string aName)
		{
			switch (aName)
			{
				case "Client":
					return this.pbClient;

				case "Server":
					return this.pbServer;

				case "ServerDisabled":
					return this.pbServerDisabled;

				case "Channel":
					return this.pbChannel;

				case "ChannelDisabled":
					return this.pbChannelDisabled;

				case "Bot":
					return this.pbBot;

				case "BotOff":
					return this.pbBotOff;

				case "Packet":
					return this.pbPacket;

				case "PacketDisabled":
					return this.pbPacketDisabled;

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

				case "LanguageDe":
					return this.pbLanguageDe;

				case "ExtAudio":
					return this.pbExtAudio;

				case "ExtCompressed":
					return this.pbExtCompressed;

				case "ExtDefault":
					return this.pbExtDefault;

				case "ExtVideo":
					return this.pbExtVideo;

				case "OverActive":
					return this.pbOverActive;

				case "OverAttention":
					return this.pbOverAttention;

				case "OverChecked0":
					return this.pbOverChecked0;

				case "OverChecked1":
					return this.pbOverChecked1;

				case "OverDisabled":
					return this.pbOverDisabled;

				case "OverDL0":
					return this.pbOverDL0;

				case "OverDL1":
					return this.pbOverDL1;

				case "OverDL2":
					return this.pbOverDL2;

				case "OverDL3":
					return this.pbOverDL3;

				case "OverDL4":
					return this.pbOverDL4;

				case "OverDL5":
					return this.pbOverDL5;

				case "OverDL6":
					return this.pbOverDL6;

				case "OverDL7":
					return this.pbOverDL7;

				case "OverDL8":
					return this.pbOverDL8;

				case "OverDL9":
					return this.pbOverDL9;

				case "OverWaiting":
					return this.pbOverWaiting;
			}
			return null;
		}

		private byte[] pbClient;

		private byte[] pbServer;
		private byte[] pbServerDisabled;

		private byte[] pbChannel;
		private byte[] pbChannelDisabled;

		private byte[] pbBot;
		private byte[] pbBotOff;

		private byte[] pbPacket;
		private byte[] pbPacketDisabled;

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

		private byte[] pbLanguageDe;

		private byte[] pbExtAudio;
		private byte[] pbExtCompressed;
		private byte[] pbExtDefault;
		private byte[] pbExtVideo;

		private byte[] pbOverActive;
		private byte[] pbOverAttention;
		private byte[] pbOverChecked0;
		private byte[] pbOverChecked1;
		private byte[] pbOverDisabled;
		private byte[] pbOverDL0;
		private byte[] pbOverDL1;
		private byte[] pbOverDL2;
		private byte[] pbOverDL3;
		private byte[] pbOverDL4;
		private byte[] pbOverDL5;
		private byte[] pbOverDL6;
		private byte[] pbOverDL7;
		private byte[] pbOverDL8;
		private byte[] pbOverDL9;
		private byte[] pbOverWaiting;
	}
}
