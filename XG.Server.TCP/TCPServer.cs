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
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using XG.Core;

namespace XG.Server.TCP
{
	public class TCPServer : IServerPlugin
	{
		private ServerRunner myRunner;
		
		private Thread myServerThread;
		private TcpListener myListener;
		
		private List<BinaryWriter> myWriter = new List<BinaryWriter>();
		private BinaryFormatter myFormatter = new BinaryFormatter();

		#region RUN STOP RESTART

		/// <summary>
		/// Run method - opens itself in a new thread
		/// </summary>
		public void Start(ServerRunner aParent)
		{
			this.myRunner = aParent;
			this.myRunner.ObjectAddedEvent += new ObjectObjectDelegate(myRunner_ObjectAddedEventHandler);
			this.myRunner.ObjectChangedEvent += new ObjectDelegate(myRunner_ObjectChangedEventHandler);
			this.myRunner.ObjectRemovedEvent += new ObjectObjectDelegate(myRunner_ObjectRemovedEventHandler);
			
			// start the server thread
			this.myServerThread = new Thread(new ThreadStart(OpenServer));
			this.myServerThread.Start();
		}

		/// <summary>
		/// called if the client signals to stop
		/// </summary>
		public void Stop()
		{
			this.myRunner.ObjectAddedEvent -= new ObjectObjectDelegate(myRunner_ObjectAddedEventHandler);
			this.myRunner.ObjectChangedEvent -= new ObjectDelegate(myRunner_ObjectChangedEventHandler);
			this.myRunner.ObjectRemovedEvent -= new ObjectObjectDelegate(myRunner_ObjectRemovedEventHandler);
			
			this.CloseServer();
			this.myServerThread.Abort();
		}

		/// <summary>
		/// called if the client signals to do a restart
		/// </summary>
		public void Restart()
		{
			this.Stop();
			this.Start(this.myRunner);
		}

		#endregion

		#region SERVER

		/// <summary>
		/// Opens the server port, waiting for clients
		/// </summary>
		private void OpenServer()
		{
			this.myListener = new TcpListener(IPAddress.Any, Settings.Instance.TcpServerPort);
			try
			{
				this.myListener.Start();

				while (true)
				{
					try
					{
						TcpClient client = this.myListener.AcceptTcpClient();
						Thread t = new Thread(new ParameterizedThreadStart(OpenClient));
						t.IsBackground = true;
						t.Start(client);
					}
					catch (Exception ex)
					{
						this.Log("OpenServer() client: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					}
				}
			}
			catch (Exception ex)
			{
				this.Log("OpenServer() server: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}
		}
		
		/// <summary>
		/// Close the server
		/// </summary>
		private void CloseServer()
		{
			this.myListener.Stop();
		}

		#endregion

		#region CLIENT

		/// <summary>
		/// Called if a client connects
		/// </summary>
		/// <param name="aObject"></param>
		private void OpenClient(object aObject)
		{
			TcpClient client = aObject as TcpClient;
			NetworkStream stream = client.GetStream();
			BinaryWriter bw = new BinaryWriter(stream);
			BinaryReader tReader = new BinaryReader(stream);

			// no pass, no way
			try
			{
				string pass = tReader.ReadString();
				// nice try
				if (pass != Settings.Instance.Password)
				{
					throw new Exception("Password wrong!");
				}
			}
			catch (Exception ex)
			{
				this.Log("OpenClient() password: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				client.Close();
				return;
			}

			// send root guid
			bw.Write(this.myRunner.RootGuid.ToByteArray());

			// send initial set of servers + channels
			this.WriteToStream(bw, this.myRunner.GetServersChannels(), TCPServerResponse.ObjectAdded);

			this.myWriter.Add(bw);

			bool alive = true;
			while (alive)
			{
				TCPClientRequest tMessage = TCPClientRequest.None;

				// read the request id
				try { tMessage = (TCPClientRequest)tReader.ReadByte(); }
				// this is ok
				catch (SocketException) { }
				catch (Exception ex)
				{
					this.Log("OpenClient() read client request: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					break;
				}

				#region DATA HANDLING

#if !UNSAFE
				try
				{
#endif
					switch (tMessage)
					{
						#region SERVER

						case TCPClientRequest.AddServer:
							this.myRunner.AddServer(tReader.ReadString());
							break;

						case TCPClientRequest.RemoveServer:
							this.myRunner.RemoveServer(new Guid(tReader.ReadBytes(16)));
							break;

						#endregion

						#region CHANNEL

						case TCPClientRequest.AddChannel:
							this.myRunner.AddChannel(new Guid(tReader.ReadBytes(16)), tReader.ReadString());
							break;

						case TCPClientRequest.RemoveChannel:
							this.myRunner.RemoveChannel(new Guid(tReader.ReadBytes(16)));
							break;

						#endregion

						#region OBJECT

						case TCPClientRequest.ActivateObject:
							this.myRunner.ActivateObject(new Guid(tReader.ReadBytes(16)));
							break;

						case TCPClientRequest.DeactivateObject:
							this.myRunner.DeactivateObject(new Guid(tReader.ReadBytes(16)));
							break;

						#endregion

						#region SEARCH

						case TCPClientRequest.SearchPacket:
							this.WriteToStream(bw, this.myRunner.SearchPacket(tReader.ReadString()), TCPServerResponse.ObjectAdded);
							break;

						case TCPClientRequest.SearchPacketTime:
							this.WriteToStream(bw, this.myRunner.SearchPacketTime(tReader.ReadString()), TCPServerResponse.ObjectAdded);
							break;

						#endregion

						#region GET

						case TCPClientRequest.GetActivePackets:
							this.WriteToStream(bw, this.myRunner.GetActivePackets(), TCPServerResponse.ObjectAdded);
							break;

						case TCPClientRequest.GetFiles:
							this.WriteToStream(bw, this.myRunner.GetFiles(), TCPServerResponse.ObjectAdded);
							break;

						case TCPClientRequest.GetObject:
							this.WriteToStream(bw, this.myRunner.GetObject(new Guid(tReader.ReadBytes(16))), TCPServerResponse.ObjectAdded);
							break;

						case TCPClientRequest.GetChildrenFromObject:
							this.WriteToStream(bw, this.myRunner.GetChildrenFromObject(new Guid(tReader.ReadBytes(16))), TCPServerResponse.ObjectAdded);
							break;

						#endregion

						#region COMMANDS

						case TCPClientRequest.CloseClient:
							alive = false;
							break;

						case TCPClientRequest.RestartServer:
							this.Restart();
							break;

						case TCPClientRequest.CloseServer:
							this.Stop();
							break;

						#endregion

						default:
							break;
					}
#if !UNSAFE
				}
				// this is ok
				catch (SocketException) {} 
				catch (Exception ex)
				{
					this.Log("OpenClient() read: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					break;
				}
#endif

				#endregion
			}

			this.Log("OpenClient() disconnected", LogLevel.Info);

			this.myWriter.Remove(bw);

			client.Close();
		}

		#endregion

		#region WRITE TO STREAM

		/// <summary>
		/// Writes a bunch of objects with a response message to a stream
		/// </summary>
		/// <param name="aWriter"></param>
		/// <param name="aList"></param>
		/// <param name="aMessage"></param>
		private void WriteToStream(BinaryWriter aWriter, XGObject[] aList, TCPServerResponse aMessage)
		{
			this.WriteToStream(aWriter, null, TCPServerResponse.ObjectBlockStart, null);
			foreach (XGObject tObj in aList)
			{
				if (!this.WriteToStream(aWriter, tObj, aMessage)) { break; }
			}
			this.WriteToStream(aWriter, null, TCPServerResponse.ObjectBlockStop, null);
		}

		/// <summary>
		/// Writes an object with a response message to a stream
		/// </summary>
		/// <param name="aWriter"></param>
		/// <param name="aObject"></param>
		/// <param name="aMessage"></param>
		/// <returns></returns>
		private bool WriteToStream(BinaryWriter aWriter, XGObject aObject, TCPServerResponse aMessage)
		{
			return this.WriteToStream(aWriter, aObject, aMessage, null);
		}

		/// <summary>
		/// Writes an object with data and a response message to a stream
		/// </summary>
		/// <param name="aWriter">the stream, or null if sending to all streams</param>
		/// <param name="aObject"></param>
		/// <param name="aMessage"></param>
		/// <param name="aData"></param>
		/// <returns></returns>
		private bool WriteToStream(BinaryWriter aWriter, XGObject aObject, TCPServerResponse aMessage, string aData)
		{
			bool ok = true;
			if (aWriter == null)
			{
				foreach (BinaryWriter tWriter in this.myWriter.ToArray())
				{
					if (!this.WriteToStream(tWriter, aObject, aMessage, aData)) { ok = false; }
				}
			}
			else
			{
				lock (aWriter)
				{
					try
					{
						aWriter.Write((byte)aMessage);
						switch (aMessage)
						{
							case TCPServerResponse.ObjectAdded:
							case TCPServerResponse.ObjectChanged:
								this.myFormatter.Serialize(aWriter.BaseStream, XGHelper.CloneObject(aObject, true));
								break;

							case TCPServerResponse.ObjectRemoved:
								aWriter.Write(aObject.Guid.ToByteArray());
								break;
						}
						aWriter.Flush();
						//this.Log("Sending " + aObject.Guid + " | " + aObject.Parent.Guid + " | " + aObject.Name, LogLevel.Warning);
					}
					// this is ok
					catch (ObjectDisposedException) { }
					catch (SocketException) { }
					catch (Exception ex)
					{
						ok = false;
						this.Log("WriteToStream() write: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					}
				}
			}
			return ok;
		}

		#endregion
		
		#region EVENTS
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="aParentObj"></param>
		/// <param name="aObj"></param>
		protected void myRunner_ObjectAddedEventHandler(XGObject aParentObj, XGObject aObj)
		{
			this.WriteToStream(null, aObj, TCPServerResponse.ObjectAdded);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aObj"></param>
		protected void myRunner_ObjectChangedEventHandler(XGObject aObj)
		{
			this.WriteToStream(null, aObj, TCPServerResponse.ObjectChanged);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aParentObj"></param>
		/// <param name="aObj"></param>
		protected void myRunner_ObjectRemovedEventHandler(XGObject aParentObj, XGObject aObj)
		{
			this.WriteToStream(null, aObj, TCPServerResponse.ObjectRemoved);
		}

		#endregion

		#region LOG

		/// <summary>
		/// Calls XGGelper.Log()
		/// </summary>
		/// <param name="aData"></param>
		/// <param name="aLevel"></param>
		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("TCPServer." + aData, aLevel);
		}

		#endregion
	}
}