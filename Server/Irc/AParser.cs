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
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using log4net;

using XG.Core;

namespace XG.Server.Irc
{
	public delegate void BotDelegate (Bot aBot);

	public abstract class AParser
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string MAGICSTRING = @"((\*|:){2,3}|->|<-|)";

		#endregion

		#region EVENTS
		
		public event DownloadDelegate AddDownload;

		protected void FireAddDownload(Packet aPack, Int64 aChunk, IPAddress aIp, int aPort)
		{
			if(AddDownload != null)
			{
				AddDownload(aPack, aChunk, aIp, aPort);
			}
		}

		public event BotDelegate RemoveDownload;

		protected void FireRemoveDownload(Bot aBot)
		{
			if(RemoveDownload != null)
			{
				RemoveDownload(aBot);
			}
		}

		public event DataTextDelegate ParsingError;

		protected void FireParsingError(string aData)
		{
			if(ParsingError != null)
			{
				ParsingError(aData);
			}
		}

		public event ServerDataTextDelegate SendData;

		protected void FireSendData(Core.Server aServer, string aData)
		{
			if(SendData != null)
			{
				SendData(aServer, aData);
			}
		}

		public event ServerChannelDelegate JoinChannel;

		protected void FireJoinChannel(Core.Server aServer, Channel aChannel)
		{
			if(JoinChannel != null)
			{
				JoinChannel(aServer, aChannel);
			}
		}

		public event ServerObjectIntBoolDelegate CreateTimer;

		protected void FireCreateTimer(Core.Server aServer, AObject aObj, Int64 aInt, bool aBool)
		{
			if(CreateTimer != null)
			{
				CreateTimer(aServer, aObj, aInt, aBool);
			}
		}

		public event ServerBotDelegate RequestFromBot;

		protected void FireRequestFromBot(Core.Server aServer, Bot aBot)
		{
			if(RequestFromBot != null)
			{
				RequestFromBot(aServer, aBot);
			}
		}

		public event ServerBotDelegate UnRequestFromBot;

		protected void FireUnRequestFromBot(Core.Server aServer, Bot aBot)
		{
			if(UnRequestFromBot != null)
			{
				UnRequestFromBot(aServer, aBot);
			}
		}

		#endregion

		

		#region PARSING

		public void ParseData(Core.Server aServer, string aRawData)
		{
			_log.Debug("ParseData(" + aRawData + ")");

			if (aRawData.StartsWith(":"))
			{
				int tSplit = aRawData.IndexOf(':', 1);
				if (tSplit != -1)
				{
					string[] tCommands = aRawData.Split(':')[1].Split(' ');
					// there is an evil : in the hostname - dont know if this matches the rfc2812
					if(tCommands.Length < 3)
					{
						tSplit = aRawData.IndexOf(':', tSplit + 1);
						tCommands = aRawData.Substring(1).Split(' ');
					}

					string tMessage = Regex.Replace(aRawData.Substring(tSplit + 1), "(\u0001|\u0002)", "");

					Parse(aServer, aRawData, tMessage, tCommands);
				}
			}

			#region PING

			else if (aRawData.StartsWith("PING"))
			{
				_log.Info("ParseData() PING");
				FireSendData(aServer, "PONG " + aRawData.Split(':')[1]);
			}

			#endregion

			#region ERROR

			else if (aRawData.StartsWith("ERROR"))
			{
				_log.Error("ParseData() ERROR: " + aRawData);
			}

			#endregion
		}

		protected abstract void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands);

		#endregion

		#region HELPER

		protected string ClearString(string aData)
		{ // |\u0031|\u0015)
			aData = Regex.Replace(aData, "(\u0002|\u0003)(\\d+(,\\d{1,2}|)|)", "");
			aData = Regex.Replace(aData, "(\u000F)", "");
			return aData.Trim();
		}

		#endregion
	}
}

