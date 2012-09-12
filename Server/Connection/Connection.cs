// 
//  Connection.cs
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
using System.IO;
using System.Net.Sockets;
using log4net;
using XG.Core;

namespace XG.Server.Connection
{
	public class Connection : AConnection
	{
		#region VARIABLES

		ILog _log;

		TcpClient _tcpClient;

		StreamWriter _writer;

		bool _isConnected;

		SocketErrorCode _errorCode = 0;

		#endregion

		#region CONNECT / RECEIVE DATA

		public override void Connect()
		{
			_log = LogManager.GetLogger("Connection(" + Hostname + ":" + Port + ")");

			_isConnected = false;
			using(_tcpClient = new TcpClient())
			{
				_tcpClient.ReceiveTimeout = MaxData > 0 ? Settings.Instance.DownloadTimeout : Settings.Instance.ServerTimeout;

				try
				{
					_log.Info("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") start");
					_tcpClient.Connect(Hostname, Port);
					_isConnected = true;
				}
				catch (SocketException ex)
				{
					_errorCode = (SocketErrorCode)ex.ErrorCode;
					_log.Error("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") : " + ((SocketErrorCode)ex.ErrorCode), ex);
				}
				catch (Exception ex)
				{
					_log.Fatal("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ")", ex);
				}

				if (_isConnected)
				{
					_log.Info("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") connected");

					using(NetworkStream stream = _tcpClient.GetStream())
					{
						// we just need a writer if reading in text mode
						if (MaxData == 0)
						{
							_writer = new StreamWriter(stream);
							_writer.NewLine = "\r\n";
							_writer.AutoFlush = true;
						}

						FireConnected();

						try
						{
							#region BINARY READING

							if (MaxData > 0)
							{
								using(BinaryReader reader = new BinaryReader(stream))
								{
									Int64 missing = MaxData;
									Int64 max = Settings.Instance.DownloadPerRead;
									byte[] data = null;
									do
									{
										try { data = reader.ReadBytes((int)(missing < max ? missing : max)); }
										catch (ObjectDisposedException) { break; }
										catch (SocketException ex)
										{
											_errorCode = (SocketErrorCode)ex.ErrorCode;
											if(_errorCode != SocketErrorCode.InterruptedFunctionCall)
											{
												_log.Fatal("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading: " + (_errorCode), ex);
											}
										}
										catch (IOException ex)
										{
											if(ex.InnerException != null && ex.InnerException is SocketException)
											{
												SocketException exi = (SocketException)ex.InnerException;
												_errorCode = (SocketErrorCode)exi.ErrorCode;
												_log.Fatal("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading: " + (_errorCode), ex);
												break;
											}
											else
											{
												_log.Fatal("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading", ex);
											}
											break;
										}
										catch (Exception ex)
										{
											_log.Fatal("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading", ex);
											break;
										}

										if (data != null && data.Length != 0)
										{
											FireDataBinaryReceived(data);
											missing -= data.Length;
										}
										else
										{
											_log.Warn("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") no data received");
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
											_errorCode = (SocketErrorCode)ex.ErrorCode;
											// we dont need to log this kind of exception, because it is just normal
											//Log("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading: " + ((SocketErrorCode)ex.ErrorCode), LogLevel.Exception);
										}
										catch (IOException ex)
										{
											if(ex.InnerException is SocketException)
											{
												SocketException exi = (SocketException)ex.InnerException;
												_errorCode = (SocketErrorCode)exi.ErrorCode;
												// we dont need to log this kind of exception, because it is just normal
												//Log("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading: " + ((SocketErrorCode)exi.ErrorCode), LogLevel.Exception);
												break;
											}
											else
											{
												_log.Fatal("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading", ex);
											}
											break;
										}
										catch (Exception ex)
										{
											_log.Fatal("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") reading", ex);
											break;
										}

										if (/*data != "" && */data != null)
										{
											if(data != "")
											{
												failCounter = 0;
												FireDataTextReceived(data);
											}
										}
										else
										{
											failCounter++;
											if (failCounter > Settings.Instance.MaxNoDataReceived)
											{
												_log.Warn("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") no data received");
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

							Disconnect();
						}
						catch (ObjectDisposedException)
						{
							// this is ok...
						}

						_log.Info("Connect(" + (MaxData > 0 ? "" + MaxData : "") + ") end");
					}
				}
	
				FireDisconnected(_errorCode);
			}

			_tcpClient = null;
			_writer = null;
		}

		#endregion

		#region DISCONNECT

		public override void Disconnect()
		{
			if (_writer != null) { _writer.Close(); }
			if (_tcpClient != null) { _tcpClient.Close(); }

			_isConnected = false;
		}

		#endregion

		#region SEND DATA

		public override void SendData(string aData)
		{
			// we have to wait, until the connection is initialized - otherwise ther is nor logger
			if(_log != null)
			{
				_log.Debug("SendData(" + aData + ")");
			}
			if (_writer != null)
			{
				try { _writer.WriteLine(aData); }
				catch (ObjectDisposedException)
				{
					// this is ok...
				}
				catch (SocketException ex)
				{
					_errorCode = (SocketErrorCode)ex.ErrorCode;
					// we dont need to log this kind of exception, because it is just normal
					//Log("SendData(" + aData + ") : " + ((SocketErrorCode)ex.ErrorCode), LogLevel.Exception);
				}
				catch (IOException ex)
				{
					if(ex.InnerException is SocketException)
					{
						SocketException exi = (SocketException)ex.InnerException;
						_errorCode = (SocketErrorCode)exi.ErrorCode;
						// we dont need to log this kind of exception, because it is just normal
						//Log("SendData(" + aData + ") : " + ((SocketErrorCode)exi.ErrorCode), LogLevel.Exception);
					}
					else
					{
						_log.Fatal("SendData(" + aData + ") ", ex);
					}
				}
				catch (Exception ex)
				{
					_log.Fatal("SendData(" + aData + ") ", ex);
				}
			}
		}

		#endregion
	}
}
