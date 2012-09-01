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

using XG.Core;

namespace XG.Server.Connection
{
	public abstract class AIrcConnection
	{
		public ServerHandler Parent { get; set; }

		AConnection _connection;
		public AConnection Connection
		{
			get
			{
				return _connection;
			}
			set
			{
				if(_connection != null)
				{
					_connection.Connected -= new EmptyDelegate(ConnectionConnected);
					_connection.Disconnected -= new SocketErrorDelegate(ConnectionDisconnected);
					_connection.DataTextReceived -= new DataTextDelegate(ConnectionDataReceived);
					_connection.DataBinaryReceived -= new DataBinaryDelegate(ConnectionDataReceived);
				}
				_connection = value;
				if(_connection != null)
				{
					_connection.Connected += new EmptyDelegate(ConnectionConnected);
					_connection.Disconnected += new SocketErrorDelegate(ConnectionDisconnected);
					_connection.DataTextReceived += new DataTextDelegate(ConnectionDataReceived);
					_connection.DataBinaryReceived += new DataBinaryDelegate(ConnectionDataReceived);
				}
			}
		}

		protected virtual void ConnectionConnected()
		{}

		protected virtual void ConnectionDisconnected(SocketErrorCode aValue)
		{}

		protected virtual void ConnectionDataReceived(string aData)
		{}

		protected virtual void ConnectionDataReceived(byte[] aData)
		{}
	}
}

