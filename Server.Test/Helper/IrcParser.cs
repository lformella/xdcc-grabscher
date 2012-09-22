// 
//  ServerHelperIrcParser.cs
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

#if !WINDOWS
using NUnit.Framework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using XG.Core;

namespace XG.Server.Helper.Test
{
#if !WINDOWS
    [TestFixture()]
#else
    [TestClass]
#endif
	public class IrcParser
	{
		string _parsingError;

		XG.Core.Server _server;
		Channel _channel;
		Bot _bot;

		Channel _eventChannel;	
		Bot _eventBot;
		Packet _eventPacket;
		Int64 _eventChunk;
		System.Net.IPAddress _eventIp;
		int _eventPort;
		string _eventData;
		AObject _eventObject;
		Int64 _eventTime;
		bool _eventOverride;

		XG.Server.Helper.IrcParser _ircParser;

		public IrcParser ()
		{
			_server = new XG.Core.Server();
			_server.Name = "test.bitpir.at";

			_channel = new Channel();
			_channel.Name = "#test";
			_server.AddChannel(_channel);

			_bot = new Bot();
			_bot.Name = "[XG]TestBot";
			_channel.AddBot(_bot);

			_ircParser = new XG.Server.Helper.IrcParser();

			_ircParser.ParsingError += new DataTextDelegate(IrcParserParsingError);

			_ircParser.AddDownload += new DownloadDelegate (IrcParserAddDownload);
			_ircParser.RemoveDownload += new BotDelegate (IrcParserRemoveDownload);

			_ircParser.SendData += new ServerDataTextDelegate(IrcParserSendData);
			_ircParser.JoinChannel += new ServerChannelDelegate(IrcParserJoinChannel);
			_ircParser.CreateTimer += new ServerObjectIntBoolDelegate(IrcParserCreateTimer);

			_ircParser.RequestFromBot += new ServerBotDelegate(IrcParserRequestFromBot);
			_ircParser.UnRequestFromBot += new ServerBotDelegate(IrcParserUnRequestFromBot);
		}

		void IrcParserParsingError (string aData)
		{
			_parsingError = aData;
		}

		void IrcParserAddDownload (Packet aPack, long aChunk, System.Net.IPAddress aIp, int aPort)
		{
			_eventPacket = aPack;
			_eventChunk = aChunk;
			_eventIp = aIp;
			_eventPort = aPort;
		}

		void IrcParserRemoveDownload (Bot aBot)
		{
			_eventBot = aBot;
		}

		void IrcParserSendData(XG.Core.Server aServer, string aData)
		{
			_eventData = aData;
		}

		void IrcParserJoinChannel(XG.Core.Server aServer, Channel aChannel)
		{
			_eventChannel = aChannel;
		}

		void IrcParserCreateTimer(XG.Core.Server aServer, AObject aObject, Int64 aTime, bool aOverride)
		{
			_eventObject = aObject;
			_eventTime = aTime;
			_eventOverride = aOverride;
		}

		void IrcParserRequestFromBot(XG.Core.Server aServer, Bot aBot)
		{
			_eventBot = aBot;
		}

		void IrcParserUnRequestFromBot(XG.Core.Server aServer, Bot aBot)
		{
			_eventBot = aBot;
		}

#if !WINDOWS
        [Test()]
#else
        [TestMethod]
#endif
		public void ParseBandwidth ()
		{
			_parsingError= "";

			_ircParser.ParseData(_server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :** Bandwidth Usage ** Current: 12.7kB/s, Record: 139.5kB/s");
			Assert.AreEqual(12.7 * 1024, _bot.InfoSpeedCurrent);
			Assert.AreEqual(139.5 * 1024, _bot.InfoSpeedMax);

			_ircParser.ParseData(_server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :** Bandwidth Usage ** Current: 0.0KB/s, Record: 231.4KB/s");
			Assert.AreEqual(0, _bot.InfoSpeedCurrent);
			Assert.AreEqual(231.4 * 1024, _bot.InfoSpeedMax);

			Assert.IsNullOrEmpty(_parsingError);
		}

#if !WINDOWS
        [Test()]
#else
        [TestMethod]
#endif
		public void ParsePacketInfo ()
		{
			_parsingError= "";

			_ircParser.ParseData(_server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :** 9 packs **  1 of 1 slot open, Min: 5.0kB/s, Record: 59.3kB/s");
			Assert.AreEqual(1, _bot.InfoSlotCurrent);
			Assert.AreEqual(1, _bot.InfoSlotTotal);

			_ircParser.ParseData(_server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :-> 1 Pack <-  10 Of 10 Slots Open Min: 15.0KB/s Record: 691.8KB/s");
			Assert.AreEqual(10, _bot.InfoSlotCurrent);
			Assert.AreEqual(10, _bot.InfoSlotTotal);

			_ircParser.ParseData(_server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :**[EWG]*   packs **  12 of 12 slots open, Record: 1736.8kB/s");
			Assert.AreEqual(12, _bot.InfoSlotCurrent);
			Assert.AreEqual(12, _bot.InfoSlotTotal);

			_ircParser.ParseData(_server, ":[XG]TestBot!~ROOT@local.host PRIVMSG #test :-> 18 PackS <-  13 Of 15 Slots Open Min: 15.0KB/s Record: 99902.4KB/s");
			Assert.AreEqual(13, _bot.InfoSlotCurrent);
			Assert.AreEqual(15, _bot.InfoSlotTotal);

			Assert.IsNullOrEmpty(_parsingError);
		}

#if !WINDOWS
        [Test()]
#else
        [TestMethod]
#endif
		public void ParsePackets ()
		{
			Packet tPack = null;
			_parsingError= "";

			_ircParser.ParseData(_server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#5   90x [181M] 6,9 Serie 9,6 The.Big.Bang.Theory.S05E05.Ab.nach.Baikonur.GERMAN.DUBBED.HDTVRiP.XviD-SOF.rar ");
			tPack = _bot.Packet(5);
			Assert.AreEqual(181 * 1024 * 1024, tPack.Size);
			Assert.AreEqual("Serie The.Big.Bang.Theory.S05E05.Ab.nach.Baikonur.GERMAN.DUBBED.HDTVRiP.XviD-SOF.rar", tPack.Name);

			_ircParser.ParseData(_server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#3  54x [150M] 2,11 [ABOOK] Fanny_Mueller--Grimms_Maerchen_(Abook)-2CD-DE-2008-OMA.rar ");
			tPack = _bot.Packet(3);
			Assert.AreEqual(150 * 1024 * 1024, tPack.Size);
			Assert.AreEqual("[ABOOK] Fanny_Mueller--Grimms_Maerchen_(Abook)-2CD-DE-2008-OMA.rar", tPack.Name);

			_ircParser.ParseData(_server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#1� 0x [� 5M] 5meg");
			tPack = _bot.Packet(1);
			Assert.AreEqual(5 * 1024 * 1024, tPack.Size);
			Assert.AreEqual("5meg", tPack.Name);

			_ircParser.ParseData(_server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT PRIVMSG #test :#18  5x [2.2G] Payback.Heute.ist.Zahltag.2011.German.DL.1080p.BluRay.x264-LeechOurStuff.mkv");
			tPack = _bot.Packet(18);
			Assert.AreEqual((Int64)(2.2 * 1024 * 1024 * 1024), tPack.Size);
			Assert.AreEqual("Payback.Heute.ist.Zahltag.2011.German.DL.1080p.BluRay.x264-LeechOurStuff.mkv", tPack.Name);

			Assert.IsNullOrEmpty(_parsingError);
		}

#if !WINDOWS
        [Test()]
#else
        [TestMethod]
#endif
		public void BotMessages ()
		{
			_parsingError= "";

			Settings.Instance.AutoJoinOnInvite = true;

			_ircParser.ParseData(_server, ":[XG]TestBot!~SYSTEM@XG.BITPIR.AT NOTICE xg1_bitpir_at : ** Closing Connection You Must JOIN MG-CHAT As Well To Download - Your Download Will Be Canceled Now");
			Assert.AreEqual("JOIN #MG-CHAT", _eventData);

			Assert.IsNullOrEmpty(_parsingError);
		}
	}
}

