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
using log4net;
using XG.Core;

namespace XG.Server
{
	public class Connection
	{
		#region VARIABLES

		private ILog myLog;

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
		public void Connect(string aHostname, int aPort, Int64 aMaxData)
		{
			this.myLog = LogManager.GetLogger("Connection(" + aHostname + ")");

			this.myHost = aHostname;
			this.myPort = aPort;

			this.myMaxData = aMaxData;
			this.myIsConnected = false;
			using(this.myTcpClient = new TcpClient())
			{
				this.myTcpClient.ReceiveTimeout = this.myMaxData > 0 ? Settings.Instance.DownloadTimeout : Settings.Instance.ServerTimeout;

				try
				{
					myLog.Info("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") start");
					this.myTcpClient.Connect(aHostname, aPort);
					this.myIsConnected = true;
				}
				catch (SocketException ex)
				{
					this.errorCode = (SocketErrorCode)ex.ErrorCode;
					myLog.Fatal("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") : " + ((SocketErrorCode)ex.ErrorCode), ex);
				}

				if (this.myIsConnected)
				{
					myLog.Info("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") connected");

					using(NetworkStream stream = this.myTcpClient.GetStream())
					{
						// we just need a writer if reading in text mode
						if (this.myMaxData == 0)
						{
							this.myWriter = new StreamWriter(stream);
							this.myWriter.NewLine = "\r\n";
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
											myLog.Fatal("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + ((SocketErrorCode)ex.ErrorCode), ex);
										}
										catch (IOException ex)
										{
											if(ex.InnerException != null && ex.InnerException.GetType() == typeof(SocketException))
											{
												SocketException exi = (SocketException)ex.InnerException;
												this.errorCode = (SocketErrorCode)exi.ErrorCode;
												myLog.Fatal("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading: " + ((SocketErrorCode)exi.ErrorCode), ex);
												break;
											}
											else
											{
												myLog.Fatal("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading", ex);
											}
											break;
										}
										catch (Exception ex)
										{
											myLog.Fatal("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading", ex);
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
											myLog.Warn("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") no data received");
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
												myLog.Fatal("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading", ex);
											}
											break;
										}
										catch (Exception ex)
										{
											myLog.Fatal("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") reading", ex);
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
												myLog.Warn("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") no data received");
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

						myLog.Info("Connect(" + (aMaxData > 0 ? "" + aMaxData : "") + ") end");
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
			myLog.Debug("SendData(" + aData + ")");
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
						myLog.Fatal("SendData(" + aData + ") ", ex);
					}
				}
				catch (Exception ex)
				{
					myLog.Fatal("SendData(" + aData + ") ", ex);
				}
			}
		}

		#endregion
	}
}
