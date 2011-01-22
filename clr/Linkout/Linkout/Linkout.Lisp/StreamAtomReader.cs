using System;
namespace Linkout.Lisp
{
	public class StreamAtomReader : AtomReader
	{
		public StreamAtomReader (System.IO.Stream stream)
		{
			this.priv_stream = stream;
		}
		
		private System.IO.Stream priv_stream;
		
		public System.IO.Stream stream
		{
			get
			{
				return priv_stream;
			}
		}
		
		public override Atom Read ()
		{
			return Atom.from_stream(priv_stream);
		}
		
		public override void Close ()
		{
			priv_stream.Close();
		}
	}
}
