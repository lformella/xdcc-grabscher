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
using System.Reflection;
using Meebey.SmartIrc4net;
using XG.Model.Domain;

namespace XG.Test.Plugin.Irc.Parser
{
	public abstract class AParser
	{
		protected XG.Plugin.Irc.IrcConnection Connection;
		protected XG.Model.Domain.Server Server;
		protected XG.Model.Domain.Channel Channel;
		protected Bot Bot;

		protected XG.Model.Domain.Channel EventChannel;
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
			Server = new XG.Model.Domain.Server { Name = "test.bitpir.at" };

			Channel = new XG.Model.Domain.Channel { Name = "#test" };
			Server.AddChannel(Channel);

			Bot = new Bot {Name = "[XG]TestBot"};
			Channel.AddBot(Bot);

			Connection = new XG.Plugin.Irc.IrcConnection();
			Connection.Server = Server;

			//parser.Parse(null, "", CreateIrcEventArgs(Channel.Name, Bot.Name, "", ReceiveType.QueryMessage));
		}

		protected IrcEventArgs CreateIrcEventArgs(string aChannel, string aBot, string aMessage, ReceiveType aType)
		{
			IrcMessageData data = new IrcMessageData(null, "", aBot, "", "", aChannel, aMessage, aMessage, aType, ReplyCode.Null);
			IrcEventArgs args = (IrcEventArgs)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(IrcEventArgs));
			FieldInfo[] EventFields = typeof(IrcEventArgs).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			EventFields[0].SetValue(args, data);

			return args;
		}

		protected IrcEventArgs CreateCtcpEventArgs(string aChannel, string aBot, string aMessage, ReceiveType aType, string aCtcpCommand)
		{
			IrcMessageData data = new IrcMessageData(null, "", aBot, "", "", aChannel, aMessage, aMessage, aType, ReplyCode.Null);
			CtcpEventArgs args = (CtcpEventArgs)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(CtcpEventArgs));
			FieldInfo[] EventFields = typeof(IrcEventArgs).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			EventFields[0].SetValue(args, data);

			FieldInfo[] EventFields2 = typeof(string).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			EventFields2[1].SetValue(args, aCtcpCommand);

			return args;
		}

		protected void Parse(XG.Plugin.Irc.Parser.AParser aParser, XG.Plugin.Irc.IrcConnection aConnection, IrcEventArgs aEvent)
		{
			string tMessage = XG.Plugin.Irc.Parser.Helper.RemoveSpecialIrcChars(aEvent.Data.Message);
			aParser.Parse(aConnection, tMessage, aEvent);
		}
	}
}
