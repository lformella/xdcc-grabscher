// 
//  BotConnection.cs
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

using log4net;

using XG.Core;
using XG.Server.Connection;

namespace XG.Server
{
	public delegate void PacketBotConnectDelegate(Packet aPack, BotConnection aCon);

	/// <summary>
	/// This class describes the connection to a single irc bot
	/// it does the following things
	/// - receiving all data comming from the bot
	/// - writing the data into the file
	/// - checking if the data matches the given file (rollback check)
	/// </summary>	
	public class BotConnection : AIrcConnection
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(BotConnection));

		BinaryWriter _writer;
		BinaryReader _reader;

		Int64 _receivedBytes;
		DateTime _speedCalcTime;
		Int64 _speedCalcSize;

		byte[] _rollbackRefernce = null;
		byte[] _startBuffer = null;
		byte[] _stopBuffer = null;

		bool _streamOk = false;
		public bool RemovePart { get; set; }
		
		Packet _packet;
		public Packet Packet
		{
			get
			{
				return _packet;
			}
			set
			{
				if(_packet != null)
				{
					_packet.EnabledChanged -= new ObjectDelegate(EnabledChanged);
				}
				_packet = value;
				if(_packet != null)
				{
					_packet.EnabledChanged += new ObjectDelegate(EnabledChanged);
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
				else { return ""; }
			}
		}

		#endregion

		#region EVENTS

		public event PacketBotConnectDelegate Connected;
		public event PacketBotConnectDelegate Disconnected;

		#endregion

		#region CONNECT

		protected override void ConnectionConnected()
		{
			_speedCalcTime = DateTime.Now;
			_speedCalcSize = 0;
			_receivedBytes = 0;

			Core.File File = FileActions.NewFile(Packet.RealName, Packet.RealSize);
			if (File == null)
			{
				_log.Fatal("ConnectionConnected() cant find or create a file to download");
				Connection.Disconnect();
				return;
			}

			Part = FileActions.Part(File, StartSize);
			if (Part != null)
			{
				// wtf?
				if (StartSize == StopSize)
				{
					_log.Error("ConnectionConnected() startSize = stopsize (" + StartSize + ")");
					Connection.Disconnect();
					return;
				}

				Part.State = FilePart.States.Open;
				Part.Packet = Packet;

				_log.Info("ConnectionConnected() startet (" + StartSize + " - " + StopSize + ")");

#if !UNSAFE
				try
				{
#endif
					FileInfo info = new FileInfo(FileName);
					FileStream stream = info.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

					// we are connected
					Connected(Packet, this);

					// we seek if it is possible
					Int64 seekPos = StartSize - Part.StartSize;
					if (seekPos > 0)
					{
						try
						{
							_reader = new BinaryReader(stream);

							// seek to 0 and extract the startbuffer bytes need for the previous file
							stream.Seek(0, SeekOrigin.Begin);
							Part.StartReference = _reader.ReadBytes((int)Settings.Instance.FileRollbackCheck);

							// seek to seekPos and extract the rollbackcheck bytes
							stream.Seek(seekPos, SeekOrigin.Begin);
							_rollbackRefernce = _reader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
							// seek back
							stream.Seek(seekPos, SeekOrigin.Begin);
						}
						catch (Exception ex)
						{
							_log.Fatal("ConnectionConnected() seek", ex);
							Connection.Disconnect();
							return;
						}
					}
					else { _streamOk = true; }

					_writer = new BinaryWriter(stream);

					#region EMIT CHANGES

					Part.Commit();

					Packet.Connected = true;
					Packet.Part = Part;
					Packet.Commit();

					Packet.Parent.State = Bot.States.Active;
					Packet.Parent.QueuePosition = 0;
					Packet.Parent.QueueTime = 0;
					Packet.Parent.Commit();

					#endregion
#if !UNSAFE
				}
				catch (Exception ex)
				{
					_log.Fatal("ConnectionConnected()", ex);
					Connection.Disconnect();
					return;
				}
#endif

				// statistics
				Statistic.Instance.Increase(StatisticType.BotConnectsOk);
			}
			else
			{
				_log.Error("ConnectionConnected() cant find a part to download");
				Connection.Disconnect();
			}
		}

		#endregion

		#region DISCONNECT

		protected override void ConnectionDisconnected(SocketErrorCode aValue)
		{
			// close the writer
			if (_writer != null) { _writer.Close(); }

			Packet.Connected = false;
			Packet.Part = null;
			Packet.Commit();

			Packet.Parent.State = Bot.States.Idle;
			Packet.Parent.Commit();

			if (Part != null)
			{
				Part.Packet = null;
				Part.State = FilePart.States.Closed;

				if (RemovePart)
				{
					Part.State = FilePart.States.Broken;
					FileActions.RemovePart(File, Part);
				}
				else
				{
					// the file is ok if the size is equal or it has an additional buffer for checking
					if (CurrrentSize == StopSize || (!Part.Checked && CurrrentSize == StopSize + Settings.Instance.FileRollbackCheck))
					{
						Part.State = FilePart.States.Ready;
						_log.Info("ConnectionDisconnected() ready" + (Part.Checked ? "" : " but unchecked"));

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsCompleted);
					}
					// that should not happen
					else if (CurrrentSize > StopSize)
					{
						Part.State = FilePart.States.Broken;
						_log.Error("ConnectionDisconnected() size is bigger than excepted: " + CurrrentSize + " > " + StopSize);
						// this mostly happens on the last part of a file - so lets remove the file and load the package again
						if (File.Parts.Count() == 1 || Part.StopSize == File.Size)
						{
							FileActions.RemoveFile(File);
							_log.Error("ConnectionDisconnected() removing corupted file " + File.Name);
						}

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsBroken);
					}
					// it did not start
					else if (_receivedBytes == 0)
					{
						_log.Error("ConnectionDisconnected() downloading did not start, disabling packet");
						Packet.Enabled = false;

						// statistics
						Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
					}
					// it is incomplete
					else
					{
						_log.Error("ConnectionDisconnected() incomplete");

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsIncompleted);
					}
				}
				Part.Commit();
			}
			// the connection didnt even connected to the given ip and port
			else
			{
				// lets disable the packet, because the bot seems to have broken config or is firewalled
				_log.Error("ConnectionDisconnected() connection did not work, disabling packet");
				Packet.Enabled = false;

				// statistics
				Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
			}

			Disconnected(Packet, this);
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
				if (_startBuffer == null) { _startBuffer = aData; }
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
					if (XG.Core.Helper.IsEqual(_rollbackRefernce, _startBuffer))
					{
						_log.Info("con_DataReceived() rollback check ok");
						aData = _startBuffer;
						_startBuffer = null;
						_streamOk = true;
					}
					// data mismatch
					else
					{
						_log.Error("con_DataReceived() rollback check failed");

						// unregister from the event because if this is triggered
						// it will remove the part
						Packet.EnabledChanged -= new ObjectDelegate(EnabledChanged);
						Packet.Enabled = false;
						aData = new byte[0];
						Connection.Disconnect();
						return;
					}
				}
				// some data is missing, so wait for more
				else { return; }
			}
			// save the reference bytes if it is a new file
			else if (Part.StartReference == null || Part.StartReference.Length < (int)Settings.Instance.FileRollbackCheck)
			{
				byte[] startReference = Part.StartReference;
				// initial data
				if (startReference == null) { startReference = aData; }
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = startReference.Length;
					Array.Resize(ref startReference, bL + dL);
					Array.Copy(aData, 0, startReference, bL, dL);
				}
				// shrink the reference if it is to big
				if (startReference.Length > Settings.Instance.FileRollbackCheck)
				{
					Array.Resize(ref startReference, (int)Settings.Instance.FileRollbackCheck);
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
					Array.Resize(ref aData, (int)length);
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
				if (Settings.Instance.FileRollbackCheck <= bufL)
				{
					// but only if we are checked
					if (Part.Checked)
					{
						Int64 stopSize = FileActions.CheckNextReferenceBytes(Part, _stopBuffer);
						// all ok
						if (stopSize == 0)
						{
							_log.Info("con_DataReceived() reference check ok");
							aData = new byte[0];
							Connection.Disconnect();
							return;
						}
						// data mismatch
						else
						{
							_log.Error("con_DataReceived() reference check failed");
							aData = _stopBuffer;
							StopSize = stopSize;
						}
					}
					// we are unchecked, so just close
					else
					{
						// shrink the buffer if it is to big
						if (_stopBuffer.Length > Settings.Instance.FileRollbackCheck)
						{
							Array.Resize(ref _stopBuffer, (int)Settings.Instance.FileRollbackCheck);
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
				else if (!initial) { return; }
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
				Statistic.Instance.Increase(StatisticType.BytesLoaded, aData.Length);
			}
			catch (Exception ex)
			{
				_log.Fatal("con_DataReceived() write", ex);
				_streamOk = false;
				Connection.Disconnect();
				return;
			}

			// update all listeners with new values and a calculated speed
			if ((DateTime.Now - _speedCalcTime).TotalMilliseconds > Settings.Instance.UpdateDownloadTime)
			{
				DateTime old = _speedCalcTime;
				_speedCalcTime = DateTime.Now;
				Part.Speed = (_speedCalcSize / (_speedCalcTime - old).TotalMilliseconds) * 1000;

				Part.Commit();
				_speedCalcSize = 0;

				// statistics
				if(Part.Speed > Statistic.Instance.Get(StatisticType.SpeedMax))
				{
					Statistic.Instance.Set(StatisticType.SpeedMax, Part.Speed);
				}
			}
		}

		#endregion
	}
}
