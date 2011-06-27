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
		#region VARIABLES

		private TcpClient myTcpClient;

		private Int64 myMaxData;
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

		private SocketErrorCode errorCode = 0;
		public SocketErrorCode ErrorCode
		{
			get { return this.errorCode; }
		}

		#endregion

		#region EVENTS

		public event EmptyDelegate ConnectedEvent;
		public event SocketErrorDelegate DisconnectedEvent;
		public event DataTextDelegate DataTextReceivedEvent;
		public event DataBinaryDelegate DataBinaryReceivedEvent;

		#endregion

		#region CONNECT / RECEIVE DATA

		/// <summary>
		/// Connect to Hostname:Port an read in text mode
		/// </summary>
		public void Connect(string aHostname, int aPort)
		{
			this.Connect(aHostname, aPort, 0);
		}
		/// <summary>
		/// Connect to Hostname:Port an read in binary mode if MaxData > 0
		/// </summary>
		public void Connect(string aHostename, int aPort, Int64 aMaxData)
		{
			this.myHost = aHostename;
			this.myPort = aPort;

			this.myMaxData = aMaxData;
			this.myIsConnected = false;
			using(this.myTcpClient = new TcpClient())
			{
				this.myTcpClient.ReceiveTimeout = this.myMaxData > 0 ? Settings.Instance.DownloadTimeout : Settings.Instance.ServerTimeout;

				try
				{
					this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") start", LogLevel.Notice);
					this.myTcpClient.Connect(aHostename, aPort);
					this.myIsConnected = true;
				}
				catch (SocketException ex)
				{
					this.errorCode = (SocketErrorCode)ex.ErrorCode;
					this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") : " + ((SocketErrorCode)ex.ErrorCode), LogLevel.Exception);
				}

				if (this.myIsConnected)
				{
					this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") connected", LogLevel.Notice);

					using(NetworkStream stream = this.myTcpClient.GetStream())
					{
						// we just need a writer if reading in text mode
						if (this.myMaxData == 0)
						{
							this.myWriter = new StreamWriter(stream);
							this.myWriter.AutoFlush = true;
						}

						if (this.ConnectedEvent != null)
						{
							this.ConnectedEvent();
						}

						try
						{
							#region BINARY READING

							if (this.myMaxData > 0)
							{
								using(BinaryReader reader = new BinaryReader(stream))
								{
									Int64 missing = this.myMaxData;
									Int64 max = Settings.Instance.DownloadPerRead;
									byte[] data = null;
									do
									{
										try { data = reader.ReadBytes((int)(missing < max ? missing : max)); }
										catch (ObjectDisposedException) { break; }
										catch (SocketException ex)
										{
											this.errorCode = (SocketErrorCode)ex.ErrorCode;
											this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + ((SocketErrorCode)ex.ErrorCode), LogLevel.Exception);
										}
										catch (IOException ex)
										{
											if(ex.InnerException.GetType() == typeof(SocketException))
											{
												SocketException exi = (SocketException)ex.InnerException;
												this.errorCode = (SocketErrorCode)exi.ErrorCode;
												this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + ((SocketErrorCode)exi.ErrorCode), LogLevel.Exception);
												break;
											}
											else
											{
												this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
											}
											break;
										}
										catch (Exception ex)
										{
											this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
											break;
										}

										if (data != null && data.Length != 0)
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
							}

							#endregion

							#region TEXT READING

							else
							{
								using(StreamReader reader = new StreamReader(stream))
								{
									int failCounter = 0;
									string data = "";
									do
									{
										try { data = reader.ReadLine(); }
										catch (ObjectDisposedException) { break; }
										catch (SocketException ex)
										{
											this.errorCode = (SocketErrorCode)ex.ErrorCode;
											// we dont need to log this kind of exception, because it is just normal
											//this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + ((SocketErrorCode)ex.ErrorCode), LogLevel.Exception);
										}
										catch (IOException ex)
										{
											if(ex.InnerException.GetType() == typeof(SocketException))
											{
												SocketException exi = (SocketException)ex.InnerException;
												this.errorCode = (SocketErrorCode)exi.ErrorCode;
												// we dont need to log this kind of exception, because it is just normal
												//this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + ((SocketErrorCode)exi.ErrorCode), LogLevel.Exception);
												break;
											}
											else
											{
												this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
											}
											break;
										}
										catch (Exception ex)
										{
											this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
											break;
										}

										if (/*data != "" && */data != null)
										{
											if(data != "")
											{
												failCounter = 0;
												if (this.DataTextReceivedEvent != null)
												{
													this.DataTextReceivedEvent(data);
												}
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
							}

							#endregion

							this.Disconnect();
						}
						catch (ObjectDisposedException)
						{
							// this is ok...
						}

						this.Log("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") end", LogLevel.Notice);
					}
				}
	
				if (this.DisconnectedEvent != null)
				{
					this.DisconnectedEvent(this.errorCode);
				}
			}

			this.myTcpClient = null;
			this.myWriter = null;
		}

		#endregion

		#region DISCONNECT

		public void Disconnect()
		{
			if (this.myWriter != null) { this.myWriter.Close(); }
			if (this.myTcpClient != null) { this.myTcpClient.Close(); }

			this.myIsConnected = false;
		}

		#endregion

		#region SEND DATA

		public void SendData(string aData)
		{
			this.Log("SendData(" + aData + ")", LogLevel.Traffic);
			if (this.myWriter != null)
			{
				try { this.myWriter.WriteLine(aData); }
				catch (ObjectDisposedException)
				{
					// this is ok...
				}
				catch (SocketException ex)
				{
					this.errorCode = (SocketErrorCode)ex.ErrorCode;
					// we dont need to log this kind of exception, because it is just normal
					//this.Log("SendData(" + aData + ") : " + ((SocketErrorCode)ex.ErrorCode), LogLevel.Exception);
				}
				catch (IOException ex)
				{
					if(ex.InnerException.GetType() == typeof(SocketException))
					{
						SocketException exi = (SocketException)ex.InnerException;
						this.errorCode = (SocketErrorCode)exi.ErrorCode;
						// we dont need to log this kind of exception, because it is just normal
						//this.Log("SendData(" + aData + ") : " + ((SocketErrorCode)exi.ErrorCode), LogLevel.Exception);
					}
					else
					{
						this.Log("SendData(" + aData + ") : " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
					}
				}
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
