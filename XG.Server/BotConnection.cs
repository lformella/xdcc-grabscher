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
using System.Linq;
using System.Net;
using log4net;
using XG.Core;
using XG.Server.Connection;

namespace XG.Server
{
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

		private static readonly ILog log = LogManager.GetLogger(typeof(BotConnection));

		private BinaryWriter writer;
		private BinaryReader reader;

		private Int64 receivedBytes;
		private DateTime speedCalcTime;
		private Int64 speedCalcSize;

		private byte[] startReference = null;
		private byte[] rollbackRefernce = null;
		private byte[] startBuffer = null;
		private byte[] stopBuffer = null;

		private bool streamOk = false;
		public bool RemovePart { get; set; }
		
		private XGPacket packet;
		public XGPacket Packet
		{
			get
			{
				return this.packet;
			}
			set
			{
				if(this.packet != null)
				{
					this.packet.EnabledChangedEvent -= new ObjectDelegate(Packet_ObjectStateChanged);
				}
				this.packet = value;
				if(this.packet != null)
				{
					this.packet.EnabledChangedEvent += new ObjectDelegate(Packet_ObjectStateChanged);
				}
			}
		}

		public Int64 StartSize { get; set; }

		private Int64 CurrrentSize
		{
			get { return this.StartSize + this.receivedBytes; }
		}

		private Int64 StopSize
		{
			get { return this.Part.StopSize; }
			set
			{
				this.Part.StopSize = value;
				log.Info("StopSize.set(" + value + ")");
				this.Part.Commit();
			}
		}

		public XGFilePart Part { get; set; }
		private XGFile File
		{
			get { return Part.Parent; }
		}

		private string FileName
		{
			get
			{
				if (this.Part != null && this.File != null)
				{
					return Settings.Instance.TempPath + this.File.TmpPath + this.Part.StartSize;
				}
				// damn this should not happen
				else { return ""; }
			}
		}

		public byte[] ReferenceBytes
		{
			get { return this.startReference; }
		}

		#endregion

		#region EVENTS

		public event PacketBotConnectDelegate ConnectedEvent;
		public event PacketBotConnectDelegate DisconnectedEvent;

		#endregion

		#region CONNECT

		protected override void Connection_ConnectedEventHandler()
		{
			this.speedCalcTime = DateTime.Now;
			this.speedCalcSize = 0;
			this.receivedBytes = 0;

			this.Part = this.Parent.GetPart(this.Parent.GetNewFile(this.Packet.RealName, this.Packet.RealSize), this.StartSize);
			if (this.Part != null)
			{
				// wtf?
				if (this.StartSize == this.StopSize)
				{
					log.Error("con_Connected() startSize = stopsize (" + this.StartSize + ")");
					this.Connection.Disconnect();
					return;
				}

				this.Part.PartState = FilePartState.Open;
				this.Part.Packet = this.Packet;

				log.Info("con_Connected() startet (" + this.StartSize + " - " + this.StopSize + ")");

#if !UNSAFE
				try
				{
#endif
					FileInfo info = new FileInfo(this.FileName);
					FileStream stream = info.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

					// we are connected
					this.ConnectedEvent(this.Packet, this);

					// we seek if it is possible
					Int64 seekPos = this.StartSize - this.Part.StartSize;
					if (seekPos > 0)
					{
						try
						{
							this.reader = new BinaryReader(stream);

							// seek to 0 and extract the startbuffer bytes need for the previous file
							stream.Seek(0, SeekOrigin.Begin);
							this.startReference = this.reader.ReadBytes((int)Settings.Instance.FileRollbackCheck);

							// seek to seekPos and extract the rollbackcheck bytes
							stream.Seek(seekPos, SeekOrigin.Begin);
							this.rollbackRefernce = this.reader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
							// seek back
							stream.Seek(seekPos, SeekOrigin.Begin);
						}
						catch (Exception ex)
						{
							log.Fatal("con_Connected() seek", ex);
							this.Connection.Disconnect();
							return;
						}
					}
					else { this.streamOk = true; }

					this.writer = new BinaryWriter(stream);

					#region EMIT CHANGES

					this.Part.Commit();

					this.Packet.Connected = true;
					this.Packet.Part = this.Part;
					this.Packet.Commit();

					this.Packet.Parent.BotState = BotState.Active;
					this.Packet.Parent.QueuePosition = 0;
					this.Packet.Parent.QueueTime = 0;
					this.Packet.Parent.Commit();

					#endregion
#if !UNSAFE
				}
				catch (Exception ex)
				{
					log.Fatal("con_Connected()", ex);
					this.Connection.Disconnect();
					return;
				}
#endif

				// statistics
				Statistic.Instance.Increase(StatisticType.BotConnectsOk);
			}
			else
			{
				log.Error("con_Connected() cant find a part to download");
				this.Connection.Disconnect();
			}
		}

		#endregion

		#region DISCONNECT

		protected override void Connection_DisconnectedEventHandler(SocketErrorCode aValue)
		{
			// close the writer
			if (this.writer != null) { this.writer.Close(); }

			this.Packet.Connected = false;
			this.Packet.Part = null;
			this.Packet.Commit();

			this.Packet.Parent.BotState = BotState.Idle;
			this.Packet.Parent.Commit();

			if (this.Part != null)
			{
				this.Part.Packet = null;
				this.Part.PartState = FilePartState.Closed;

				if (this.RemovePart)
				{
					this.Part.PartState = FilePartState.Broken;
					this.Parent.RemovePart(this.File, this.Part);
				}
				else
				{
					// the file is ok if the size is equal or it has an additional buffer for checking
					if (this.CurrrentSize == this.StopSize || (!this.Part.IsChecked && this.CurrrentSize == this.StopSize + Settings.Instance.FileRollbackCheck))
					{
						this.Part.PartState = FilePartState.Ready;
						log.Info("con_Disconnected() ready" + (this.Part.IsChecked ? "" : " but unchecked"));

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsCompleted);
					}
					// that should not happen
					else if (this.CurrrentSize > this.StopSize)
					{
						this.Part.PartState = FilePartState.Broken;
						log.Error("con_Disconnected() size is bigger than excepted: " + this.CurrrentSize + " > " + this.StopSize);
						// this mostly happens on the last part of a file - so lets remove the file and load the package again
						if (this.File.Parts.Count() == 1 || this.Part.StopSize == this.File.Size)
						{
							this.Parent.RemoveFile(this.File);
							log.Error("con_Disconnected() removing corupted file " + this.File.Name);
						}

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsBroken);
					}
					// it did not start
					else if (this.receivedBytes == 0)
					{
						log.Error("con_Disconnected() downloading did not start, disabling packet");
						this.Packet.Enabled = false;

						// statistics
						Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
					}
					// it is incomplete
					else
					{
						log.Error("con_Disconnected() incomplete");

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsIncompleted);
					}
				}
				this.Part.Commit();
			}
			// the connection didnt even connected to the given ip and port
			else
			{
				// lets disable the packet, because the bot seems to have broken config or is firewalled
				log.Error("con_Disconnected() connection did not work, disabling packet");
				this.Packet.Enabled = false;

				// statistics
				Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
			}

			this.DisconnectedEvent(this.Packet, this);
		}

		void Packet_ObjectStateChanged(XGObject aObj)
		{
			if (!aObj.Enabled)
			{
				this.RemovePart = true;
				this.Connection.Disconnect();
			}
		}

		#endregion

		#region DATA
		
		protected override void Connection_DataReceivedEventHandler(string aData)
		{
		}

		protected override void Connection_DataReceivedEventHandler(byte[] aData)
		{
			#region ROLLBACKCHECK

			if (!this.streamOk)
			{
				// intial data
				if (this.startBuffer == null) { this.startBuffer = aData; }
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = this.startBuffer.Length;
					Array.Resize(ref this.startBuffer, bL + dL);
					Array.Copy(aData, 0, this.startBuffer, bL, dL);
				}

				int refL = this.rollbackRefernce.Length;
				int bufL = this.startBuffer.Length;
				// we have enough data so check them
				if (refL <= bufL)
				{
					// all ok
					if (XGHelper.IsEqual(this.rollbackRefernce, this.startBuffer))
					{
						log.Info("con_DataReceived() rollback check ok");
						aData = this.startBuffer;
						this.startBuffer = null;
						this.streamOk = true;
					}
					// data mismatch
					else
					{
						log.Error("con_DataReceived() rollback check failed");

						// unregister from the event because if this is triggered
						// it will remove the part
						this.Packet.EnabledChangedEvent -= new ObjectDelegate(Packet_ObjectStateChanged);
						this.Packet.Enabled = false;
						aData = new byte[0];
						this.Connection.Disconnect();
						return;
					}
				}
				// some data is missing, so wait for more
				else { return; }
			}
			// save the reference bytes if it is a new file
			else if (this.startReference == null || this.startReference.Length < (int)Settings.Instance.FileRollbackCheck)
			{
				// initial data
				if (this.startReference == null) { this.startReference = aData; }
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = this.startReference.Length;
					Array.Resize(ref this.startReference, bL + dL);
					Array.Copy(aData, 0, this.startReference, bL, dL);
				}
				// shrink the reference if it is to big
				if (this.startReference.Length > Settings.Instance.FileRollbackCheck)
				{
					Array.Resize(ref this.startReference, (int)Settings.Instance.FileRollbackCheck);
				}
			}

			#endregion

			#region NEXT REFERENCE CHECK

			//    stop     needed refbytes
			// ----------~~~~~~~~~~~~~~~~~~~
			// -------~~~~~~~~
			//   cur    data
			if (this.StopSize < this.Packet.RealSize && this.StopSize < this.StartSize + this.receivedBytes + aData.Length)
			{
				bool initial = false;
				// intial data
				if (this.stopBuffer == null)
				{
					initial = true;
					Int64 length = this.StopSize - (this.StartSize + this.receivedBytes);
					if (length < 0)
					{
						// this is bad and should not happen	  
						length = 0;
						this.Connection.Disconnect();
					}
					this.stopBuffer = new byte[aData.Length - length];
					// copy the overlapping data into the buffer
					Array.Copy(aData, length, this.stopBuffer, 0, aData.Length - length);
					// and shrink the actual data
					Array.Resize(ref aData, (int)length);
				}
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = this.stopBuffer.Length;
					Array.Resize(ref this.stopBuffer, bL + dL);
					Array.Copy(aData, 0, this.stopBuffer, bL, dL);
				}

				int bufL = this.stopBuffer.Length;
				// we have enough data so check them
				if (Settings.Instance.FileRollbackCheck <= bufL)
				{
					// but only if we are checked
					if (this.Part.IsChecked)
					{
						Int64 stopSize = this.Parent.CheckNextReferenceBytes(this.Part, this.stopBuffer);
						// all ok
						if (stopSize == 0)
						{
							log.Info("con_DataReceived() reference check ok");
							aData = new byte[0];
							this.Connection.Disconnect();
							return;
						}
						// data mismatch
						else
						{
							log.Error("con_DataReceived() reference check failed");
							aData = this.stopBuffer;
							this.StopSize = stopSize;
						}
					}
					// we are unchecked, so just close
					else
					{
						// shrink the buffer if it is to big
						if (this.stopBuffer.Length > Settings.Instance.FileRollbackCheck)
						{
							Array.Resize(ref this.stopBuffer, (int)Settings.Instance.FileRollbackCheck);
						}
						// and write it to file to be able to check the next file
						this.writer.Write(this.stopBuffer);
						this.writer.Flush();
						this.receivedBytes += this.stopBuffer.Length;

						this.Connection.Disconnect();
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
				this.writer.Write(aData);
				this.writer.Flush();
				this.receivedBytes += aData.Length;
				this.speedCalcSize += aData.Length;
				this.Part.CurrentSize += aData.Length;

				// statistics
				Statistic.Instance.Increase(StatisticType.BytesLoaded, aData.Length);
			}
			catch (Exception ex)
			{
				log.Fatal("con_DataReceived() write", ex);
				this.streamOk = false;
				this.Connection.Disconnect();
				return;
			}

			// update all listeners with new values and a calculated speed
			if ((DateTime.Now - this.speedCalcTime).TotalMilliseconds > Settings.Instance.UpdateDownloadTime)
			{
				DateTime old = this.speedCalcTime;
				this.speedCalcTime = DateTime.Now;
				this.Part.Speed = (this.speedCalcSize / (this.speedCalcTime - old).TotalMilliseconds) * 1000;

				this.Part.Commit();
				this.speedCalcSize = 0;

				// statistics
				if(this.Part.Speed > Statistic.Instance.Get(StatisticType.SpeedMax))
				{
					Statistic.Instance.Set(StatisticType.SpeedMax, this.Part.Speed);
				}
				else if(this.Part.Speed < Statistic.Instance.Get(StatisticType.SpeedMin))
				{
					Statistic.Instance.Set(StatisticType.SpeedMin, this.Part.Speed);
				}
			}
		}

		#endregion
	}
}
