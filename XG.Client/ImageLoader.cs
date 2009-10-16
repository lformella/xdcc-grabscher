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
			this.ServerConnected = assembly.GetManifestResourceStream(name + ".Resources.Server_connected.png");
			this.ServerDisabled = assembly.GetManifestResourceStream(name + ".Resources.Server_disabled.png");

			this.Channel = assembly.GetManifestResourceStream(name + ".Resources.Channel.png");
			this.ChannelConnected = assembly.GetManifestResourceStream(name + ".Resources.Channel_connected.png");
			this.ChannelDisabled = assembly.GetManifestResourceStream(name + ".Resources.Channel_disabled.png");

			this.Bot = assembly.GetManifestResourceStream(name + ".Resources.Bot.png");
			this.BotOff = assembly.GetManifestResourceStream(name + ".Resources.Bot_offline.png");
			this.BotQueued = assembly.GetManifestResourceStream(name + ".Resources.Bot_queued.png");
			this.BotFree = assembly.GetManifestResourceStream(name + ".Resources.Bot_slotsfree.png");
			this.BotFull = assembly.GetManifestResourceStream(name + ".Resources.Bot_slotsfull.png");
			this.BotDL0 = assembly.GetManifestResourceStream(name + ".Resources.Bot_dl0.png");
			this.BotDL1 = assembly.GetManifestResourceStream(name + ".Resources.Bot_dl1.png");
			this.BotDL2 = assembly.GetManifestResourceStream(name + ".Resources.Bot_dl2.png");
			this.BotDL3 = assembly.GetManifestResourceStream(name + ".Resources.Bot_dl3.png");
			this.BotDL4 = assembly.GetManifestResourceStream(name + ".Resources.Bot_dl4.png");
			this.BotDL5 = assembly.GetManifestResourceStream(name + ".Resources.Bot_dl5.png");

			this.Packet = assembly.GetManifestResourceStream(name + ".Resources.Packet.png");
			this.PacketDisabled = assembly.GetManifestResourceStream(name + ".Resources.Packet_disabled.png");
			this.PacketQueued = assembly.GetManifestResourceStream(name + ".Resources.Packet_queued.png");
			this.PacketBroken = assembly.GetManifestResourceStream(name + ".Resources.Packet_broken.png");
			this.PacketReady0 = assembly.GetManifestResourceStream(name + ".Resources.Packet_ready_0.png");
			this.PacketReady1 = assembly.GetManifestResourceStream(name + ".Resources.Packet_ready_1.png");
			this.PacketNew = assembly.GetManifestResourceStream(name + ".Resources.Packet_new.png");
			this.PacketDL0 = assembly.GetManifestResourceStream(name + ".Resources.Packet_dl0.png");
			this.PacketDL1 = assembly.GetManifestResourceStream(name + ".Resources.Packet_dl1.png");
			this.PacketDL2 = assembly.GetManifestResourceStream(name + ".Resources.Packet_dl2.png");
			this.PacketDL3 = assembly.GetManifestResourceStream(name + ".Resources.Packet_dl3.png");
			this.PacketDL4 = assembly.GetManifestResourceStream(name + ".Resources.Packet_dl4.png");
			this.PacketDL5 = assembly.GetManifestResourceStream(name + ".Resources.Packet_dl5.png");

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
		}

		protected Stream Client;

		protected Stream Server;
		protected Stream ServerConnected;
		protected Stream ServerDisabled;

		protected Stream Channel;
		protected Stream ChannelConnected;
		protected Stream ChannelDisabled;

		protected Stream Bot;
		protected Stream BotOff;
		protected Stream BotQueued;
		protected Stream BotFree;
		protected Stream BotFull;
		protected Stream BotDL0;
		protected Stream BotDL1;
		protected Stream BotDL2;
		protected Stream BotDL3;
		protected Stream BotDL4;
		protected Stream BotDL5;

		protected Stream Packet;
		protected Stream PacketDisabled;
		protected Stream PacketQueued;
		protected Stream PacketBroken;
		protected Stream PacketReady0;
		protected Stream PacketReady1;
		protected Stream PacketNew;
		protected Stream PacketDL0;
		protected Stream PacketDL1;
		protected Stream PacketDL2;
		protected Stream PacketDL3;
		protected Stream PacketDL4;
		protected Stream PacketDL5;

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
	}
}
