//  
//  Copyright (C) 2012 Lars Formella <ich@larsformella.de>
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
using XG.Core;

namespace XG.Server.Connection
{
	public abstract class AConnection
	{
		public string Hostname { get; set; }

		public int Port { get; set; }

		public Int64 MaxData { get; set; }

		public event EmptyDelegate ConnectedEvent;

		public void FireConnectedEvent ()
		{
			if (this.ConnectedEvent != null)
			{
				this.ConnectedEvent();
			}
		}

		public event SocketErrorDelegate DisconnectedEvent;

		public void FireDisconnectedEvent (SocketErrorCode aValue)
		{
			if (this.DisconnectedEvent != null)
			{
				this.DisconnectedEvent(aValue);
			}
		}

		public event DataTextDelegate DataTextReceivedEvent;

		public void FireDataTextReceivedEvent (string aData)
		{
			if (this.DataTextReceivedEvent != null)
			{
				this.DataTextReceivedEvent(aData);
			}
		}

		public event DataBinaryDelegate DataBinaryReceivedEvent;

		public void FireDataBinaryReceivedEvent (byte[] aData)
		{
			if (this.DataBinaryReceivedEvent != null)
			{
				this.DataBinaryReceivedEvent(aData);
			}
		}

		public abstract void Connect ();

		public abstract void Disconnect ();

		public abstract void SendData (string aData);
	}
}

