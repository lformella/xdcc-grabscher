//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
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
using System.Text.RegularExpressions;

namespace XG.Core
{
	#region FLAGS

	[Flags]
	public enum BotState : byte
	{
		Idle,
		Active,
		Waiting
	}

	[Flags]
	public enum FilePartState : byte
	{
		Open,
		Closed,
		Ready,
		Broken
	}

	[Flags]
	public enum LogLevel : byte
	{
		Traffic,
		Info,
		Notice,
		Warning,
		Error,
		Exception,
		None
	}

	[Flags]
	public enum TCPServerResponse : byte
	{
		None,

		ObjectChanged,
		ObjectAdded,
		ObjectRemoved,

		ObjectBlockStart,
		ObjectBlockStop
	}

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
		SearchPacketTime = 9,
		SearchPacketActiveDownloads = 10,
		SearchPacketsEnabled = 11,
		SearchBot = 12,
		SearchBotTime = 13,
		SearchBotActiveDownloads = 14,
		SearchBotsEnabled = 15,

		GetServersChannels = 16,
		GetActivePackets = 17,
		GetFiles = 18,
		GetObject = 19,
		GetChildrenFromObject = 20,

		CloseClient = 21,
		CloseServer = 22
	}

	[Flags]
	public enum SocketErrorCode : int
	{
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
	public delegate void DataBinaryDelegate(byte[] aData);

	public delegate void RootServerDelegate(RootObject aObj, XGServer aServer);
	public delegate void ServerChannelDelegate(XGServer aServer, XGChannel aChan);
	public delegate void StringPacketDelegate(string aData, XGPacket aPack);
	public delegate void StringGuidDelegate(string aData, Guid aGuid);

	public delegate void GuidDelegate(Guid aGuid);
	public delegate void ObjectDelegate(XGObject aObj);
	public delegate void ObjectObjectDelegate(XGObject aObj1, XGObject aObj2);
	public delegate void ServerDelegate(XGServer aServer);
	public delegate void ServerSocketErrorDelegate(XGServer aServer, SocketErrorCode aValue);
	public delegate void BotDelegate (XGBot aBot);
	public delegate void PacketDelegate(XGPacket aPack);

	#endregion

	public class XGHelper
	{
		#region CLONING

		public static RootObject CloneObject(RootObject aFromObj, bool aFull)
		{
			RootObject tObj = new RootObject();
			XGHelper.CloneObject(aFromObj, tObj, aFull);
			foreach (XGServer oldServ in aFromObj.Children)
			{
				XGServer newServ = new XGServer(tObj);
				XGHelper.CloneObject(oldServ, newServ, aFull);
				foreach (XGChannel oldChan in oldServ.Children)
				{
					XGChannel newChan = new XGChannel(newServ);
					XGHelper.CloneObject(oldChan, newChan, aFull);
					foreach (XGBot oldBot in oldChan.Children)
					{
						XGBot newBot = new XGBot(newChan);
						XGHelper.CloneObject(oldBot, newBot, aFull);
						foreach (XGPacket oldPack in oldBot.Children)
						{
							XGPacket newPack = new XGPacket(newBot);
							XGHelper.CloneObject(oldPack, newPack, aFull);
						}
					}
				}
			}
			return tObj;
		}

		public static XGObject CloneObject(XGObject aFromObj, bool aFull)
		{
			XGObject tObj = null;
			if (aFromObj != null)
			{
				if (aFromObj.GetType() == typeof(XGServer)) { tObj = new XGServer(); }
				else if (aFromObj.GetType() == typeof(XGChannel)) { tObj = new XGChannel(); }
				else if (aFromObj.GetType() == typeof(XGBot)) { tObj = new XGBot(); }
				else if (aFromObj.GetType() == typeof(XGPacket)) { tObj = new XGPacket(); }
				else if (aFromObj.GetType() == typeof(XGFile)) { tObj = new XGFile(); }
				else if (aFromObj.GetType() == typeof(XGFilePart)) { tObj = new XGFilePart(); }
				CloneObject(aFromObj, tObj, aFull);
			}
			return tObj;
		}

		public static void CloneObject(XGObject aFromObj, XGObject aToObj, bool aFull)
		{
			if (aFromObj != null && aToObj != null)
			{
				//Console.WriteLine(aFromObj.Guid + " " + aFromObj.Name);
				if (aFromObj.GetType() == typeof(XGServer))
				{
					(aToObj as XGServer).Clone(aFromObj as XGServer, aFull);
				}
				else if (aFromObj.GetType() == typeof(XGChannel))
				{
					(aToObj as XGChannel).Clone(aFromObj as XGChannel, aFull);
				}
				else if (aFromObj.GetType() == typeof(XGBot))
				{
					(aToObj as XGBot).Clone(aFromObj as XGBot, aFull);
				}
				else if (aFromObj.GetType() == typeof(XGPacket))
				{
					(aToObj as XGPacket).Clone(aFromObj as XGPacket, aFull);
				}
				else if (aFromObj.GetType() == typeof(XGFile))
				{
					(aToObj as XGFile).Clone(aFromObj as XGFile, aFull);
				}
				else if (aFromObj.GetType() == typeof(XGFilePart))
				{
					(aToObj as XGFilePart).Clone(aFromObj as XGFilePart, aFull);
				}
			}
		}

		#endregion

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

		public static int CompareObjects(XGObject aObj1, XGObject aObj2)
		{
			if (aObj1 == null)
			{
				if (aObj2 == null) { return 0; }
				else { return -1; }
			}
			else
			{
				if (aObj2 == null) { return 1; }
				else
				{
					if (aObj1.GetType() == typeof(XGPacket))
					{
						int ret = ((XGPacket)aObj1).Id.CompareTo(((XGPacket)aObj2).Id);
						return ret != 0 ? ret : aObj1.Name.CompareTo(aObj2.Name);
					}
					else if (aObj1.GetType() == typeof(XGFilePart))
					{
						return ((XGFilePart)aObj1).StartSize.CompareTo(((XGFilePart)aObj2).StartSize);
					}
					else { return aObj1.Name.CompareTo(aObj2.Name); }
				}
			}
		}

		#endregion

		#region COMPARING EXTENDED

		public static int CompareObjectName(XGObject aObj1, XGObject aObj2)
		{
			if (aObj1 == null)
			{
				if (aObj2 == null) { return 0; }
				else { return -1; }
			}
			else
			{
				if (aObj2 == null) { return 1; }
				else
				{
					return aObj1.Name.CompareTo(aObj2.Name);
				}
			}
		}

		public static int CompareObjectNameReverse(XGObject aObj1, XGObject aObj2)
		{
			return CompareObjectName(aObj2, aObj1);
		}

		public static int CompareObjectConnected(XGObject aObj1, XGObject aObj2)
		{
			if (aObj1 == null)
			{
				if (aObj2 == null) { return 0; }
				else { return -1; }
			}
			else
			{
				if (aObj2 == null) { return 1; }
				else
				{
					int ret = aObj1.Connected.CompareTo(aObj2.Connected);
					return ret != 0 ? ret : aObj1.Name.CompareTo(aObj2.Name);
				}
			}
		}

		public static int CompareObjectConnectedReverse(XGObject aObj1, XGObject aObj2)
		{
			return CompareObjectConnected(aObj2, aObj1);
		}

		public static int CompareObjectEnabled(XGObject aObj1, XGObject aObj2)
		{
			if (aObj1 == null)
			{
				if (aObj2 == null) { return 0; }
				else { return -1; }
			}
			else
			{
				if (aObj2 == null) { return 1; }
				else
				{
					int ret = aObj1.Enabled.CompareTo(aObj2.Enabled);
					return ret != 0 ? ret : aObj1.Name.CompareTo(aObj2.Name);
				}
			}
		}

		public static int CompareObjectEnabledReverse(XGObject aObj1, XGObject aObj2)
		{
			return CompareObjectEnabled(aObj2, aObj1);
		}

		public static int ComparePacketId(XGObject aObj1, XGObject aObj2)
		{
			if (aObj1 == null)
			{
				if (aObj2 == null) { return 0; }
				else { return -1; }
			}
			else
			{
				if (aObj2 == null) { return 1; }
				else
				{
					int ret = ((XGPacket)aObj1).Id.CompareTo(((XGPacket)aObj2).Id);
					return ret != 0 ? ret : aObj1.Name.CompareTo(aObj2.Name);
				}
			}
		}

		public static int ComparePacketIdReverse(XGObject aObj1, XGObject aObj2)
		{
			return ComparePacketId(aObj2, aObj1);
		}

		public static int ComparePacketSize(XGObject aObj1, XGObject aObj2)
		{
			if (aObj1 == null)
			{
				if (aObj2 == null) { return 0; }
				else { return -1; }
			}
			else
			{
				if (aObj2 == null) { return 1; }
				else
				{
					XGPacket tPack1 = (XGPacket)aObj1;
					XGPacket tPack2 = (XGPacket)aObj2;
					int ret = (tPack1.RealSize > 0 ? tPack1.RealSize : tPack1.Size).CompareTo(tPack2.RealSize > 0 ? tPack2.RealSize : tPack2.Size);
					return ret != 0 ? ret : aObj1.Name.CompareTo(aObj2.Name);
				}
			}
		}

		public static int ComparePacketSizeReverse(XGObject aObj1, XGObject aObj2)
		{
			return ComparePacketSize(aObj2, aObj1);
		}

		public static int ComparePacketLastUpdated(XGObject aObj1, XGObject aObj2)
		{
			if (aObj1 == null)
			{
				if (aObj2 == null) { return 0; }
				else { return -1; }
			}
			else
			{
				if (aObj2 == null) { return 1; }
				else
				{
					int ret = ((XGPacket)aObj1).LastUpdated.CompareTo(((XGPacket)aObj2).LastUpdated);
					return ret != 0 ? ret : aObj1.Name.CompareTo(aObj2.Name);
				}
			}
		}

		public static int ComparePacketLastUpdatedReverse(XGObject aObj1, XGObject aObj2)
		{
			return ComparePacketLastUpdated(aObj2, aObj1);
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

		#region LOGGING

		private static object LogLock = new object();
		public static LogLevel LogLevel = LogLevel.Warning;
		public static void Log(string aData, LogLevel aLevel)
		{
			if ((Int16)aLevel >= (Int16)LogLevel)
			{
				lock (LogLock)
				{
					Console.Write(DateTime.Now.ToString() + " ");
					switch (aLevel)
					{
						case LogLevel.Traffic:		Console.Write("TRAFFIC  "); break;
						case LogLevel.Info:			Console.Write("INFO     "); break;
						case LogLevel.Notice:		Console.Write("NOTICE   "); break;
						case LogLevel.Warning:		Console.Write("WARNING  "); break;
						case LogLevel.Error:		Console.Write("ERROR    "); break;
						case LogLevel.Exception:	Console.Write("EXCEPTION"); break;
					}
					Console.WriteLine(" " + aData);
				}
			}
		}

		public static string GetExceptionMessage(Exception aException)
		{
#if DEBUG
			return aException.ToString();
#else
			return aException.Message;
#endif
		}

		#endregion
	}
}
