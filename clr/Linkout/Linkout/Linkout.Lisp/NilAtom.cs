using System;
namespace Linkout.Lisp
{
	public sealed class NilAtom : Atom
	{
		private NilAtom () : base(AtomType.Nil)
		{
		}

		private static readonly NilAtom nil_singleton = new NilAtom();
		
		public static NilAtom nil
		{
			get
			{
				return nil_singleton;
			}
		}

		public override Int64 get_fixedpoint()
		{
			throw new NotSupportedException();
		}

		public override byte[] get_string()
		{
			throw new NotSupportedException();
		}
		
		public override Atom get_car()
		{
			throw new NotSupportedException();
		}

		public override Atom get_cdr()
		{
			throw new NotSupportedException();
		}

		private readonly byte[] nil_str = {40, 41}; // ()
		
		public override void to_stream (System.IO.Stream output)
		{
			output.Write(nil_str, 0, 2);
		}
	}
}
