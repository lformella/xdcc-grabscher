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
using XG.Core;

namespace XG.Server
{
	public class Connection
	{
		private TcpClient myTcpClient;

		private Int64 myMaxData;
		private StreamReader myReaderT;
		private BinaryReader myReaderB;
		private StreamWriter myWriter;

		private string myHost;
		public string Host
		{
			get { return myHost; }
		}

		private int myPort;
		public int Port
		{
			get { return myPort; }
		}

		bool myIsConnected;
		public bool IsConnected
		{
			get { return myIsConnected; }
		}

		#region DELEGATES

		public event EmptyDelegate ConnectedEvent;
		public event EmptyDelegate DisconnectedEvent;
		public event DataTextDelegate DataTextReceivedEvent;
		public event DataBinaryDelegate DataBinaryReceivedEvent;

		#endregion

		#region CONNECT / RECEIVE DATA

		public void Connect(string aName, int aPort)
		{
			this.Connect(aName, aPort, 0);
		}
		public void Connect(string aName, int aPort, Int64 aMaxData)
		{
			this.myHost = aName;
			this.myPort = aPort;

			this.myMaxData = aMaxData;
			this.myIsConnected = false;
			this.myTcpClient = new TcpClient();
			this.myTcpClient.ReceiveTimeout = this.myMaxData > 0 ? Settings.Instance.DownloadTimeout : Settings.Instance.ServerTimeout;

			try
			{
				this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") start", LogLevel.Notice);
				this.myTcpClient.Connect(aName, aPort);
				this.myIsConnected = true;
			}
			catch (SocketException ex)
			{
				this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
			}

			if (this.myIsConnected)
			{
				this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") connected", LogLevel.Notice);

				NetworkStream stream = this.myTcpClient.GetStream();
				if (this.myMaxData > 0) { this.myReaderB = new BinaryReader(stream); }
				else { this.myReaderT = new StreamReader(stream); }

				this.myWriter = new StreamWriter(stream);
				this.myWriter.AutoFlush = true;

				if (this.ConnectedEvent != null)
				{
					this.ConnectedEvent();
				}

				try
				{
					#region BINARY READING

					if (this.myMaxData > 0)
					{
						Int64 missing = this.myMaxData;
						Int64 max = Settings.Instance.DownloadPerRead;
						byte[] data;
						do
						{
							try { data = this.myReaderB.ReadBytes((int)(missing < max ? missing : max)); }
							catch (ObjectDisposedException) { break; }
							catch (Exception ex)
							{
								this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
								break;
							}
							if (data.Length != 0)
							{
								if (this.DataBinaryReceivedEvent != null)
								{
									this.DataBinaryReceivedEvent(data);
								}
								missing -= data.Length;
							}
							else
							{
								this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") no data received", LogLevel.Warning);
								break;
							}
						}
						while (data != null && missing > 0);
					}

					#endregion

					#region TEXT READING

					else
					{
						int failCounter = 0;
						string data;
						do
						{
							try { data = this.myReaderT.ReadLine(); }
							catch (ObjectDisposedException) { break; }
							catch (Exception ex)
							{
								this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
								break;
							}
							if (data != "" && data != null)
							{
								failCounter = 0;
								if (this.DataTextReceivedEvent != null)
								{
									this.DataTextReceivedEvent(data);
								}
							}
							else
							{
								failCounter++;
								if (failCounter > Settings.Instance.MaxNoDataReceived)
								{
									this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") no data received", LogLevel.Warning);
									break;
								}
								else
								{
									data = "";
								}
							}
						}
						while (data != null);
					}

					#endregion

					this.Disconnect();
				}
				catch (ObjectDisposedException)
				{
					// this is ok...
				}
				/*catch (Exception ex)
				{
					this.Log("Connect(" + aName + ", " + aPort + (aMaxData > 0 ? ", " + aMaxData : "") + ") Exception: " + XGHelper.GetExceptionMessage(ex), LogLevel.Error);
				}*/

				this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") end", LogLevel.Notice);
			}

			if (this.DisconnectedEvent != null)
			{
				this.DisconnectedEvent();
			}
		}

		#endregion

		#region DISCONNECT

		public void Disconnect()
		{
			if (this.myReaderB != null) { this.myReaderB.Close(); }
			if (this.myReaderT != null) { this.myReaderT.Close(); }
			if (this.myWriter != null) { this.myWriter.Close(); }
			this.myTcpClient.Close();
			this.myIsConnected = false;
		}

		#endregion

		#region SEND DATA

		public void SendData(string aData)
		{
			this.Log("SendData(" + aData + ")", LogLevel.Traffic);
			if (this.myReaderT != null)
			{
				try { this.myWriter.WriteLine(aData); }
				catch (Exception ex)
				{
					this.Log("SendData(" + aData + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				}
			}
		}

		#endregion

		#region LOG

		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("Connection(" + this.myHost + ":" + this.myPort + ")." + aData, aLevel);
		}

		#endregion
	}
}
