// 
//  AParser.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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
using System.Net;

using NUnit.Framework;

using XG.Core;

namespace XG.Server.Test.Irc
{
	public abstract class AParser
	{
		protected Core.Server Server;
		protected Channel Channel;
		protected Bot Bot;

		protected string EventParsingError;
		protected Channel EventChannel;
		protected Bot EventBot;
		protected Packet EventPacket;
		protected Int64 EventChunk;
		protected IPAddress EventIp;
		protected int EventPort;
		protected string EventData;
		protected AObject EventObject;
		protected Int64 EventTime;
		protected bool EventOverride;

		protected Server.Irc.AParser IrcParser;

		protected AParser()
		{
			Server = new Core.Server {Name = "test.bitpir.at"};

			Channel = new Channel {Name = "#test"};
			Server.AddChannel(Channel);

			Bot = new Bot {Name = "[XG]TestBot"};
			Channel.AddBot(Bot);
		}

		public void RegisterParser(Server.Irc.AParser aParser)
		{
			IrcParser = aParser;

			IrcParser.ParsingError += delegate(string aData) { EventParsingError = aData; };

			IrcParser.AddDownload += delegate(Packet aPack, long aChunk, IPAddress aIp, int aPort)
			{
				EventPacket = aPack;
				EventChunk = aChunk;
				EventIp = aIp;
				EventPort = aPort;
			};
			IrcParser.RemoveDownload += delegate(Bot aBot) { EventBot = aBot; };

			IrcParser.SendData += delegate(Core.Server aServer, string aData)
			{
				Assert.AreEqual(Server, aServer);
				EventData = aData;
			};
			IrcParser.JoinChannel += delegate(Core.Server aServer, Channel aChannel)
			{
				Assert.AreEqual(Server, aServer);
				EventChannel = aChannel;
			};
			IrcParser.CreateTimer += delegate(Core.Server aServer, AObject aObject, int aTime, bool aOverride)
			{
				Assert.AreEqual(Server, aServer);
				EventObject = aObject;
				EventTime = aTime;
				EventOverride = aOverride;
			};

			IrcParser.RequestFromBot += delegate(Core.Server aServer, Bot aBot)
			{
				Assert.AreEqual(Server, aServer);
				EventBot = aBot;
			};
			IrcParser.UnRequestFromBot += delegate(Core.Server aServer, Bot aBot)
			{
				Assert.AreEqual(Server, aServer);
				EventBot = aBot;
			};
		}
	}
}
