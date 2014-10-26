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
using XG.Business.Helper;
using XG.Model.Domain;
using XG.Plugin.Irc.Parser;

namespace XG.Test.Plugin.Irc.Parser
{
	public abstract class AParser
	{
		protected XG.Plugin.Irc.IrcConnection Connection;
		protected Server Server;
		protected Channel Channel;
		protected Bot Bot;
		protected Packet Packet;
		protected File File;

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

		protected AParser()
		{
			Server = new Server
			{
				Name = "test.bitpir.at"
			};

			Channel = new Channel
			{
				Name = "#test"
			};
			Server.AddChannel(Channel);

			Bot = new Bot
			{
				Name = "[XG]TestBot"
			};
			Channel.AddBot(Bot);

			Packet = new Packet
			{
				Name = "Testfile.with.a.long.name.mkv",
				RealName = "Testfile.with.a.long.name.mkv",
				Id = 1,
				Enabled = true,
				RealSize = 975304559
			};
			Bot.AddPacket(Packet);

			Connection = new XG.Plugin.Irc.IrcConnection();
			Connection.Server = Server;

			FileActions.Files = new Files();
			File = new File("Testfile.with.a.long.name.mkv", 975304559);
			FileActions.Files.Add(File);
		}

		protected void Parse(XG.Plugin.Irc.Parser.AParser aParser, string aMessage)
		{
			aMessage = XG.Plugin.Irc.Parser.Helper.RemoveSpecialIrcChars(aMessage);
			var message = new Message
			{
				Channel = Channel,
				Nick = Bot.Name,
				Text = aMessage
			};
			aParser.Parse(message);
		}
	}
}
