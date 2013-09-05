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
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

using XG.Core;
using XG.Server.Helper;
using XG.Server.Worker;

using log4net;

namespace XG.Server.Plugin.Core.Irc
{
	public class Download : AWorker
	{
		#region VARIABLES

		ILog _log;

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

		public Int64 StartSize { get; set; }
		public string Hostname { get; set; }
		public int Port { get; set; }
		public Int64 MaxData { get; set; }
		public FileActions FileActions { get; set; }

		TcpClient _tcpClient;
		BinaryWriter _writer;
		BinaryReader _reader;

		Int64 _receivedBytes;
		DateTime _speedCalcTime;
		Int64 _speedCalcSize;

		byte[] _rollbackRefernce;
		byte[] _startBuffer;
		byte[] _stopBuffer;

		bool _streamOk;
		bool _removePart;

		Int64 CurrentSize
		{
			get { return StartSize + _receivedBytes; }
		}

		Int64 StopSize
		{
			get { return Part.StopSize; }
			set
			{
				Part.StopSize = value;
				_log.Info("StopSize.set(" + value + ")");
				Part.Commit();
			}
		}

		public FilePart Part { get; set; }

		XG.Core.File File
		{
			get { return Part.Parent; }
		}

		string FileName
		{
			get
			{
				if (Part != null && File != null)
				{
					return Settings.Instance.TempPath + File.TmpPath + Part.StartSize;
				}
				// damn this should not happen
				_log.Error("Filename find no part or file");
				return "";
			}
		}

		#endregion

		#region EVENTS

		public event PacketDelegate OnConnected;
		public event PacketDelegate OnDisconnected;

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + Hostname + ":" + Port + ")");

			using (_tcpClient = new TcpClient())
			{
				_tcpClient.SendTimeout = Settings.Instance.DownloadTimeoutTime * 1000;
				_tcpClient.ReceiveTimeout = Settings.Instance.DownloadTimeoutTime * 1000;
				//_tcpClient.ReceiveBufferSize = Settings.Instance.DownloadPerReadBytes;

				try
				{
					_tcpClient.Connect(Hostname, Port);
					_log.Info("StartRun() connected");

					using (NetworkStream stream = _tcpClient.GetStream())
					{
						StartWriting();

						using (var reader = new BinaryReader(stream))
						{
							Int64 missing = MaxData;
							Int64 max = Settings.Instance.DownloadPerReadBytes;
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

				StopWriting();
			}

			_tcpClient = null;
			_writer = null;
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
			_speedCalcTime = DateTime.Now;
			_speedCalcSize = 0;
			_receivedBytes = 0;

			Packet.Parent.QueuePosition = 0;
			Packet.Parent.QueueTime = 0;
			Packet.Parent.Commit();

			var tFile = FileActions.NewFile(Packet.RealName, Packet.RealSize);
			if (tFile == null)
			{
				_log.Fatal("StartWriting(" + Packet + ") cant find or create a file to download");
				_tcpClient.Close();
				return;
			}

			Part = FileActions.Part(tFile, StartSize);
			if (Part != null)
			{
				// wtf?
				if (StartSize == StopSize)
				{
					_log.Error("StartWriting(" + Packet + ") startSize = stopsize (" + StartSize + ")");
					_tcpClient.Close();
					return;
				}

				Part.State = FilePart.States.Open;
				Part.Packet = Packet;

				_log.Info("StartWriting(" + Packet + ") started (" + StartSize + " - " + StopSize + ")");

#if !UNSAFE
				try
				{
#endif
					var info = new FileInfo(FileName);
					FileStream stream = info.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

					// we are connected
					if (OnConnected != null)
					{
						OnConnected(Packet);
					}

					// we seek if it is possible
					Int64 seekPos = StartSize - Part.StartSize;
					if (seekPos > 0)
					{
						try
						{
							_reader = new BinaryReader(stream);

							// seek to 0 and extract the startbuffer bytes need for the previous file
							stream.Seek(0, SeekOrigin.Begin);
							Part.StartReference = _reader.ReadBytes(Settings.Instance.FileRollbackCheckBytes);

							// seek to seekPos and extract the rollbackcheck bytes
							stream.Seek(seekPos, SeekOrigin.Begin);
							_rollbackRefernce = _reader.ReadBytes(Settings.Instance.FileRollbackCheckBytes);
							// seek back
							stream.Seek(seekPos, SeekOrigin.Begin);
						}
						catch (Exception ex)
						{
							_log.Fatal("StartWriting(" + Packet + ") seek", ex);
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

					Part.Commit();

					Packet.Connected = true;
					Packet.Part = Part;
					Packet.Commit();

					Packet.Parent.State = Bot.States.Active;
					Packet.Parent.Commit();

					#endregion

#if !UNSAFE
				}
				catch (Exception ex)
				{
					_log.Fatal("StartWriting(" + Packet + ")", ex);
					_tcpClient.Close();
					return;
				}
#endif

				FireNotificationAdded(new Notification(Notification.Types.BotConnected, Packet));
			}
			else
			{
				_log.Error("StartWriting(" + Packet + ") cant find a part to download");
				_tcpClient.Close();
			}
		}

		protected void StopWriting()
		{
			// close the writer
			if (_writer != null)
			{
				_writer.Close();
			}

			Packet.Connected = false;
			Packet.Part = null;
			Packet.Commit();

			Packet.Parent.State = Bot.States.Idle;
			Packet.Parent.Commit();

			Packet.Parent.HasNetworkProblems = false;
			if (Part != null)
			{
				Part.Packet = null;
				Part.State = FilePart.States.Closed;

				if (_removePart)
				{
					_log.Info("StopWriting(" + Packet + ") removing part");
					Part.State = FilePart.States.Broken;
					FileActions.RemovePart(File, Part);
				}
				else
				{
					// the file is ok if the size is equal or it has an additional buffer for checking
					if (CurrentSize == StopSize || (!Part.Checked && CurrentSize == StopSize + Settings.Instance.FileRollbackCheckBytes))
					{
						Part.State = FilePart.States.Ready;
						_log.Info("StopWriting(" + Packet + ") ready" + (Part.Checked ? "" : " but unchecked"));

						FireNotificationAdded(new Notification(Notification.Types.PacketCompleted, Packet));
					}
					// that should not happen
					else if (CurrentSize > StopSize)
					{
						Part.State = FilePart.States.Broken;
						_log.Error("StopWriting(" + Packet + ") size is bigger than excepted: " + CurrentSize + " > " + StopSize);
						// this mostly happens on the last part of a file - so lets remove the file and load the package again
						if (File.Parts.Count() == 1 || Part.StopSize == File.Size)
						{
							FileActions.RemoveFile(File);
							_log.Error("StopWriting(" + Packet + ") removing corupted " + File);
						}

						FireNotificationAdded(new Notification(Notification.Types.PacketBroken, Packet));
					}
					// it did not start
					else if (_receivedBytes == 0)
					{
						_log.Error("StopWriting(" + Packet + ") downloading did not start, disabling packet");
						Packet.Enabled = false;
						Packet.Parent.HasNetworkProblems = true;

						FireNotificationAdded(new Notification(Notification.Types.BotConnectFailed, Packet.Parent));
					}
					// it is incomplete
					else
					{
						_log.Error("StopWriting(" + Packet + ") incomplete");

						FireNotificationAdded(new Notification(Notification.Types.PacketIncompleted, Packet));
					}
				}
			}
			// the connection didnt even connected to the given ip and port
			else
			{
				// lets disable the packet, because the bot seems to have broken config or is firewalled
				_log.Error("StopWriting(" + Packet + ") connection did not work, disabling packet");
				Packet.Enabled = false;
				Packet.Parent.HasNetworkProblems = true;

				FireNotificationAdded(new Notification(Notification.Types.BotConnectFailed, Packet.Parent));
			}

			if (Part != null)
			{
				Part.Commit();
			}
			Packet.Parent.Commit();
			
			if (OnDisconnected != null)
			{
				OnDisconnected(Packet);
			}
		}

		void EnabledChanged(AObject aObj)
		{
			if (!aObj.Enabled)
			{
				_removePart = true;
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
				// save the reference bytes if it is a new file
			else if (Part.StartReference == null || Part.StartReference.Length < Settings.Instance.FileRollbackCheckBytes)
			{
				byte[] startReference = Part.StartReference;
				// initial data
				if (startReference == null)
				{
					startReference = aData;
				}
					// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = startReference.Length;
					Array.Resize(ref startReference, bL + dL);
					Array.Copy(aData, 0, startReference, bL, dL);
				}
				// shrink the reference if it is to big
				if (startReference.Length > Settings.Instance.FileRollbackCheckBytes)
				{
					Array.Resize(ref startReference, Settings.Instance.FileRollbackCheckBytes);
				}
				Part.StartReference = startReference;
			}

			#endregion

			#region NEXT REFERENCE CHECK

			//    stop     needed refbytes
			// ----------~~~~~~~~~~~~~~~~~~~
			// -------~~~~~~~~
			//   cur    data
			if (StopSize < Packet.RealSize && StopSize < StartSize + _receivedBytes + aData.Length)
			{
				bool initial = false;
				// intial data
				if (_stopBuffer == null)
				{
					initial = true;
					Int64 length = StopSize - (StartSize + _receivedBytes);
					if (length < 0)
					{
						// this is bad and should not happen
						length = 0;
						_tcpClient.Close();
					}
					_stopBuffer = new byte[aData.Length - length];
					// copy the overlapping data into the buffer
					Array.Copy(aData, length, _stopBuffer, 0, aData.Length - length);
					// and shrink the actual data
					Array.Resize(ref aData, (int) length);
				}
					// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = _stopBuffer.Length;
					Array.Resize(ref _stopBuffer, bL + dL);
					Array.Copy(aData, 0, _stopBuffer, bL, dL);
				}

				int bufL = _stopBuffer.Length;
				// we have enough data so check them
				if (Settings.Instance.FileRollbackCheckBytes <= bufL)
				{
					// but only if we are checked
					if (Part.Checked)
					{
						Int64 stopSize = FileActions.CheckNextReferenceBytes(Part, _stopBuffer);
						// all ok
						if (stopSize == 0)
						{
							_log.Info("SaveData(" + Packet + ") reference check ok");
							_tcpClient.Close();
							return;
						}
						// data mismatch
						else
						{
							_log.Error("SaveData(" + Packet + ") reference check failed");
							aData = _stopBuffer;
							StopSize = stopSize;
						}
					}
						// we are unchecked, so just close
					else
					{
						// shrink the buffer if it is to big
						if (_stopBuffer.Length > Settings.Instance.FileRollbackCheckBytes)
						{
							Array.Resize(ref _stopBuffer, Settings.Instance.FileRollbackCheckBytes);
						}
						// and write it to file to be able to check the next file
						_writer.Write(_stopBuffer);
						_writer.Flush();
						_receivedBytes += _stopBuffer.Length;

						_tcpClient.Close();
						return;
					}
				}
					// the splitted data must be written to the file of course
					// but if some data is missing wait for more
				else if (!initial)
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
				Part.CurrentSize += aData.Length;
			}
			catch (Exception ex)
			{
				_log.Fatal("SaveData(" + Packet + ") write", ex);
				_streamOk = false;
				_tcpClient.Close();
				return;
			}

			// update part speed
			if ((DateTime.Now - _speedCalcTime).TotalSeconds > Settings.Instance.UpdateDownloadTime)
			{
				DateTime old = _speedCalcTime;
				_speedCalcTime = DateTime.Now;
				Part.Speed = Convert.ToInt64(_speedCalcSize / (_speedCalcTime - old).TotalSeconds);

				Part.Commit();
				_speedCalcSize = 0;
			}
		}

		#endregion
	}
}
