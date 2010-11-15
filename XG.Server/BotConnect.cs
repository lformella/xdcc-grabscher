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
using System.Net;
using XG.Core;

namespace XG.Server
{
	/// <summary>
	/// This class describes the connection to a single irc bot
	/// it does the following things
	/// - receiving all data comming from the bot
	/// - writing the data into the file
	/// - checking if the data matches the given file (rollback check)
	/// </summary>	
	public class BotConnect
	{
		#region VARIABLES

		private ServerHandler myParent;

		private XGPacket myPacket;
		private Int64 myStartSize;
		private Connection myCon;

		private BinaryWriter myWriter;
		private BinaryReader myReader;

		private Int64 myReceivedBytes;
		private DateTime mySpeedCalcTime;
		private Int64 mySpeedCalcSize;

		private byte[] myStartReference = null;
		private byte[] myRollbackRefernce = null;
		private byte[] myStartBuffer = null;
		private byte[] myStopBuffer = null;

		private bool myStreamOk = false;
		private bool removePart = false;

		#endregion

		#region EVENTS

		public event PacketBotConnectDelegate ConnectedEvent;
		public event PacketBotConnectDelegate DisconnectedEvent;
		public event ObjectDelegate ObjectChangedEvent;

		#endregion

		#region GETTER SETTER

		private XGPacket Packet
		{
			get { return this.myPacket; }
		}

		public Int64 StartSize
		{
			get { return this.myStartSize; }
		}
		public Int64 CurrrentSize
		{
			get { return this.myStartSize + this.myReceivedBytes; }
		}

		private XGFilePart part;
		public XGFilePart Part
		{
			get { return part; }
			set { part = value; }
		}
		public XGFile File
		{
			get { return Part.Parent; }
		}

		public Int64 StopSize
		{
			get { return this.Part.StopSize; }
			set
			{
				this.Part.StopSize = value;
				this.Log("StopSize.set(" + value + ")", LogLevel.Notice);
				this.ObjectChangedEvent(this.Part);
			}
		}

		public Int64 TimeMissing
		{
			get { return this.Part.TimeMissing; }
		}

		public string FileName
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

		public bool ReferenceCheck
		{
			set { this.Part.IsChecked = value; }
		}
		public byte[] ReferenceBytes
		{
			get { return this.myStartReference; }
		}

		#endregion

		#region INIT

		public BotConnect(ServerHandler aParent)
		{
			this.myParent = aParent;
		}

		#endregion

		#region CONNECT

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aObject"></param>
		public void Connect(XGPacket aPacket, Int64 aStartSize, IPAddress aIp, int aPort)
		{
			this.myPacket = aPacket;
			this.myStartSize = aStartSize;

			this.myCon = new Connection();
			this.myCon.ConnectedEvent += new EmptyDelegate(con_ConnectedEventHandler);
			this.myCon.DisconnectedEvent += new SocketErrorDelegate(con_DisconnectedEventHandler);
			this.myCon.DataBinaryReceivedEvent += new DataBinaryDelegate(con_DataReceivedEventHandler);

			this.myCon.Connect(aIp.ToString(), aPort, aPacket.RealSize - aStartSize);
		}
		private void con_ConnectedEventHandler()
		{
			this.mySpeedCalcTime = DateTime.Now;
			this.mySpeedCalcSize = 0;
			this.myReceivedBytes = 0;

			this.Packet.EnabledChangedEvent += new ObjectDelegate(packet_ObjectStateChanged);

			this.Part = this.myParent.GetPart(this.myParent.GetNewFile(this.Packet.RealName, this.Packet.RealSize), this.StartSize);
			if (this.Part != null)
			{
				// wtf?
				if (this.StartSize == this.StopSize)
				{
					this.Log("con_Connected() startSize = stopsize (" + this.StartSize + ")", LogLevel.Error);
					this.Disconnect();
					return;
				}

				this.Part.PartState = FilePartState.Open;
				this.Part.Packet = this.Packet;

				this.Log("con_Connected() startet (" + this.StartSize + " - " + this.StopSize + ")", LogLevel.Notice);

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
							this.myReader = new BinaryReader(stream);

							// seek to 0 and extract the startbuffer bytes need for the previous file
							stream.Seek(0, SeekOrigin.Begin);
							this.myStartReference = this.myReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);

							// seek to seekPos and extract the rollbackcheck bytes
							stream.Seek(seekPos, SeekOrigin.Begin);
							this.myRollbackRefernce = this.myReader.ReadBytes((int)Settings.Instance.FileRollbackCheck);
							// seek back
							stream.Seek(seekPos, SeekOrigin.Begin);
						}
						catch (Exception ex)
						{
							this.Log("con_Connected() seek: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
							this.Disconnect();
							return;
						}
					}
					else { this.myStreamOk = true; }

					this.myWriter = new BinaryWriter(stream);

					#region EMIT CHANGES

					this.ObjectChangedEvent(this.Part);

					this.Packet.Connected = true;
					this.ObjectChangedEvent(this.Packet);

					this.Packet.Parent.BotState = BotState.Active;
					this.Packet.Parent.QueuePosition = 0;
					this.Packet.Parent.QueueTime = 0;
					this.ObjectChangedEvent(this.Packet.Parent);

					#endregion
#if !UNSAFE
				}
				catch (Exception ex)
				{
					this.Log("con_Connected() Exception: " + XGHelper.GetExceptionMessage(ex), LogLevel.Error);
					this.Disconnect();
					return;
				}
#endif

				// statistics
				Statistic.Instance.Increase(StatisticType.BotConnectsOk);
			}
			else
			{
				this.Log("con_Connected() cant find a part to download", LogLevel.Error);
				this.Disconnect();
			}
		}

		#endregion

		#region DISCONNECT

		public void Remove()
		{
			this.removePart = true;
			this.Disconnect();
		}
		public void Disconnect()
		{
			// this condition should never be false
			if (this.myCon != null)
			{
				this.myCon.Disconnect();
			}
		}
		private void con_DisconnectedEventHandler(SocketErrorCode aValue)
		{
			// close the writer
			if (this.myWriter != null) { this.myWriter.Close(); }

			this.Packet.EnabledChangedEvent -= new ObjectDelegate(packet_ObjectStateChanged);

			this.Packet.Connected = false;
			this.ObjectChangedEvent(this.Packet);

			this.Packet.Parent.BotState = BotState.Idle;
			this.ObjectChangedEvent(this.Packet.Parent);

			if (this.Part != null)
			{
				this.Part.Packet = null;
				this.Part.PartState = FilePartState.Closed;

				if (this.removePart)
				{
					this.Part.PartState = FilePartState.Broken;
					this.myParent.RemovePart(this.File, this.Part);
				}
				else
				{
					// the file is ok if the size is equal or it has an additional buffer for checking
					if (this.CurrrentSize == this.StopSize || (!this.Part.IsChecked && this.CurrrentSize == this.StopSize + Settings.Instance.FileRollbackCheck))
					{
						this.Part.PartState = FilePartState.Ready;
						this.Log("con_Disconnected() ready" + (this.Part.IsChecked ? "" : " but unchecked"), LogLevel.Notice);

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsCompleted);
					}
					// that should not happen
					else if (this.CurrrentSize > this.StopSize)
					{
						this.Part.PartState = FilePartState.Broken;
						this.Log("con_Disconnected() size is bigger than excepted: " + this.CurrrentSize + " > " + this.StopSize, LogLevel.Error);
						// this mostly happens on the last part of a file - so lets remove the file and load the package again
						if (this.File.Children.Length == 1 || this.Part.StopSize == this.File.Size)
						{
							this.myParent.RemoveFile(this.File);
							this.Log("con_Disconnected() removing corputed file " + this.File.Name, LogLevel.Error);
						}

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsBroken);
					}
					// it did not start
					else if (this.myReceivedBytes == 0)
					{
						this.Log("con_Disconnected() downloading did not start, disabling packet", LogLevel.Error);
						this.Packet.Enabled = false;

						// statistics
						Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
					}
					// it is incomplete
					else
					{
						this.Log("con_Disconnected() incomplete", LogLevel.Error);

						// statistics
						Statistic.Instance.Increase(StatisticType.PacketsIncompleted);
					}
				}
				this.ObjectChangedEvent(this.Part);
			}
			// the connection didnt even connected to the given ip and port
			else
			{
				// lets disable the packet, because the bot seems to have broken config or is firewalled
				this.Log("con_Disconnected() connection did not work, disabling packet", LogLevel.Error);
				this.Packet.Enabled = false;

				// statistics
				Statistic.Instance.Increase(StatisticType.BotConnectsFailed);
			}

			this.myCon.ConnectedEvent -= new EmptyDelegate(con_ConnectedEventHandler);
			this.myCon.DisconnectedEvent -= new SocketErrorDelegate(con_DisconnectedEventHandler);
			this.myCon.DataBinaryReceivedEvent -= new DataBinaryDelegate(con_DataReceivedEventHandler);
			this.myCon = null;

			this.DisconnectedEvent(this.Packet, this);
			this.myPacket = null;
		}

		void packet_ObjectStateChanged(XGObject aObj)
		{
			if (!aObj.Enabled)
			{
				this.Remove();
			}
		}

		#endregion

		#region DATA

		private void con_DataReceivedEventHandler(byte[] aData)
		{
			#region ROLLBACKCHECK

			if (!this.myStreamOk)
			{
				// intial data
				if (this.myStartBuffer == null) { this.myStartBuffer = aData; }
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = this.myStartBuffer.Length;
					Array.Resize(ref this.myStartBuffer, bL + dL);
					Array.Copy(aData, 0, this.myStartBuffer, bL, dL);
				}

				int refL = this.myRollbackRefernce.Length;
				int bufL = this.myStartBuffer.Length;
				// we have enough data so check them
				if (refL <= bufL)
				{
					// all ok
					if (XGHelper.IsEqual(this.myRollbackRefernce, this.myStartBuffer))
					{
						this.Log("con_DataReceived() rollback check ok", LogLevel.Notice);
						aData = this.myStartBuffer;
						this.myStartBuffer = null;
						this.myStreamOk = true;
					}
					// data mismatch
					else
					{
						this.Log("con_DataReceived() rollback check failed", LogLevel.Error);

						// unregister from the event because if this is triggered
						// it will remove the part
						this.Packet.EnabledChangedEvent -= new ObjectDelegate(packet_ObjectStateChanged);
						this.myPacket.Enabled = false;
						aData = new byte[0];
						this.Disconnect();
						return;
					}
				}
				// some data is missing, so wait for more
				else { return; }
			}
			// save the reference bytes if it is a new file
			else if (this.myStartReference == null || this.myStartReference.Length < (int)Settings.Instance.FileRollbackCheck)
			{
				// initial data
				if (this.myStartReference == null) { this.myStartReference = aData; }
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = this.myStartReference.Length;
					Array.Resize(ref this.myStartReference, bL + dL);
					Array.Copy(aData, 0, this.myStartReference, bL, dL);
				}
				// shrink the reference if it is to big
				if (this.myStartReference.Length > Settings.Instance.FileRollbackCheck)
				{
					Array.Resize(ref this.myStartReference, (int)Settings.Instance.FileRollbackCheck);
				}
			}

			#endregion

			#region NEXT REFERENCE CHECK

			//    stop     needed refbytes
			// ----------~~~~~~~~~~~~~~~~~~~
			// -------~~~~~~~~
			//   cur    data
			if (this.StopSize < this.Packet.RealSize && this.StopSize < this.StartSize + this.myReceivedBytes + aData.Length)
			{
				bool initial = false;
				// intial data
				if (this.myStopBuffer == null)
				{
					initial = true;
					Int64 length = this.StopSize - (this.StartSize + this.myReceivedBytes);
					if (length < 0)
					{
						// this is bad and should not happen	  
						length = 0;
						this.Disconnect();
					}
					this.myStopBuffer = new byte[aData.Length - length];
					// copy the overlapping data into the buffer
					Array.Copy(aData, length, this.myStopBuffer, 0, aData.Length - length);
					// and shrink the actual data
					Array.Resize(ref aData, (int)length);
				}
				// resize buffer and copy data
				else
				{
					int dL = aData.Length;
					int bL = this.myStopBuffer.Length;
					Array.Resize(ref this.myStopBuffer, bL + dL);
					Array.Copy(aData, 0, this.myStopBuffer, bL, dL);
				}

				int bufL = this.myStopBuffer.Length;
				// we have enough data so check them
				if (Settings.Instance.FileRollbackCheck <= bufL)
				{
					// but only if we are checked
					if (this.Part.IsChecked)
					{
						Int64 stopSize = this.myParent.CheckNextReferenceBytes(this.Part, this.myStopBuffer);
						// all ok
						if (stopSize == 0)
						{
							this.Log("con_DataReceived() reference check ok", LogLevel.Notice);
							aData = new byte[0];
							this.Disconnect();
							return;
						}
						// data mismatch
						else
						{
							this.Log("con_DataReceived() reference check failed", LogLevel.Error);
							aData = this.myStopBuffer;
							this.StopSize = stopSize;
						}
					}
					// we are unchecked, so just close
					else
					{
						// shrink the buffer if it is to big
						if (this.myStopBuffer.Length > Settings.Instance.FileRollbackCheck)
						{
							Array.Resize(ref this.myStopBuffer, (int)Settings.Instance.FileRollbackCheck);
						}
						// and write it to file to be able to check the next file
						this.myWriter.Write(this.myStopBuffer);
						this.myWriter.Flush();
						this.myReceivedBytes += this.myStopBuffer.Length;

						this.Disconnect();
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
				this.myWriter.Write(aData);
				this.myWriter.Flush();
				this.myReceivedBytes += aData.Length;
				this.mySpeedCalcSize += aData.Length;
				this.Part.CurrentSize += aData.Length;

				// statistics
				Statistic.Instance.Increase(StatisticType.BytesLoaded, aData.Length);
			}
			catch (Exception ex)
			{
				this.Log("con_DataReceived() write: " + XGHelper.GetExceptionMessage(ex), LogLevel.Exception);
				this.myStreamOk = false;
				this.Disconnect();
				return;
			}

			// update all listeners with new values and a calculated speed
			if ((DateTime.Now - this.mySpeedCalcTime).TotalMilliseconds > Settings.Instance.UpdateDownloadTime)
			{
				DateTime old = this.mySpeedCalcTime;
				this.mySpeedCalcTime = DateTime.Now;
				this.Part.Speed = (this.mySpeedCalcSize / (this.mySpeedCalcTime - old).TotalMilliseconds) * 1000;

				this.ObjectChangedEvent(this.Part);
				this.mySpeedCalcSize = 0;

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

		#region LOG

		private void Log(string aData, LogLevel aLevel)
		{
			XGHelper.Log("BotConnect(" + (this.myCon != null ? this.myCon.Host + ":" + this.myCon.Port + ", " : "") + this.FileName + ")." + aData, aLevel);
		}

		#endregion
	}
}
