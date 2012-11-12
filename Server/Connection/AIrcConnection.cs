// 
//  AIrcConnection.cs
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

using XG.Core;
using XG.Server.Helper;

namespace XG.Server.Connection
{
	public abstract class AIrcConnection
	{
		public FileActions FileActions { set; get; }

		AConnection _connection;

		public AConnection Connection
		{
			get { return _connection; }
			set
			{
				if (_connection != null)
				{
					_connection.Connected -= ConnectionConnected;
					_connection.Disconnected -= ConnectionDisconnected;
					_connection.DataTextReceived -= ConnectionDataReceived;
					_connection.DataBinaryReceived -= ConnectionDataReceived;
				}
				_connection = value;
				if (_connection != null)
				{
					_connection.Connected += ConnectionConnected;
					_connection.Disconnected += ConnectionDisconnected;
					_connection.DataTextReceived += ConnectionDataReceived;
					_connection.DataBinaryReceived += ConnectionDataReceived;
				}
			}
		}

		protected virtual void ConnectionConnected() {}

		protected virtual void ConnectionDisconnected(SocketErrorCode aValue) {}

		protected virtual void ConnectionDataReceived(string aData) {}

		protected virtual void ConnectionDataReceived(byte[] aData) {}
	}
}
