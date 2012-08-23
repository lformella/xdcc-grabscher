//  
//  Copyright (C) 2012 Lars Formella <ich@larsformella.de>
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
		
		[Test()]
		public void ParsePacketInfo ()
		{
			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :** 9 packs **  1 of 1 slot open, Min: 5.0kB/s, Record: 59.3kB/s");
			Assert.AreEqual(this.Bot.InfoSlotCurrent, 1);
			Assert.AreEqual(this.Bot.InfoSlotTotal, 1);

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :-> 1 Pack <-  10 Of 10 Slots Open Min: 15.0KB/s Record: 691.8KB/s");
			Assert.AreEqual(this.Bot.InfoSlotCurrent, 10);
			Assert.AreEqual(this.Bot.InfoSlotTotal, 10);

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :**[EWG]*   packs **  12 of 12 slots open, Record: 1736.8kB/s");
			Assert.AreEqual(this.Bot.InfoSlotCurrent, 12);
			Assert.AreEqual(this.Bot.InfoSlotTotal, 12);

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :-> 18 PackS <-  13 Of 15 Slots Open Min: 15.0KB/s Record: 99902.4KB/s");
			Assert.AreEqual(this.Bot.InfoSlotCurrent, 13);
			Assert.AreEqual(this.Bot.InfoSlotTotal, 15);
		}
		
		[Test()]
		public void ParsePackets ()
		{
			XGPacket tPack = null;

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#5   90x [181M] 6,9 Serie 9,6 The.Big.Bang.Theory.S05E05.Ab.nach.Baikonur.GERMAN.DUBBED.HDTVRiP.XviD-SOF.rar ");
			tPack = this.Bot[5];
			Assert.AreEqual(tPack.Size, 181 * 1024 * 1024);
			Assert.AreEqual(tPack.Name, "Serie The.Big.Bang.Theory.S05E05.Ab.nach.Baikonur.GERMAN.DUBBED.HDTVRiP.XviD-SOF.rar");

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#3  54x [150M] 2,11 [ABOOK] Fanny_Mueller--Grimms_Maerchen_(Abook)-2CD-DE-2008-OMA.rar ");
			tPack = this.Bot[3];
			Assert.AreEqual(tPack.Size, 150 * 1024 * 1024);
			Assert.AreEqual(tPack.Name, "[ABOOK] Fanny_Mueller--Grimms_Maerchen_(Abook)-2CD-DE-2008-OMA.rar");

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#1� 0x [� 5M] 5meg");
			tPack = this.Bot[1];
			Assert.AreEqual(tPack.Size, 5 * 1024 * 1024);
			Assert.AreEqual(tPack.Name, "5meg");

			this.IrcParser.ParseData(this.Server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#18  5x [2.2G] Payback.Heute.ist.Zahltag.2011.German.DL.1080p.BluRay.x264-LeechOurStuff.mkv");
			tPack = this.Bot[18];
			Assert.AreEqual(tPack.Size, (Int64)(2.2 * 1024 * 1024 * 1024));
			Assert.AreEqual(tPack.Name, "Payback.Heute.ist.Zahltag.2011.German.DL.1080p.BluRay.x264-LeechOurStuff.mkv");
		}

		// Closing Connection You Must JOIN MG-CHAT As Well To Download - Your Download Will Be Canceled Now
	}
}

