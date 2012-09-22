// 
//  ImageLoader.cs
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

using System.IO;
using System.Reflection;

namespace XG.Client
{
	public class ImageLoader
	{
		protected ImageLoader()
		{
			Assembly assembly = Assembly.GetAssembly(typeof(ImageLoader));
            string name = "XG." + assembly.GetName().Name;

			Client = assembly.GetManifestResourceStream(name + ".Resources.client.png");

			Server = assembly.GetManifestResourceStream(name + ".Resources.Server.png");
			ServerDisabled = assembly.GetManifestResourceStream(name + ".Resources.Server_disabled.png");

			Channel = assembly.GetManifestResourceStream(name + ".Resources.Channel.png");
			ChannelDisabled = assembly.GetManifestResourceStream(name + ".Resources.Channel_disabled.png");

			Bot = assembly.GetManifestResourceStream(name + ".Resources.Bot.png");
			BotOff = assembly.GetManifestResourceStream(name + ".Resources.Bot_offline.png");

			Packet = assembly.GetManifestResourceStream(name + ".Resources.Packet.png");
			PacketDisabled = assembly.GetManifestResourceStream(name + ".Resources.Packet_disabled.png");

			Blind = assembly.GetManifestResourceStream(name + ".Resources.Blind.png");
			Search = assembly.GetManifestResourceStream(name + ".Resources.Search.png");
			SearchSlots = assembly.GetManifestResourceStream(name + ".Resources.Search_freeslots.png");
			ODay = assembly.GetManifestResourceStream(name + ".Resources.ODay.png");
			OWeek = assembly.GetManifestResourceStream(name + ".Resources.OWeek.png");

			Add = assembly.GetManifestResourceStream(name + ".Resources.Add.png");
			Remove = assembly.GetManifestResourceStream(name + ".Resources.Remove.png");
			Connect = assembly.GetManifestResourceStream(name + ".Resources.Connect.png");
			Disconnect = assembly.GetManifestResourceStream(name + ".Resources.Disconnect.png");
			Ok = assembly.GetManifestResourceStream(name + ".Resources.Ok.png");
			No = assembly.GetManifestResourceStream(name + ".Resources.No.png");

			LanguageDe = assembly.GetManifestResourceStream(name + ".Resources.language.de.png");

			ExtAudio = assembly.GetManifestResourceStream(name + ".Resources.extension.audio.png");
			ExtCompressed = assembly.GetManifestResourceStream(name + ".Resources.extension.compressed.png");
			ExtDefault = assembly.GetManifestResourceStream(name + ".Resources.extension.default.png");
			ExtVideo = assembly.GetManifestResourceStream(name + ".Resources.extension.video.png");
			ExtAudio2 = assembly.GetManifestResourceStream(name + ".Resources.extension.audio2.png");
			ExtCompressed2 = assembly.GetManifestResourceStream(name + ".Resources.extension.compressed2.png");
			ExtDefault2 = assembly.GetManifestResourceStream(name + ".Resources.extension.default2.png");
			ExtVideo2 = assembly.GetManifestResourceStream(name + ".Resources.extension.video2.png");
			
			OverActive = assembly.GetManifestResourceStream(name + ".Resources.Overlay._active.png");
			OverAttention = assembly.GetManifestResourceStream(name + ".Resources.Overlay._attention.png");
			OverChecked0 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._checked_0.png");
			OverChecked1 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._checked_1.png");
			OverDisabled = assembly.GetManifestResourceStream(name + ".Resources.Overlay._disabled.png");
			OverDL0 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl0.png");
			OverDL1 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl1.png");
			OverDL2 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl2.png");
			OverDL3 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl3.png");
			OverDL4 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl4.png");
			OverDL5 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl5.png");
			OverDL6 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl6.png");
			OverDL7 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl7.png");
			OverDL8 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl8.png");
			OverDL9 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl9.png");
			OverWaiting = assembly.GetManifestResourceStream(name + ".Resources.Overlay._waiting.png");
		}

		protected Stream Client;

		protected Stream Server;
		protected Stream ServerDisabled;

		protected Stream Channel;
		protected Stream ChannelDisabled;

		protected Stream Bot;
		protected Stream BotOff;

		protected Stream Packet;
		protected Stream PacketDisabled;

		protected Stream Blind;
		protected Stream Search;
		protected Stream SearchSlots;
		protected Stream ODay;
		protected Stream OWeek;

		protected Stream Add;
		protected Stream Remove;
		protected Stream Connect;
		protected Stream Disconnect;
		protected Stream Ok;
		protected Stream No;

		protected Stream LanguageDe;

		protected Stream ExtAudio;
		protected Stream ExtCompressed;
		protected Stream ExtDefault;
		protected Stream ExtVideo;
		protected Stream ExtAudio2;
		protected Stream ExtCompressed2;
		protected Stream ExtDefault2;
		protected Stream ExtVideo2;
			
		protected Stream OverActive;
		protected Stream OverAttention;
		protected Stream OverChecked0;
		protected Stream OverChecked1;
		protected Stream OverDisabled;
		protected Stream OverDL0;
		protected Stream OverDL1;
		protected Stream OverDL2;
		protected Stream OverDL3;
		protected Stream OverDL4;
		protected Stream OverDL5;
		protected Stream OverDL6;
		protected Stream OverDL7;
		protected Stream OverDL8;
		protected Stream OverDL9;
		protected Stream OverWaiting;
	}
}
