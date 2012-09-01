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

		public event EmptyDelegate Connected;
		public void FireConnected ()
		{
			if (Connected != null)
			{
				Connected();
			}
		}

		public event SocketErrorDelegate Disconnected;
		public void FireDisconnected (SocketErrorCode aValue)
		{
			if (Disconnected != null)
			{
				Disconnected(aValue);
			}
		}

		public event DataTextDelegate DataTextReceived;
		public void FireDataTextReceived (string aData)
		{
			if (DataTextReceived != null)
			{
				DataTextReceived(aData);
			}
		}

		public event DataBinaryDelegate DataBinaryReceived;
		public void FireDataBinaryReceived (byte[] aData)
		{
			if (DataBinaryReceived != null)
			{
				DataBinaryReceived(aData);
			}
		}

		public abstract void Connect ();

		public abstract void Disconnect ();

		public abstract void SendData (string aData);
	}
}

