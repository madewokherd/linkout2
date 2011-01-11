using System;
namespace Linkout.Lisp
{
	public class StreamAtomWriter : AtomWriter
	{
		public StreamAtomWriter (System.IO.Stream stream)
		{
			priv_stream = stream;
		}
		
		private System.IO.Stream priv_stream;
		
		System.IO.Stream stream
		{
			get
			{
				return priv_stream;
			}
		}
		
		public override void Write (Atom data)
		{
			data.write_to_stream(priv_stream);
		}
		
		public override void Close ()
		{
			priv_stream.Close();
		}
	}
}

