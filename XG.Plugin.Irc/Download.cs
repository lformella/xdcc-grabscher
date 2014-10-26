// 
//  Download.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using XG.Config.Properties;
using XG.Extensions;
using XG.Model.Domain;
using log4net;

namespace XG.Plugin.Irc
{
	public delegate void ServerUserDelegate(Server aServer, string aBot);

	public class Download : AWorker
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public Server Server { get; set; }
		public string Bot { get; set; }
		public Int64 Size { get; set; }
		public Int64 CurrentSize { get; private set; }
		public IPAddress IP { get; set; }
		public int Port { get; set; }
		public string FileName { get; set; }

		TcpClient _tcpClient;
		BinaryWriter _writer;

		bool _streamOk;

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<Server, string>> OnConnected;
		public event EventHandler<EventArgs<Server, string>> OnDisconnected;
		public event EventHandler<EventArgs<Server, string>> OnReady;

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			using (_tcpClient = new TcpClient())
			{
				_tcpClient.SendTimeout = Settings.Default.DownloadTimeoutTime * 1000;
				_tcpClient.ReceiveTimeout = Settings.Default.DownloadTimeoutTime * 1000;

				try
				{
					_tcpClient.Connect(IP, Port);
					_log.Info("StartRun() connected");

					using (Stream stream = new ThrottledStream(_tcpClient.GetStream(), Settings.Default.MaxDownloadSpeedInKB * 1000))
					{
						StartWriting();

						using (var reader = new BinaryReader(stream))
						{
							Int64 missing = Size;
							Int64 max = Settings.Default.DownloadPerReadBytes;
							byte[] data = null;
							do
							{
								data = reader.ReadBytes((int) (missing < max ? missing : max));

								if (data != null && data.Length != 0)
								{
									SaveData(data);
									missing -= data.Length;
								}
								else
								{
									_log.Warn("StartRun() no data received");
									break;
								}
							} while (AllowRunning && missing > 0);
						}

						_log.Info("StartRun() end");
					}
				}
				catch (ObjectDisposedException) {}
				catch (Exception ex)
				{
					_log.Fatal("StartRun()", ex);
				}
				finally
				{
					StopWriting();

					_tcpClient = null;
					_writer = null;
				}
			}
		}

		protected override void StopRun()
		{
			if (_tcpClient != null)
			{
				_tcpClient.Close();
			}
		}

		#endregion

		#region CONNECT

		protected void StartWriting()
		{
			try
			{
				var info = new FileInfo(FileName);
				FileStream stream = info.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

				// we are connected
				if (OnConnected != null)
				{
					OnConnected(this, new EventArgs<Server, string>(Server, Bot));
				}

				_streamOk = true;
				_writer = new BinaryWriter(stream);
			}
			catch (Exception ex)
			{
				_log.Fatal("StartWriting(" + FileName + ")", ex);
				_tcpClient.Close();
				return;
			}
		}

		protected void StopWriting()
		{
			// close the writer
			if (_writer != null)
			{
				_writer.Close();
			}

			if (_streamOk)
			{
				// the file is ok if the size is equal or it has an additional buffer for checking
				if (CurrentSize == Size)
				{
					_log.Info("StopWriting(" + FileName + ") ready");

					if (OnReady != null)
					{
						OnReady(this, new EventArgs<Server, string>(Server, Bot));
					}
				}
				// that should not happen
				else if (CurrentSize > Size)
				{
					_log.Error("StopWriting(" + FileName + ") size is bigger than excepted: " + CurrentSize + " > " + Size);
				}
				// it did not start
				else if (CurrentSize == 0)
				{
					_log.Error("StopWriting(" + FileName + ") downloading did not start");
				}
				// it is incomplete
				else
				{
					_log.Error("StopWriting(" + FileName + ") incomplete");
				}
			}
			// the connection didnt even connected to the given ip and port
			else
			{
				_log.Error("StopWriting(" + FileName + ") connection did not work");
			}

			if (OnDisconnected != null)
			{
				OnDisconnected(this, new EventArgs<Server, string>(Server, Bot));
			}
		}

		void SaveData(byte[] aData)
		{
			try
			{
				_writer.Write(aData);
				_writer.Flush();
				CurrentSize += aData.Length;
			}
			catch (Exception ex)
			{
				_log.Fatal("SaveData(" + FileName + ") write", ex);
				_streamOk = false;
				_tcpClient.Close();
				return;
			}
		}

		#endregion
	}
}
