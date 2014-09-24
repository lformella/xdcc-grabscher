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
using XG.Business.Helper;
using XG.Config.Properties;
using XG.Extensions;
using XG.Model.Domain;
using log4net;

namespace XG.Plugin.Irc
{
	public class BotDownload : Connection
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		Packet _packet;

		public Packet Packet
		{
			get { return _packet; }
			set
			{
				if (_packet != null)
				{
					_packet.OnEnabledChanged -= EnabledChanged;
				}
				_packet = value;
				if (_packet != null)
				{
					_packet.OnEnabledChanged += EnabledChanged;
				}
			}
		}

		public Files Files { get; set; }

		public Int64 StartSize { get; set; }
		public IPAddress IP { get; set; }
		public int Port { get; set; }
		public Int64 MaxData { get; set; }

		TcpClient _tcpClient;
		BinaryWriter _writer;
		BinaryReader _reader;

		Int64 _receivedBytes;
		DateTime _speedCalcTime;
		Int64 _speedCalcSize;

		byte[] _rollbackRefernce;
		byte[] _startBuffer;

		bool _streamOk;
		bool _removeFile;

		Int64 CurrentSize
		{
			get { return StartSize + _receivedBytes; }
		}

		public Model.Domain.File File { get; set; }

		#endregion

		#region EVENTS

		public event EventHandler<EventArgs<Packet>> OnConnected;
		public event EventHandler<EventArgs<Packet>> OnDisconnected;

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			Name = IP + ":" + Port;

			Packet.Parent.QueuePosition = 0;
			Packet.Parent.QueueTime = 0;
			Packet.Parent.Commit();

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
						InitializeWriting();

						using (var reader = new BinaryReader(stream))
						{
							Int64 missing = MaxData;
							Int64 max = Settings.Default.DownloadPerReadBytes;
							byte[] data = null;

							// start watch to look if our connection is still receiving data
							StartWatch(Settings.Default.DownloadTimeoutTime, IP + ":" + Port);

							int failCounter = 0;
							do
							{
								data = reader.ReadBytes((int) (missing < max ? missing : max));
								LastContact = DateTime.Now;

								if (data != null && data.Length != 0)
								{
									SaveData(data);
									missing -= data.Length;
								}
								else
								{
									failCounter++;
									_log.Warn("StartRun() no data received - " + failCounter);

									if (failCounter > Settings.Default.MaxNoDateReceived)
									{
										_log.Warn("StartRun() no data received - skipping");
										break;
									}
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
					_log.Info("StartRun() finishing");
					FinishWriting();

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

		#region CONNECTION

		protected void InitializeWriting()
		{
			ConnectionStarted = DateTime.Now;

			_speedCalcTime = DateTime.Now;
			_speedCalcSize = 0;
			_receivedBytes = 0;

			File = FileActions.GetFileOrCreateNew(Packet.RealName, Packet.RealSize);
			if (File == null)
			{
				_log.Warn("InitializeWriting(" + Packet + ") cant find or create a file to download");
				_tcpClient.Close();
				return;
			}
			if (File.Connected)
			{
				_log.Warn("InitializeWriting(" + Packet + ") file already downloading");
				File = null;
				_tcpClient.Close();
				return;
			}

			// wtf?
			if (StartSize == File.Size)
			{
				_log.Error("InitializeWriting(" + Packet + ") startSize = File.Size (" + StartSize + ")");
				_tcpClient.Close();
				return;
			}

			File.Connected = true;
			File.Packet = Packet;

			_log.Info("InitializeWriting(" + Packet + ") started (" + StartSize + " - " + File.Size + ")");

			try
			{
				var stream = new FileStream(Settings.Default.TempPath + File.TmpName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

				// we are connected
				if (OnConnected != null)
				{
					OnConnected(this, new EventArgs<Packet>(Packet));
				}

				// we seek if it is necesarry
				if (StartSize > 0)
				{
					try
					{
						_reader = new BinaryReader(stream);

						// seek to seekPos and extract the rollbackcheck bytes
						stream.Seek(StartSize, SeekOrigin.Begin);
						_rollbackRefernce = _reader.ReadBytes(Settings.Default.FileRollbackCheckBytes);

						// seek back
						stream.Seek(StartSize, SeekOrigin.Begin);
					}
					catch (Exception ex)
					{
						_log.Fatal("InitializeWriting(" + Packet + ") seek", ex);
						_tcpClient.Close();
						return;
					}
				}
				else
				{
					_streamOk = true;
				}

				_writer = new BinaryWriter(stream);

				#region EMIT CHANGES

				File.Commit();

				Packet.Connected = true;
				Packet.File = File;
				Packet.Commit();

				Packet.Parent.State = Bot.States.Active;
				Packet.Parent.Commit();

				#endregion
			}
			catch (Exception ex)
			{
				_log.Fatal("InitializeWriting(" + Packet + ")", ex);
				_tcpClient.Close();
				return;
			}

			FireNotificationAdded(Notification.Types.BotConnected, Packet);
		}

		protected void FinishWriting()
		{
			_log.Info("FinishWriting()");
			ConnectionStopped = DateTime.Now;

			Stopwatch();

			// close the writer
			if (_writer != null)
			{
				_writer.Close();
			}

			Packet.Connected = false;
			Packet.File = null;
			Packet.Commit();

			Packet.Parent.State = Bot.States.Idle;
			Packet.Parent.HasNetworkProblems = false;
			Packet.Parent.Commit();

			if (File != null)
			{
				File.Packet = null;
				File.Connected = false;
				File.Speed = 0;
				File.Commit();

				if (_removeFile)
				{
					_log.Info("FinishWriting(" + Packet + ") removing file");
					Files.Remove(File);
				}
				else
				{
					// the file is ok if the size is equal or it has an additional buffer for checking
					if (CurrentSize == File.Size)
					{
						_log.Info("FinishWriting(" + Packet + ") ready");
						FireNotificationAdded(Notification.Types.PacketCompleted, Packet);
					}
					// that should not happen
					else if (CurrentSize > File.Size)
					{
						_log.Error("FinishWriting(" + Packet + ") size is bigger than excepted: " + CurrentSize + " > " + File.Size);
						// lets remove the file and load the package again
						Files.Remove(File);
						_log.Error("FinishWriting(" + Packet + ") removing corupted " + File);

						FireNotificationAdded(Notification.Types.PacketBroken, Packet);
					}
					// it did not start
					else if (_receivedBytes == 0)
					{
						_log.Error("FinishWriting(" + Packet + ") downloading did not start, disabling packet");
						Packet.Enabled = false;

						Packet.Parent.HasNetworkProblems = true;
						Packet.Parent.Commit();

						FireNotificationAdded(Notification.Types.BotConnectFailed, Packet);
					}
					// it is incomplete
					else
					{
						_log.Error("FinishWriting(" + Packet + ") incomplete");

						FireNotificationAdded(Notification.Types.PacketIncomplete, Packet);
					}
				}
			}
			// the connection didnt even connected to the given ip and port
			else
			{
				// lets disable the packet, because the bot seems to have broken config or is firewalled
				_log.Error("FinishWriting(" + Packet + ") connection did not work, disabling packet");
				Packet.Enabled = false;

				Packet.Parent.HasNetworkProblems = true;
				Packet.Parent.Commit();

				FireNotificationAdded(Notification.Types.BotConnectFailed, Packet);
			}

			if (OnDisconnected != null)
			{
				OnDisconnected(this, new EventArgs<Packet>(Packet));
			}
		}

		void EnabledChanged(object aSender, EventArgs<AObject> aEventArgs)
		{
			if (_tcpClient != null && !aEventArgs.Value1.Enabled)
			{
				_removeFile = true;
				_tcpClient.Close();
			}
		}

		void SaveData(byte[] aData)
		{
			#region ROLLBACKCHECK

			if (!_streamOk)
			{
				// intial data
				if (_startBuffer == null)
				{
					_startBuffer = aData;
				}
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = _startBuffer.Length;
					Array.Resize(ref _startBuffer, bL + dL);
					Array.Copy(aData, 0, _startBuffer, bL, dL);
				}

				int refL = _rollbackRefernce.Length;
				int bufL = _startBuffer.Length;
				// we have enough data so check them
				if (refL <= bufL)
				{
					// all ok
					if (_rollbackRefernce.IsEqualWith(_startBuffer))
					{
						_log.Info("SaveData(" + Packet + ") rollback check ok");
						aData = _startBuffer;
						_startBuffer = null;
						_streamOk = true;
					}
					// data mismatch
					else
					{
						_log.Error("SaveData(" + Packet + ") rollback check failed");
						FireNotificationAdded(Notification.Types.PacketFileMismatch, Packet, File);

						// unregister from the event because if this is triggered
						// it will remove the part
						Packet.OnEnabledChanged -= EnabledChanged;
						Packet.Enabled = false;
						_tcpClient.Close();

						return;
					}
				}
				// some data is missing, so wait for more
				else
				{
					return;
				}
			}

			#endregion

			try
			{
				_writer.Write(aData);
				_writer.Flush();
				_receivedBytes += aData.Length;
				_speedCalcSize += aData.Length;
				File.CurrentSize += aData.Length;
			}
			catch (Exception ex)
			{
				_log.Fatal("SaveData(" + Packet + ") write", ex);
				_streamOk = false;
				_tcpClient.Close();
				return;
			}

			// update part speed
			if ((DateTime.Now - _speedCalcTime).TotalSeconds > Settings.Default.UpdateDownloadTime)
			{
				DateTime old = _speedCalcTime;
				_speedCalcTime = DateTime.Now;
				File.Speed = Convert.ToInt64(_speedCalcSize / (_speedCalcTime - old).TotalSeconds);

				File.Commit();
				_speedCalcSize = 0;
			}
		}

		#endregion
	}
}
