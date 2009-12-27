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
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using XG.Core;

namespace XG.Client.TCP
{
	public class TCPClient
	{
		#region VARIABLES

		private TcpClient myClient;
		private BinaryWriter myWriter;

		bool myIsConnected;
		public bool IsConnected
		{
			get { return myIsConnected; }
		}

		private RootObject myRootObject;
		public RootObject RootObject
		{
			get { return myRootObject; }
		}

		private List<XGObject> myParentlessObjects;
		private List<string> myCompleteSearches;
		private List<XGObject> myCompleteObjects;
		private bool myEnabledPacketsFetched = false;

		#endregion

		#region EVENTS

		public event EmptyDelegate ConnectedEvent;
		public event DataTextDelegate ConnectionErrorEvent;
		public event EmptyDelegate DisconnectedEvent;

		public event ObjectObjectDelegate ObjectAddedEvent;
		public event ObjectDelegate ObjectChangedEvent;
		public event ObjectDelegate ObjectRemovedEvent;

		public event EmptyDelegate ObjectBlockStartEvent;
		public event EmptyDelegate ObjectBlockStopEvent;

		#endregion

		#region INIT

		public TCPClient()
		{
			this.myParentlessObjects = new List<XGObject>();
			this.myCompleteSearches = new List<string>();
			this.myCompleteObjects = new List<XGObject>();

			this.myRootObject = new RootObject();
		}

		#endregion

		#region TCP CONNECTION

		public void Connect(string aServer, int aPort, string aPassword)
		{
#if !UNSAFE
			try
			{
#endif
			this.myClient = new TcpClient();
			this.myClient.Connect(aServer, aPort);

			NetworkStream stream = this.myClient.GetStream();
			BinaryFormatter binaryRead = new BinaryFormatter();
			BinaryReader reader = new BinaryReader(stream);

			this.myWriter = new BinaryWriter(stream);
			this.WriteData(TCPClientRequest.None, Guid.Empty, aPassword);

			this.myRootObject.SetGuid(new Guid(reader.ReadBytes(16)));

			if (this.ConnectedEvent != null) { this.ConnectedEvent(); }
			this.myIsConnected = true;

			#region DATA HANDLING

			do
			{
				TCPServerResponse tMessage = (TCPServerResponse)reader.ReadByte();

				switch (tMessage)
				{
					case TCPServerResponse.ObjectAdded:
					case TCPServerResponse.ObjectChanged:
						XGObject tObj = (XGObject)binaryRead.Deserialize(stream);
						XGObject oldObj = this.myRootObject.getChildByGuid(tObj.Guid);
						if (oldObj != null)
						{
							XGHelper.CloneObject(tObj, oldObj, true);
							if (this.ObjectChangedEvent != null) { this.ObjectChangedEvent(tObj); }
						}
						else
						{
							XGObject parentObj = this.myRootObject.getChildByGuid(tObj.ParentGuid);
							if (parentObj != null || tObj.ParentGuid == Guid.Empty)
							{
								this.AddObject(tObj);
								foreach (XGObject obj in this.myParentlessObjects.ToArray())
								{
									if (obj.ParentGuid == tObj.Guid)
									{
										this.myParentlessObjects.Remove(obj);
										this.AddObject(obj);
									}
								}
							}
							else
							{
								// just ask once on multiple parentless objects needing the same parent
								bool ask = true;
								foreach (XGObject tChildObj in this.myParentlessObjects.ToArray())
								{
									if (tChildObj.ParentGuid == tObj.ParentGuid)
									{
										ask = false;
										break;
									}
								}
								if (ask) { this.WriteData(TCPClientRequest.GetObject, tObj.ParentGuid, null); }
								this.myParentlessObjects.Add(tObj);
							}
						}
						break;

					case TCPServerResponse.ObjectRemoved:
						Guid tGuid = new Guid(reader.ReadBytes(16));
						XGObject remObj = this.myRootObject.getChildByGuid(tGuid);
						if (remObj != null)
						{
							if (remObj.GetType() == typeof(XGServer))
							{
								XGServer tServ = remObj as XGServer;
								this.myRootObject.removeServer(tServ);
							}

							else if (remObj.GetType() == typeof(XGChannel))
							{
								XGChannel tChan = remObj as XGChannel;
								tChan.Parent.removeChannel(tChan);
							}

							else if (remObj.GetType() == typeof(XGBot))
							{
								XGBot tBot = remObj as XGBot;
								tBot.Parent.removeBot(tBot);
							}

							else if (remObj.GetType() == typeof(XGPacket))
							{
								XGPacket tPack = remObj as XGPacket;
								tPack.Parent.removePacket(tPack);
							}

							else if (remObj.GetType() == typeof(XGFile))
							{
								XGFile tFile = remObj as XGFile;
								this.myRootObject.removeChild(tFile);
							}

							else if (remObj.GetType() == typeof(XGFilePart))
							{
								XGFilePart tPart = remObj as XGFilePart;
								tPart.Parent.removePart(tPart);
							}

							if (this.ObjectRemovedEvent != null) { this.ObjectRemovedEvent(remObj); }
						}
						break;

					case TCPServerResponse.ObjectBlockStart:
						if (this.ObjectBlockStartEvent != null) { this.ObjectBlockStartEvent(); }
						break;

					case TCPServerResponse.ObjectBlockStop:
						if (this.ObjectBlockStopEvent != null) { this.ObjectBlockStopEvent(); }
						break;

					default:
						break;
				}
			}
			while (true);

			#endregion
#if !UNSAFE
			}
			catch (Exception ex)
			{
				if (this.ConnectionErrorEvent != null) { this.ConnectionErrorEvent(XGHelper.GetExceptionMessage(ex)); }
				this.Disconnect();
			}
#endif
		}

		public void Disconnect()
		{
			this.myIsConnected = false;

			try
			{
				this.WriteData(TCPClientRequest.CloseClient, Guid.Empty, null);
				if (this.myWriter != null) { this.myWriter.Close(); }
				if (this.myClient != null) { this.myClient.Close(); }
			}
			catch (Exception) {}

			if (this.DisconnectedEvent != null) { this.DisconnectedEvent(); }

			this.myParentlessObjects.Clear();
			this.myCompleteSearches.Clear();
			this.myCompleteObjects.Clear();
			this.myEnabledPacketsFetched = false;

			this.myRootObject = new RootObject();
		}

		private void WriteData(TCPClientRequest aMessage, Guid aGuid, string aData)
		{
			try
			{
				if (aMessage != TCPClientRequest.None) { this.myWriter.Write((byte)aMessage); }
				if (aGuid != Guid.Empty) { this.myWriter.Write(aGuid.ToByteArray()); }
				if (aData != null && aData != "") { this.myWriter.Write(aData); }
				this.myWriter.Flush();
			}
			catch (Exception ex)
			{
				if (this.ConnectionErrorEvent != null) { this.ConnectionErrorEvent(XGHelper.GetExceptionMessage(ex)); }
				this.Disconnect();
			}
		}

		private void AddObject(XGObject aObj)
		{
			if (aObj.GetType() == typeof(XGServer))
			{
				this.myRootObject.addServer(aObj as XGServer);
				if (this.ObjectAddedEvent != null) { this.ObjectAddedEvent(aObj, this.myRootObject); }
			}

			else if (aObj.GetType() == typeof(XGChannel))
			{
				XGServer tServ = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGServer;
				tServ.addChannel(aObj as XGChannel);
				if (this.ObjectAddedEvent != null) { this.ObjectAddedEvent(aObj, tServ); }
			}

			else if (aObj.GetType() == typeof(XGBot))
			{
				XGChannel tChan = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGChannel;
				XGBot tBot = aObj as XGBot;
				tChan.addBot(tBot);
				if (this.ObjectAddedEvent != null) { this.ObjectAddedEvent(aObj, tChan); }
			}

			else if (aObj.GetType() == typeof(XGPacket))
			{
				XGBot tBot = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGBot;
				XGPacket tPack = aObj as XGPacket;
				tBot.addPacket(tPack);
				if (this.ObjectAddedEvent != null) { this.ObjectAddedEvent(aObj, tPack); }
			}

			else if (aObj.GetType() == typeof(XGFile))
			{
				this.myRootObject.addChild(aObj as XGFile);
				if (this.ObjectAddedEvent != null) { this.ObjectAddedEvent(aObj, this.myRootObject); }
			}

			else if (aObj.GetType() == typeof(XGFilePart))
			{
				XGFile tFile = this.myRootObject.getChildByGuid(aObj.ParentGuid) as XGFile;
				XGFilePart tPart = aObj as XGFilePart;
				tFile.addPart(tPart);
				if (this.ObjectAddedEvent != null) { this.ObjectAddedEvent(aObj, tFile); }

				XGPacket tPack = this.myRootObject.getChildByGuid(tPart.PacketGuid) as XGPacket;
				if (tPack != null)
				{
					tPart.Packet = tPack;
				}
			}
		}

		#endregion

		#region GET

		public void GetChildren(XGObject aObj)
		{
			if (!this.myCompleteObjects.Contains(aObj))
			{
				this.WriteData(TCPClientRequest.GetChildrenFromObject, aObj.Guid, null);
				this.myCompleteObjects.Add(aObj);
			}
		}

		public void GetEnabledPackets()
		{
			if (!this.myEnabledPacketsFetched)
			{
				this.myEnabledPacketsFetched = true;
				this.WriteData(TCPClientRequest.GetActivePackets, Guid.Empty, null);
			}
		}

		public void GetFiles()
		{
			this.WriteData(TCPClientRequest.GetFiles, Guid.Empty, null);
		}

		#endregion

		#region SEARCH

		public void SearchPacket(string aSearch)
		{
			if (!this.myCompleteSearches.Contains(aSearch))
			{
				this.myCompleteSearches.Add(aSearch);
				this.WriteData(TCPClientRequest.SearchPacket, Guid.Empty, aSearch);
			}
		}

		public void SearchPacketTime(string aSearch)
		{
			if (!this.myCompleteSearches.Contains(aSearch))
			{
				this.myCompleteSearches.Add(aSearch);
				this.WriteData(TCPClientRequest.SearchPacketTime, Guid.Empty, aSearch);
			}
		}

		#endregion

		#region SET

		public void FlipObject(XGObject aObj)
		{
			if (aObj != null)
			{
				if (!aObj.Enabled) { this.WriteData(TCPClientRequest.ActivateObject, aObj.Guid, null); }
				else { this.WriteData(TCPClientRequest.DeactivateObject, aObj.Guid, null); }
			}
		}

		public void AddServer(string aData)
		{
			this.WriteData(TCPClientRequest.AddServer, Guid.Empty, aData);
		}

		public void AddChannel(string aData, Guid aGuid)
		{
			this.WriteData(TCPClientRequest.AddChannel, aGuid, aData);
		}

		public void RemoveServer(Guid aGuid)
		{
			this.WriteData(TCPClientRequest.RemoveServer, aGuid, null);
		}

		public void RemoveChannel(Guid aGuid)
		{
			this.WriteData(TCPClientRequest.RemoveChannel, aGuid, null);
		}

		#endregion
	}
}
