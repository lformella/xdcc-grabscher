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
		None,

		AddServer,
		RemoveServer,
		AddChannel,
		RemoveChannel,

		ActivateObject,
		DeactivateObject,

		SearchPacket,
		SearchPacketTime,
		SearchBot,
		SearchBotTime,

		GetServersChannels,
		GetActivePackets,
		GetFiles,
		GetObject,
		GetChildrenFromObject,

		CloseClient,
		RestartServer,
		CloseServer
	}

	#endregion

	#region DELEGATES

	public delegate void EmptyDelegate();
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
			if (aFromObj.GetType() == typeof(XGServer)) { tObj = new XGServer(); }
			else if (aFromObj.GetType() == typeof(XGChannel)) { tObj = new XGChannel(); }
			else if (aFromObj.GetType() == typeof(XGBot)) { tObj = new XGBot(); }
			else if (aFromObj.GetType() == typeof(XGPacket)) { tObj = new XGPacket(); }
			else if (aFromObj.GetType() == typeof(XGFile)) { tObj = new XGFile(); }
			else if (aFromObj.GetType() == typeof(XGFilePart)) { tObj = new XGFilePart(); }
			CloneObject(aFromObj, tObj, aFull);
			return tObj;
		}

		public static void CloneObject(XGObject aFromObj, XGObject aToObj, bool aFull)
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

		#endregion

		#region COMPARING

		public static bool IsEqual(byte[] aBytes1, byte[] aBytes2)
		{
			if(aBytes1 == null || aBytes2 == null) { return false; }
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

		#endregion

		#region HELPER

		/// <summary>
		/// Returns the file name shrinked to its information
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
		public static void Log(string aData, LogLevel aLevel)
		{
			// TODO uncomment this
//#if DEBUG
			if ((Int16)aLevel >= (Int16)LogLevel.Notice)
/*#else
			if ((Int16)aLevel >= (Int16)LogLevel.Error)
#endif*/
			{
				lock(LogLock)
				{ 
					Console.Write(DateTime.Now.ToString() + " ");
					switch(aLevel)
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
