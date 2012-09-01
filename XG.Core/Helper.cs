// 
//  Helper.cs
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
using System.Text.RegularExpressions;

namespace XG.Core
{
	#region FLAGS

	[Flags]
	public enum TCPClientRequest : byte
	{
		None = 0,
		Version = 1,

		AddServer = 2,
		RemoveServer = 3,
		AddChannel = 4,
		RemoveChannel = 5,

		ActivateObject = 6,
		DeactivateObject = 7,

		SearchPacket = 8,
		SearchBot = 9,

		GetServers = 10,
		GetChannelsFromServer = 11,
		GetBotsFromChannel = 12,
		GetPacketsFromBot = 13,
		GetFiles = 14,
		GetObject = 15,

		AddSearch = 16,
		RemoveSearch = 17,
		GetSearches = 18,

		GetStatistics = 19,
		ParseXdccLink = 20,

		CloseServer = 21
	}

	[Flags]
	public enum SocketErrorCode : int
	{
		None							= 0,
		InterruptedFunctionCall			= 10004,
		PermissionDenied				= 10013,
		BadAddress						= 10014,
		InvalidArgument					= 10022,
		TooManyOpenFiles				= 10024,
		ResourceTemporarilyUnavailable	= 10035,
		OperationNowInProgress			= 10036,
		OperationAlreadyInProgress		= 10037,
		SocketOperationOnNonSocket		= 10038,
		DestinationAddressRequired		= 10039,
		MessgeTooLong					= 10040,
		WrongProtocolType				= 10041,
		BadProtocolOption				= 10042,
		ProtocolNotSupported			= 10043,
		SocketTypeNotSupported			= 10044,
		OperationNotSupported			= 10045,
		ProtocolFamilyNotSupported		= 10046,
		AddressFamilyNotSupported		= 10047,
		AddressInUse					= 10048,
		AddressNotAvailable				= 10049,
		NetworkIsDown					= 10050,
		NetworkIsUnreachable			= 10051,
		NetworkReset					= 10052,
		ConnectionAborted				= 10053,
		ConnectionResetByPeer			= 10054,
		NoBufferSpaceAvailable			= 10055,
		AlreadyConnected				= 10056,
		NotConnected					= 10057,
		CannotSendAfterShutdown			= 10058,
		ConnectionTimedOut				= 10060,
		ConnectionRefused				= 10061,
		HostIsDown						= 10064,
		HostUnreachable					= 10065,
		TooManyProcesses				= 10067,
		NetworkSubsystemIsUnavailable	= 10091,
		UnsupportedVersion				= 10092,
		NotInitialized					= 10093,
		ShutdownInProgress				= 10101,
		ClassTypeNotFound				= 10109,
		HostNotFound					= 11001,
		HostNotFoundTryAgain			= 11002,
		NonRecoverableError				= 11003,
		NoDataOfRequestedType			= 11004
	}

	#endregion

	#region DELEGATES

	public delegate void EmptyDelegate();

	public delegate void SocketErrorDelegate(SocketErrorCode aValue);

	public delegate void DataTextDelegate(string aData);
	public delegate void ServerDataTextDelegate(Server aServer, string aData);
	public delegate void DataBinaryDelegate(byte[] aData);

	public delegate void ServerDelegate(Server aServer);
	public delegate void ServerBotDelegate (Server aServer, Bot aBot);
	public delegate void ServerChannelDelegate(Server aServer, Channel aChan);

	public delegate void ServerObjectIntBoolDelegate(Server aServer, AObject aObj, Int64 aInt, bool aBool);

	#endregion

	public class XGHelper
	{
		#region COMPARING

		public static bool IsEqual(byte[] aBytes1, byte[] aBytes2)
		{
			if (aBytes1 == null || aBytes2 == null) { return false; }
			for (int i = 0; i < aBytes1.Length; i++)
			{
				if (aBytes1[i] != aBytes2[i]) { return false; }
			}
			return true;
		}

		#endregion

		#region HELPER

		/// <summary>
		/// Returns the file name ripped of the following chars ()[]{}-_.
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aSize"></param>
		/// <returns></returns>
		public static string ShrinkFileName(string aName, Int64 aSize)
		{
			if (aName != null)
			{
				return Regex.Replace(aName, "(\\(|\\)|\\[|\\]|\\{|\\}|-|_|\\.)", "").ToLower() + "." + aSize + "/";
			}
			else { return ""; }
		}

		#endregion
	}
}
