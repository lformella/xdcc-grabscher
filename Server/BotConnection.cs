// 
//  BotConnection.cs
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
using System.Reflection;

using XG.Core;
using XG.Server.Connection;

using log4net;

namespace XG.Server
{
	/// <summary>
	/// 	This class describes the connection to a single irc bot
	/// 	it does the following things
	/// 	- receiving all data comming from the bot
	/// 	- writing the data into the file
	/// 	- checking if the data matches the given file (rollback check)
	/// </summary>
	public class BotConnection : AIrcConnection
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		BinaryWriter _writer;
		BinaryReader _reader;

		Int64 _receivedBytes;
		DateTime _speedCalcTime;
		Int64 _speedCalcSize;

		byte[] _rollbackRefernce;
		byte[] _startBuffer;
		byte[] _stopBuffer;

		bool _streamOk;
		public bool RemovePart { get; set; }

		Packet _packet;

		public Packet Packet
		{
			get { return _packet; }
			set
			{
				if (_packet != null)
				{
					_packet.EnabledChanged -= EnabledChanged;
				}
				_packet = value;
				if (_packet != null)
				{
					_packet.EnabledChanged += EnabledChanged;
				}
			}
		}

		public Int64 StartSize { get; set; }

		Int64 CurrrentSize
		{
			get { return StartSize + _receivedBytes; }
		}

		Int64 StopSize
		{
			get { return Part.StopSize; }
			set
			{
				Part.StopSize = value;
				Log.Info("StopSize.set(" + value + ")");
				Part.Commit();
			}
		}

		public FilePart Part { get; set; }

		Core.File File
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
				Log.Error("Filename find no part or file");
				return "";
			}
		}

		#endregion

		#region EVENTS

		public event PacketDelegate Connected;
		public event PacketDelegate Disconnected;

		#endregion

		#region CONNECT

		protected override void ConnectionConnected()
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
				Log.Fatal("ConnectionConnected(" + Packet + ") cant find or create a file to download");
				Connection.Disconnect();
				return;
			}

			Part = FileActions.Part(tFile, StartSize);
			if (Part != null)
			{
				// wtf?
				if (StartSize == StopSize)
				{
					Log.Error("ConnectionConnected(" + Packet + ") startSize = stopsize (" + StartSize + ")");
					Connection.Disconnect();
					return;
				}

				Part.State = FilePart.States.Open;
				Part.Packet = Packet;

				Log.Info("ConnectionConnected(" + Packet + ") started (" + StartSize + " - " + StopSize + ")");

#if !UNSAFE
				try
				{
#endif
					var info = new FileInfo(FileName);
					FileStream stream = info.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

					// we are connected
					if (Connected != null)
					{
						Connected(Packet);
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
							Log.Fatal("ConnectionConnected(" + Packet + ") seek", ex);
							Connection.Disconnect();
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
					Log.Fatal("ConnectionConnected(" + Packet + ")", ex);
					Connection.Disconnect();
					return;
				}
#endif

				FireNotificationAdded(new Notification(Notification.Types.BotConnected, Packet));
			}
			else
			{
				Log.Error("ConnectionConnected(" + Packet + ") cant find a part to download");
				Connection.Disconnect();
			}
		}

		#endregion

		#region DISCONNECT

		protected override void ConnectionDisconnected(SocketErrorCode aValue)
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

				if (RemovePart)
				{
					Log.Info("ConnectionDisconnected(" + Packet + ") removing part");
					Part.State = FilePart.States.Broken;
					FileActions.RemovePart(File, Part);
				} else
				{
					// the file is ok if the size is equal or it has an additional buffer for checking
					if (CurrrentSize == StopSize || (!Part.Checked && CurrrentSize == StopSize + Settings.Instance.FileRollbackCheckBytes))
					{
						Part.State = FilePart.States.Ready;
						Log.Info("ConnectionDisconnected(" + Packet + ") ready" + (Part.Checked ? "" : " but unchecked"));

						FireNotificationAdded(new Notification(Notification.Types.PacketCompleted, Packet));
					}
					// that should not happen
					else if (CurrrentSize > StopSize)
					{
						Part.State = FilePart.States.Broken;
						Log.Error("ConnectionDisconnected(" + Packet + ") size is bigger than excepted: " + CurrrentSize + " > " + StopSize);
						// this mostly happens on the last part of a file - so lets remove the file and load the package again
						if (File.Parts.Count() == 1 || Part.StopSize == File.Size)
						{
							FileActions.RemoveFile(File);
							Log.Error("ConnectionDisconnected(" + Packet + ") removing corupted " + File);
						}

						FireNotificationAdded(new Notification(Notification.Types.PacketBroken, Packet));
					}
					// it did not start
					else if (_receivedBytes == 0)
					{
						Log.Error("ConnectionDisconnected(" + Packet + ") downloading did not start, disabling packet");
						Packet.Enabled = false;
						Packet.Parent.HasNetworkProblems = true;

						FireNotificationAdded(new Notification(Notification.Types.BotConnectFailed, Packet.Parent));
					}
					// it is incomplete
					else
					{
						Log.Error("ConnectionDisconnected(" + Packet + ") incomplete");

						FireNotificationAdded(new Notification(Notification.Types.PacketIncompleted, Packet));
					}
				}
			}
			// the connection didnt even connected to the given ip and port
			else
			{
				// lets disable the packet, because the bot seems to have broken config or is firewalled
				Log.Error("ConnectionDisconnected(" + Packet + ") connection did not work, disabling packet");
				Packet.Enabled = false;
				Packet.Parent.HasNetworkProblems = true;

				FireNotificationAdded(new Notification(Notification.Types.BotConnectFailed, Packet.Parent));
			}

			if (Part != null)
			{
				Part.Commit();
			}
			Packet.Parent.Commit();
			
			if (Disconnected != null)
			{
				Disconnected(Packet);
			}
		}

		void EnabledChanged(AObject aObj)
		{
			if (!aObj.Enabled)
			{
				RemovePart = true;
				Connection.Disconnect();
			}
		}

		#endregion

		#region DATA

		protected override void ConnectionDataReceived(byte[] aData)
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
						Log.Info("ConnectionDataReceived(" + Packet + ") rollback check ok");
						aData = _startBuffer;
						_startBuffer = null;
						_streamOk = true;
					}
						// data mismatch
					else
					{
						Log.Error("ConnectionDataReceived(" + Packet + ") rollback check failed");

						// unregister from the event because if this is triggered
						// it will remove the part
						Packet.EnabledChanged -= EnabledChanged;
						Packet.Enabled = false;
						Connection.Disconnect();
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
						Connection.Disconnect();
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
							Log.Info("ConnectionDataReceived(" + Packet + ") reference check ok");
							Connection.Disconnect();
							return;
						}
						// data mismatch
						else
						{
							Log.Error("ConnectionDataReceived(" + Packet + ") reference check failed");
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

						Connection.Disconnect();
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

				// statistics
				//Statistic.Instance.Increase(StatisticType.BytesLoaded, aData.Length);
			}
			catch (Exception ex)
			{
				Log.Fatal("ConnectionDataReceived(" + Packet + ") write", ex);
				_streamOk = false;
				Connection.Disconnect();
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

				// statistics
				/*if (Part.Speed > Statistic.Instance.Get(StatisticType.SpeedMax))
				{
					Statistic.Instance.Set(StatisticType.SpeedMax, Part.Speed);
				}*/
			}
		}

		#endregion
	}
}
