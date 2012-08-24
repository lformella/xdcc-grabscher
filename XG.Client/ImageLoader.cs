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

using System.IO;
using System.Reflection;

namespace XG.Client
{
	public class ImageLoader
	{
		protected ImageLoader()
		{
			Assembly assembly = Assembly.GetAssembly(typeof(ImageLoader));
			string name = assembly.GetName().Name;

			this.Client = assembly.GetManifestResourceStream(name + ".Resources.client.png");

			this.Server = assembly.GetManifestResourceStream(name + ".Resources.Server.png");
			this.ServerDisabled = assembly.GetManifestResourceStream(name + ".Resources.Server_disabled.png");

			this.Channel = assembly.GetManifestResourceStream(name + ".Resources.Channel.png");
			this.ChannelDisabled = assembly.GetManifestResourceStream(name + ".Resources.Channel_disabled.png");

			this.Bot = assembly.GetManifestResourceStream(name + ".Resources.Bot.png");
			this.BotOff = assembly.GetManifestResourceStream(name + ".Resources.Bot_offline.png");

			this.Packet = assembly.GetManifestResourceStream(name + ".Resources.Packet.png");
			this.PacketDisabled = assembly.GetManifestResourceStream(name + ".Resources.Packet_disabled.png");

			this.Blind = assembly.GetManifestResourceStream(name + ".Resources.Blind.png");
			this.Search = assembly.GetManifestResourceStream(name + ".Resources.Search.png");
			this.SearchSlots = assembly.GetManifestResourceStream(name + ".Resources.Search_freeslots.png");
			this.ODay = assembly.GetManifestResourceStream(name + ".Resources.ODay.png");
			this.OWeek = assembly.GetManifestResourceStream(name + ".Resources.OWeek.png");

			this.Add = assembly.GetManifestResourceStream(name + ".Resources.Add.png");
			this.Remove = assembly.GetManifestResourceStream(name + ".Resources.Remove.png");
			this.Connect = assembly.GetManifestResourceStream(name + ".Resources.Connect.png");
			this.Disconnect = assembly.GetManifestResourceStream(name + ".Resources.Disconnect.png");
			this.Ok = assembly.GetManifestResourceStream(name + ".Resources.Ok.png");
			this.No = assembly.GetManifestResourceStream(name + ".Resources.No.png");

			this.LanguageDe = assembly.GetManifestResourceStream(name + ".Resources.language.de.png");

			this.ExtAudio = assembly.GetManifestResourceStream(name + ".Resources.extension.audio.png");
			this.ExtCompressed = assembly.GetManifestResourceStream(name + ".Resources.extension.compressed.png");
			this.ExtDefault = assembly.GetManifestResourceStream(name + ".Resources.extension.default.png");
			this.ExtVideo = assembly.GetManifestResourceStream(name + ".Resources.extension.video.png");
			
			this.OverActive = assembly.GetManifestResourceStream(name + ".Resources.Overlay._active.png");
			this.OverAttention = assembly.GetManifestResourceStream(name + ".Resources.Overlay._attention.png");
			this.OverChecked0 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._checked_0.png");
			this.OverChecked1 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._checked_1.png");
			this.OverDisabled = assembly.GetManifestResourceStream(name + ".Resources.Overlay._disabled.png");
			this.OverDL0 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl0.png");
			this.OverDL1 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl1.png");
			this.OverDL2 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl2.png");
			this.OverDL3 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl3.png");
			this.OverDL4 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl4.png");
			this.OverDL5 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl5.png");
			this.OverDL6 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl6.png");
			this.OverDL7 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl7.png");
			this.OverDL8 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl8.png");
			this.OverDL9 = assembly.GetManifestResourceStream(name + ".Resources.Overlay._dl9.png");
			this.OverWaiting = assembly.GetManifestResourceStream(name + ".Resources.Overlay._waiting.png");
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
