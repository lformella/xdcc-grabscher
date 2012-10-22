// 
//  AParser.cs
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

namespace XG.Server.Irc.Test
{
	public abstract class AParser
	{
		protected Core.Server _server;
		protected Channel _channel;
		protected Bot _bot;


		protected string _eventParsingError;
		protected Channel _eventChannel;	
		protected Bot _eventBot;
		protected Packet _eventPacket;
		protected Int64 _eventChunk;
		protected System.Net.IPAddress _eventIp;
		protected int _eventPort;
		protected string _eventData;
		protected AObject _eventObject;
		protected Int64 _eventTime;
		protected bool _eventOverride;

		protected Server.Irc.AParser _ircParser;

		public AParser()
		{
			_server = new Core.Server();
			_server.Name = "test.bitpir.at";

			_channel = new Channel();
			_channel.Name = "#test";
			_server.AddChannel(_channel);

			_bot = new Bot();
			_bot.Name = "[XG]TestBot";
			_channel.AddBot(_bot);
		}

		public void RegisterParser(Server.Irc.AParser aParser)
		{
			_ircParser =aParser;

			_ircParser.ParsingError += delegate (string aData)
			{
				_eventParsingError = aData;
			};

			_ircParser.AddDownload += delegate (Packet aPack, long aChunk, System.Net.IPAddress aIp, int aPort)
			{
				_eventPacket = aPack;
				_eventChunk = aChunk;
				_eventIp = aIp;
				_eventPort = aPort;
			};
			_ircParser.RemoveDownload += delegate (Bot aBot)
			{
				_eventBot = aBot;
			};

			_ircParser.SendData += delegate (Core.Server aServer, string aData)
			{
				Assert.AreEqual(_server, aServer);
				_eventData = aData;
			};
			_ircParser.JoinChannel += delegate (Core.Server aServer, Channel aChannel)
			{
				Assert.AreEqual(_server, aServer);
				_eventChannel = aChannel;
			};
			_ircParser.CreateTimer += delegate (Core.Server aServer, AObject aObject, int aTime, bool aOverride)
			{
				Assert.AreEqual(_server, aServer);
				_eventObject = aObject;
				_eventTime = aTime;
				_eventOverride = aOverride;
			};

			_ircParser.RequestFromBot += delegate (Core.Server aServer, Bot aBot)
			{
				Assert.AreEqual(_server, aServer);
				_eventBot = aBot;
			};
			_ircParser.UnRequestFromBot += delegate (Core.Server aServer, Bot aBot)
			{
				Assert.AreEqual(_server, aServer);
				_eventBot = aBot;
			};
		}
	}
}

