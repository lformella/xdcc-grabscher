// 
//  User.cs
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
using XG.Extensions;
using XG.Model.Domain;

namespace XG.Plugin.Irc.Parser.Types.Dcc
{
	public class XdccListSend : AParser
	{
		public override bool Parse(Message aMessage)
		{
			if (!aMessage.Text.StartsWith("\u0001DCC ", StringComparison.Ordinal))
			{
				return false;
			}
			string text = aMessage.Text.Substring(5, aMessage.Text.Length - 6);

			string[] tDataList = text.Split(' ');
			if (tDataList[0] == "SEND")
			{
				if (!Helper.Match(tDataList[1], ".*\\.txt$").Success)
				{
					Log.Error("Parse() " + aMessage.Nick + " send no text file: " + tDataList[1]);
					return false;
				}

				IPAddress ip;
				try
				{
					ip = IPAddress.Parse(tDataList[2]);
				}
				catch (Exception ex)
				{
					Log.Fatal("Parse() " + aMessage.Nick + " - can not parse ip from string: " + aMessage, ex);
					return false;
				}

				Int64 size;
				try
				{
					size = Int64.Parse(tDataList[4]);
				}
				catch (Exception ex)
				{
					Log.Fatal("Parse() " + aMessage.Nick + " - can not parse size from string: " + aMessage, ex);
					return false;
				}

				int port;
				try
				{
					port = int.Parse(tDataList[3]);
				}
				catch (Exception ex)
				{
					Log.Fatal("Parse() " + aMessage.Nick + " - can not parse port from string: " + aMessage, ex);
					return false;
				}

				// we cant connect to port <= 0
				if (port <= 0)
				{
					Log.Error("Parse() " + aMessage.Nick + " submitted wrong port: " + port);
					return false;
				}
				else
				{
					FireDownloadXdccList(this, new EventArgs<Server, string, Int64, IPAddress, int>(aMessage.Channel.Parent, aMessage.Nick, size, ip, port));
					return true;
				}
			}
			return false;
		}
	}
}
