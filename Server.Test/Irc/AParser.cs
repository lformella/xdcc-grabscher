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

using XG.Core;

namespace XG.Server.Irc.Test
{
	public abstract class AParser
	{
		protected XG.Core.Server _server;
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

		protected XG.Server.Irc.AParser _ircParser;

		public AParser()
		{
			_server = new XG.Core.Server();
			_server.Name = "test.bitpir.at";

			_channel = new Channel();
			_channel.Name = "#test";
			_server.AddChannel(_channel);

			_bot = new Bot();
			_bot.Name = "[XG]TestBot";
			_channel.AddBot(_bot);
		}

		public void RegisterParser(XG.Server.Irc.AParser aParser)
		{
			_ircParser =aParser;

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
			_eventParsingError = aData;
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
	}
}

