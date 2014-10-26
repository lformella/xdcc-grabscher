// 
//  ThrottledStream.cs
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
using System.Threading;

namespace XG.Plugin.Irc
{
	public class ThrottledStream : Stream
	{
		#region VARIABLES

		Stream _baseStream;
		long _byteCount;
		long _start;

		protected long CurrentMilliseconds
		{
			get
			{
				return Environment.TickCount;
			}
		}

		long _maximumBytesPerSecond;

		public long MaximumBytesPerSecond
		{
			get
			{
				return _maximumBytesPerSecond;
			}
			set
			{
				if (_maximumBytesPerSecond != value)
				{
					if (value < 0)
					{
						throw new ArgumentOutOfRangeException("The maximum number of bytes per second can't be negative.");
					}
					_maximumBytesPerSecond = value;
					Reset();
				}
			}
		}

		public override bool CanRead
		{
			get
			{
				return _baseStream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return _baseStream.CanSeek;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return _baseStream.CanWrite;
			}
		}

		public override long Length
		{
			get
			{
				return _baseStream.Length;
			}
		}

		public override long Position
		{
			get
			{
				return _baseStream.Position;
			}
			set
			{
				_baseStream.Position = value;
			}
		}

		#endregion

		#region Ctor

		public ThrottledStream(Stream baseStream) : this(baseStream, 0)
		{
		}

		public ThrottledStream(Stream baseStream, long maximumBytesPerSecond)
		{
			if (baseStream == null)
			{
				throw new ArgumentNullException("baseStream");
			}
			MaximumBytesPerSecond = maximumBytesPerSecond;

			_baseStream = baseStream;
			_start = CurrentMilliseconds;
			_byteCount = 0;
		}

		#endregion

		#region Stream Methods

		public override void Flush()
		{
			_baseStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Throttle(count);

			return _baseStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _baseStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_baseStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Throttle(count);

			_baseStream.Write(buffer, offset, count);
		}

		public override string ToString()
		{
			return _baseStream.ToString();
		}

		#endregion

		#region Throttle Methods

		protected void Throttle(int bufferSizeInBytes)
		{
			if (_maximumBytesPerSecond <= 0 || bufferSizeInBytes <= 0)
			{
				return;
			}

			_byteCount += bufferSizeInBytes;
			long elapsedMilliseconds = CurrentMilliseconds - _start;

			if (elapsedMilliseconds > 0)
			{
				// Calculate the current bps.
				long bps = _byteCount * 1000L / elapsedMilliseconds;

				// If the bps are more then the maximum bps, try to throttle.
				if (bps > _maximumBytesPerSecond)
				{
					// Calculate the time to sleep.
					long wakeElapsed = _byteCount * 1000L / _maximumBytesPerSecond;
					int toSleep = (int)(wakeElapsed - elapsedMilliseconds);

					if (toSleep > 1)
					{
						try
						{
							Thread.Sleep(toSleep);
						}
						catch (ThreadAbortException) {}
						finally
						{
							Reset();
						}
					}
				}
			}
		}

		protected void Reset()
		{
			long difference = CurrentMilliseconds - _start;

			// Only reset counters when a known history is available of more then second.
			if (difference > 1000)
			{
				_byteCount = 0;
				_start = CurrentMilliseconds;
			}
		}

		#endregion
	}
}
