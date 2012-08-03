//  
//  Copyright (C) 2012  <ich@larsformella.de>
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

using NUnit.Framework;

using XG.Core;
using XG.Server.Helper;

namespace XG.Test
{
	[TestFixture()]
	public class XG_Server_Helper_IrcParser
	{
		private XGServer Server;
		private XGChannel Channel;
		private XGBot Bot;
		private XGPacket Packet;

		private IrcParser IrcParser;

		public XG_Server_Helper_IrcParser ()
		{
			this.Server = new XGServer();
			this.Server.Name = "test.bitpir.at";

			this.Channel = new XGChannel();
			this.Channel.Name = "#test";
			this.Server.AddChannel(this.Channel);

			this.Bot = new XGBot();
			this.Bot.Name = "[XG]TestBot";
			this.Channel.AddBot(this.Bot);

			this.IrcParser = new IrcParser();
		}

		[Test()]
		public void ParseBandwidth ()
		{
			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :** Bandwidth Usage ** Current: 12.7kB/s, Record: 139.5kB/s");
			Assert.AreEqual(this.Bot.InfoSpeedCurrent, 12.7 * 1024);
			Assert.AreEqual(this.Bot.InfoSpeedMax, 139.5 * 1024);

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :** Bandwidth Usage ** Current: 0.0KB/s, Record: 231.4KB/s");
			Assert.AreEqual(this.Bot.InfoSpeedCurrent, 0);
			Assert.AreEqual(this.Bot.InfoSpeedMax, 231.4 * 1024);
		}

		public void ParsePackets()
		{
			//string str = ":[Hidd3n]-DreGen!~SYSTEM@F9D7CE5B.E493CF59.D35B78B8.IP PRIVMSG #HIDD3N-XDCC :#5   90x [181M] 6,9 Serie 9,6 The.Big.Bang.Theory.S05E05.Ab.nach.Baikonur.GERMAN.DUBBED.HDTVRiP.XviD-SOF.rar ";
		}
	}
}

