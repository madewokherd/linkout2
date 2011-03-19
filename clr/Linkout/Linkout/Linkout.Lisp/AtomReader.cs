/*
 * Copyright 2010, 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Linkout is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *  
 * You should have received a copy of the GNU General Public License
 * along with Linkout.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.IO.Compression;
namespace Linkout.Lisp
{
	public abstract class AtomReader
	{
		public abstract Atom Read();
		
		public abstract void Close();
		
		private enum StreamType
		{
			Plaintext,
			Binary,
			Gzip
		}
		
		private static StreamType GuessTypeOfStream(Stream stream)
		{
			byte[] magic = new byte[4];
			
			if (stream.Read(magic, 0, 2) != 2)
				return StreamType.Plaintext;
			
			if (magic[0] == 0x1f && magic[1] == 0x8b)
				return StreamType.Gzip;
			
			if (stream.Read(magic, 2, 2) != 2)
				return StreamType.Plaintext;

			if (magic[0] == 0x4c && magic[1] == 0x00 && magic[2] == 0x74 && magic[3] == 0x02)
				return StreamType.Binary;
			
			return StreamType.Plaintext;
		}
		
		public static AtomReader FromStream(Stream stream)
		{
			StreamType stream_type;
			
			if (!stream.CanSeek)
			{
				throw new ArgumentException("must be seekable", "stream");
			}

			stream.Seek(0, SeekOrigin.Begin);
			
			stream_type = GuessTypeOfStream(stream);
			
			stream.Seek(0, SeekOrigin.Begin);
			
			if (stream_type == StreamType.Gzip)
			{
				GZipStream gzipstream = new GZipStream(stream, CompressionMode.Decompress, true);
				
				stream_type = GuessTypeOfStream(gzipstream);
				gzipstream.Close();
				
				stream.Seek(0, SeekOrigin.Begin);
				
				gzipstream = new GZipStream(stream, CompressionMode.Decompress);
				
				AtomReader result;
				
				if (stream_type == StreamType.Binary)
					result = new BinaryAtomReader(gzipstream);
				else
					result = new StreamAtomReader(gzipstream);
				
				return result;
			}
			else if (stream_type == StreamType.Binary)
			{
				BinaryAtomReader result = new BinaryAtomReader(stream);
				
				return result;
			}
			else // stream_type == StreamType.Plaintext
			{
				StreamAtomReader result = new StreamAtomReader(stream);
				
				return result;
			}
		}
	}
}

