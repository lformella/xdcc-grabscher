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
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using XG.Core;

namespace XG.Client.TCP
{
	public class TCPClient
	{
		private TcpClient myClient;
		private BinaryWriter myWriter;

		public event EmptyDelegate ConnectedEvent;
		public event DataTextDelegate ConnectionErrorEvent;
		public event EmptyDelegate DisconnectedEvent;

		public event GuidDelegate RootGuidReceivedEvent;
		public event ObjectDelegate ObjectAddedEvent;
		public event ObjectDelegate ObjectChangedEvent;
		public event GuidDelegate ObjectRemovedEvent;

		public event EmptyDelegate ObjectBlockStartEvent;
		public event EmptyDelegate ObjectBlockStopEvent;

		bool myIsConnected;
		public bool IsConnected
		{
			get { return myIsConnected; }
		}

		public void Connect(string aServer, int aPort, string aPassword)
		{
			try
			{
				this.myClient = new TcpClient();
				this.myClient.Connect(aServer, aPort);

				NetworkStream stream = this.myClient.GetStream();
				BinaryFormatter binaryRead = new BinaryFormatter();
				BinaryReader reader = new BinaryReader(stream);

				this.myWriter = new BinaryWriter(stream);
				this.WriteData(TCPClientRequest.None, Guid.Empty, aPassword);

				this.RootGuidReceivedEvent(new Guid(reader.ReadBytes(16)));

				if (this.ConnectedEvent != null) { this.ConnectedEvent(); }
				this.myIsConnected = true;

				#region DATA HANDLING

				do
				{
					TCPServerResponse tMessage = (TCPServerResponse)reader.ReadByte();

					switch (tMessage)
					{
						case TCPServerResponse.ObjectAdded:
							if (this.ObjectAddedEvent != null) { this.ObjectAddedEvent((XGObject)binaryRead.Deserialize(stream)); }
							break;

						case TCPServerResponse.ObjectChanged:
							if (this.ObjectChangedEvent != null) { this.ObjectChangedEvent((XGObject)binaryRead.Deserialize(stream)); }
							break;

						case TCPServerResponse.ObjectRemoved:
							if (this.ObjectRemovedEvent != null) { this.ObjectRemovedEvent(new Guid(reader.ReadBytes(16))); }
							break;

						case TCPServerResponse.ObjectBlockStart:
							if(this.ObjectBlockStartEvent != null) { this.ObjectBlockStartEvent(); }
							break;

						case TCPServerResponse.ObjectBlockStop:
							if(this.ObjectBlockStopEvent != null) { this.ObjectBlockStopEvent(); }
							break;

						default:
							break;
					}
				}
				while (true);

				#endregion
			}
			catch (Exception ex)
			{
				if (this.ConnectionErrorEvent != null) { this.ConnectionErrorEvent(XGHelper.GetExceptionMessage(ex)); }
				this.Disconnect();
			}
		}

		public void Disconnect()
		{
			this.myIsConnected = false;

			if (this.myWriter != null) { this.myWriter.Close(); }
			if (this.myClient != null) { this.myClient.Close(); }
			if (this.DisconnectedEvent != null) { this.DisconnectedEvent(); }
		}

		public void WriteData(TCPClientRequest aMessage, Guid aGuid, string aData)
		{
			try
			{
				if (aMessage != TCPClientRequest.None) { this.myWriter.Write((byte)aMessage); }
				if (aGuid != Guid.Empty) { this.myWriter.Write(aGuid.ToByteArray()); }
				if (aData != null) { this.myWriter.Write(aData); }
				this.myWriter.Flush();
			}
			catch (Exception ex)
			{
				if (this.ConnectionErrorEvent != null) { this.ConnectionErrorEvent(XGHelper.GetExceptionMessage(ex)); }
				this.Disconnect();
			}
		}

		public void WriteSerializeData(TCPClientRequest aMessage, object aObject)
		{
			try
			{
				if (aMessage != TCPClientRequest.None) { this.myWriter.Write((byte)aMessage); }
				if (aObject != null) { new BinaryFormatter().Serialize(this.myWriter.BaseStream, aObject); }
				this.myWriter.Flush();
			}
			catch (Exception ex)
			{
				if (this.ConnectionErrorEvent != null) { this.ConnectionErrorEvent(XGHelper.GetExceptionMessage(ex)); }
				this.Disconnect();
			}
		}
	}
}