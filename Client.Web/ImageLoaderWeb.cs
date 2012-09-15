// 
//  ImageLoaderWeb.cs
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
using System.IO;

namespace XG.Client.Web
{
	public class ImageLoaderWeb : ImageLoader
	{
		public static ImageLoaderWeb Instance
		{
			get { return Nested.Instance; }
		}
		class Nested
		{
			static Nested() { }
			internal static readonly ImageLoaderWeb Instance = new ImageLoaderWeb();
		}

		ImageLoaderWeb()
		{
			pbClient = LoadImage(base.Client);

			pbServer = LoadImage(base.Server);
			pbServerDisabled = LoadImage(base.ServerDisabled);

			pbChannel = LoadImage(base.Channel);
			pbChannelDisabled = LoadImage(base.ChannelDisabled);

			pbBot = LoadImage(base.Bot);
			pbBotOff = LoadImage(base.BotOff);

			pbPacket = LoadImage(base.Packet);
			pbPacketDisabled = LoadImage(base.PacketDisabled);

			pbBlind = LoadImage(base.Blind);
			pbSearch = LoadImage(base.Search);
			pbSearchSlots = LoadImage(base.SearchSlots);
			pbODay = LoadImage(base.ODay);
			pbOWeek = LoadImage(base.OWeek);

			pbAdd = LoadImage(base.Add);
			pbRemove = LoadImage(base.Remove);
			pbConnect = LoadImage(base.Connect);
			pbDisconnect = LoadImage(base.Disconnect);
			pbOk = LoadImage(base.Ok);
			pbNo = LoadImage(base.No);

			pbLanguageDe = LoadImage(base.LanguageDe);

			pbExtAudio = LoadImage(base.ExtAudio);
			pbExtCompressed = LoadImage(base.ExtCompressed);
			pbExtDefault = LoadImage(base.ExtDefault);
			pbExtVideo = LoadImage(base.ExtVideo);
			pbExtAudio2 = LoadImage(base.ExtAudio2);
			pbExtCompressed2 = LoadImage(base.ExtCompressed2);
			pbExtDefault2 = LoadImage(base.ExtDefault2);
			pbExtVideo2 = LoadImage(base.ExtVideo2);
			
			pbOverActive = LoadImage(base.OverActive);
			pbOverAttention = LoadImage(base.OverAttention);
			pbOverChecked0 = LoadImage(base.OverChecked0);
			pbOverChecked1 = LoadImage(base.OverChecked1);
			pbOverDisabled = LoadImage(base.OverDisabled);
			pbOverDL0 = LoadImage(base.OverDL0);
			pbOverDL1 = LoadImage(base.OverDL1);
			pbOverDL2 = LoadImage(base.OverDL2);
			pbOverDL3 = LoadImage(base.OverDL3);
			pbOverDL4 = LoadImage(base.OverDL4);
			pbOverDL5 = LoadImage(base.OverDL5);
			pbOverDL6 = LoadImage(base.OverDL6);
			pbOverDL7 = LoadImage(base.OverDL7);
			pbOverDL8 = LoadImage(base.OverDL8);
			pbOverDL9 = LoadImage(base.OverDL9);
			pbOverWaiting = LoadImage(base.OverWaiting);
		}

		byte[] LoadImage(Stream aStream)
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

		public byte[] Image(string aName)
		{
			switch (aName)
			{
				case "Client":
					return pbClient;

				case "Server":
					return pbServer;

				case "ServerDisabled":
					return pbServerDisabled;

				case "Channel":
					return pbChannel;

				case "ChannelDisabled":
					return pbChannelDisabled;

				case "Bot":
					return pbBot;

				case "BotOff":
					return pbBotOff;

				case "Packet":
					return pbPacket;

				case "PacketDisabled":
					return pbPacketDisabled;

				case "Blind":
					return pbBlind;

				case "Search":
					return pbSearch;

				case "SearchSlots":
					return pbSearchSlots;

				case "ODay":
					return pbODay;

				case "OWeek":
					return pbOWeek;

				case "Add":
					return pbAdd;

				case "Remove":
					return pbRemove;

				case "Connect":
					return pbConnect;

				case "Disconnect":
					return pbDisconnect;

				case "Ok":
					return pbOk;

				case "No":
					return pbNo;

				case "LanguageDe":
					return pbLanguageDe;

				case "ExtAudio":
					return pbExtAudio;

				case "ExtCompressed":
					return pbExtCompressed;

				case "ExtDefault":
					return pbExtDefault;

				case "ExtVideo":
					return pbExtVideo;

				case "ExtAudio2":
					return pbExtAudio2;

				case "ExtCompressed2":
					return pbExtCompressed2;

				case "ExtDefault2":
					return pbExtDefault2;

				case "ExtVideo2":
					return pbExtVideo2;

				case "OverActive":
					return pbOverActive;

				case "OverAttention":
					return pbOverAttention;

				case "OverChecked0":
					return pbOverChecked0;

				case "OverChecked1":
					return pbOverChecked1;

				case "OverDisabled":
					return pbOverDisabled;

				case "OverDL0":
					return pbOverDL0;

				case "OverDL1":
					return pbOverDL1;

				case "OverDL2":
					return pbOverDL2;

				case "OverDL3":
					return pbOverDL3;

				case "OverDL4":
					return pbOverDL4;

				case "OverDL5":
					return pbOverDL5;

				case "OverDL6":
					return pbOverDL6;

				case "OverDL7":
					return pbOverDL7;

				case "OverDL8":
					return pbOverDL8;

				case "OverDL9":
					return pbOverDL9;

				case "OverWaiting":
					return pbOverWaiting;
			}
			return null;
		}

		byte[] pbClient;

		byte[] pbServer;
		byte[] pbServerDisabled;

		byte[] pbChannel;
		byte[] pbChannelDisabled;

		byte[] pbBot;
		byte[] pbBotOff;

		byte[] pbPacket;
		byte[] pbPacketDisabled;

		byte[] pbBlind;
		byte[] pbSearch;
		byte[] pbSearchSlots;
		byte[] pbODay;
		byte[] pbOWeek;

		byte[] pbAdd;
		byte[] pbRemove;
		byte[] pbConnect;
		byte[] pbDisconnect;
		byte[] pbOk;
		byte[] pbNo;

		byte[] pbLanguageDe;

		byte[] pbExtAudio;
		byte[] pbExtCompressed;
		byte[] pbExtDefault;
		byte[] pbExtVideo;
		byte[] pbExtAudio2;
		byte[] pbExtCompressed2;
		byte[] pbExtDefault2;
		byte[] pbExtVideo2;

		byte[] pbOverActive;
		byte[] pbOverAttention;
		byte[] pbOverChecked0;
		byte[] pbOverChecked1;
		byte[] pbOverDisabled;
		byte[] pbOverDL0;
		byte[] pbOverDL1;
		byte[] pbOverDL2;
		byte[] pbOverDL3;
		byte[] pbOverDL4;
		byte[] pbOverDL5;
		byte[] pbOverDL6;
		byte[] pbOverDL7;
		byte[] pbOverDL8;
		byte[] pbOverDL9;
		byte[] pbOverWaiting;
	}
}
