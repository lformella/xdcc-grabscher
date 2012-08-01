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

namespace XG.Server.Connection
{
	public class Connection : AConnection
	{
		#region VARIABLES

		private ILog log;

		private TcpClient tcpClient;

		private StreamWriter writer;

		bool isConnected;

		private SocketErrorCode errorCode = 0;

		#endregion

		#region CONNECT / RECEIVE DATA

		public override void Connect()
		{
			this.log = LogManager.GetLogger("Connection(" + this.Hostname + ":" + this.Port + ")");

			this.isConnected = false;
			using(this.tcpClient = new TcpClient())
			{
				this.tcpClient.ReceiveTimeout = this.MaxData > 0 ? Settings.Instance.DownloadTimeout : Settings.Instance.ServerTimeout;

				try
				{
					log.Info("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") start");
					this.tcpClient.Connect(this.Hostname, this.Port);
					this.isConnected = true;
				}
				catch (SocketException ex)
				{
					this.errorCode = (SocketErrorCode)ex.ErrorCode;
					log.Error("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") : " + ((SocketErrorCode)ex.ErrorCode), ex);
				}
				catch (Exception ex)
				{
					log.Fatal("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ")", ex);
				}

				if (this.isConnected)
				{
					log.Info("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") connected");

					using(NetworkStream stream = this.tcpClient.GetStream())
					{
						// we just need a writer if reading in text mode
						if (this.MaxData == 0)
						{
							this.writer = new StreamWriter(stream);
							this.writer.NewLine = "\r\n";
							this.writer.AutoFlush = true;
						}

						this.FireConnectedEvent();

						try
						{
							#region BINARY READING

							if (this.MaxData > 0)
							{
								using(BinaryReader reader = new BinaryReader(stream))
								{
									Int64 missing = this.MaxData;
									Int64 max = Settings.Instance.DownloadPerRead;
									byte[] data = null;
									do
									{
										try { data = reader.ReadBytes((int)(missing < max ? missing : max)); }
										catch (ObjectDisposedException) { break; }
										catch (SocketException ex)
										{
											this.errorCode = (SocketErrorCode)ex.ErrorCode;
											log.Fatal("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading: " + ((SocketErrorCode)ex.ErrorCode), ex);
										}
										catch (IOException ex)
										{
											if(ex.InnerException != null && ex.InnerException.GetType() == typeof(SocketException))
											{
												SocketException exi = (SocketException)ex.InnerException;
												this.errorCode = (SocketErrorCode)exi.ErrorCode;
												log.Fatal("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading: " + ((SocketErrorCode)exi.ErrorCode), ex);
												break;
											}
											else
											{
												log.Fatal("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading", ex);
											}
											break;
										}
										catch (Exception ex)
										{
											log.Fatal("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading", ex);
											break;
										}

										if (data != null && data.Length != 0)
										{
											this.FireDataBinaryReceivedEvent(data);
											missing -= data.Length;
										}
										else
										{
											log.Warn("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") no data received");
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
											//this.Log("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading: " + ((SocketErrorCode)ex.ErrorCode), LogLevel.Exception);
										}
										catch (IOException ex)
										{
											if(ex.InnerException.GetType() == typeof(SocketException))
											{
												SocketException exi = (SocketException)ex.InnerException;
												this.errorCode = (SocketErrorCode)exi.ErrorCode;
												// we dont need to log this kind of exception, because it is just normal
												//this.Log("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading: " + ((SocketErrorCode)exi.ErrorCode), LogLevel.Exception);
												break;
											}
											else
											{
												log.Fatal("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading", ex);
											}
											break;
										}
										catch (Exception ex)
										{
											log.Fatal("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") reading", ex);
											break;
										}

										if (/*data != "" && */data != null)
										{
											if(data != "")
											{
												failCounter = 0;
												this.FireDataTextReceivedEvent(data);
											}
										}
										else
										{
											failCounter++;
											if (failCounter > Settings.Instance.MaxNoDataReceived)
											{
												log.Warn("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") no data received");
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

						log.Info("Connect(" + (this.MaxData > 0 ? "" + this.MaxData : "") + ") end");
					}
				}
	
				this.FireDisconnectedEvent(this.errorCode);
			}

			this.tcpClient = null;
			this.writer = null;
		}

		#endregion

		#region DISCONNECT

		public override void Disconnect()
		{
			if (this.writer != null) { this.writer.Close(); }
			if (this.tcpClient != null) { this.tcpClient.Close(); }

			this.isConnected = false;
		}

		#endregion

		#region SEND DATA

		public override void SendData(string aData)
		{
			// we have to wait, until the connection is initialized - otherwise ther is nor logger
			if(this.log != null)
			{
				log.Debug("SendData(" + aData + ")");
			}
			if (this.writer != null)
			{
				try { this.writer.WriteLine(aData); }
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
						log.Fatal("SendData(" + aData + ") ", ex);
					}
				}
				catch (Exception ex)
				{
					log.Fatal("SendData(" + aData + ") ", ex);
				}
			}
		}

		#endregion
	}
}
