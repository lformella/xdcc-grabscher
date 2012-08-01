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
	public abstract class AIrcConnection
	{
		public ServerHandler Parent { get; set; }

		private AConnection connection;
		public AConnection Connection
		{
			get
			{
				return this.connection;
			}
			set
			{
				if(this.connection != null)
				{
					this.connection.ConnectedEvent -= new EmptyDelegate(Connection_ConnectedEventHandler);
					this.connection.DisconnectedEvent -= new SocketErrorDelegate(Connection_DisconnectedEventHandler);
					this.connection.DataTextReceivedEvent -= new DataTextDelegate(Connection_DataReceivedEventHandler);
					this.connection.DataBinaryReceivedEvent -= new DataBinaryDelegate(Connection_DataReceivedEventHandler);
				}
				this.connection = value;
				if(this.connection != null)
				{
					this.connection.ConnectedEvent += new EmptyDelegate(Connection_ConnectedEventHandler);
					this.connection.DisconnectedEvent += new SocketErrorDelegate(Connection_DisconnectedEventHandler);
					this.connection.DataTextReceivedEvent += new DataTextDelegate(Connection_DataReceivedEventHandler);
					this.connection.DataBinaryReceivedEvent += new DataBinaryDelegate(Connection_DataReceivedEventHandler);
				}
			}
		}

		protected abstract void Connection_ConnectedEventHandler();

		protected abstract void Connection_DisconnectedEventHandler(SocketErrorCode aValue);

		protected abstract void Connection_DataReceivedEventHandler(string aData);

		protected abstract void Connection_DataReceivedEventHandler(byte[] aData);
	}
}

