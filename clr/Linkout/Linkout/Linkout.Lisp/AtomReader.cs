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
			Gzip
		}
		
		private static StreamType GuessTypeOfStream(Stream stream)
		{
			byte[] magic = new byte[2];
			
			if (stream.Read(magic, 0, 2) != 2)
				return StreamType.Plaintext;
			
			if (magic[0] == 0x1f && magic[1] == 0x8b)
				return StreamType.Gzip;
			
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
				GZipStream gzipstream = new GZipStream(stream, CompressionMode.Decompress);
				
				StreamAtomReader result = new StreamAtomReader(gzipstream);
				
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

