using System;
namespace Linkout.Lisp
{
	public class FixedPointAtom : Atom
	{
		public readonly Int64 int_value;
		
		public FixedPointAtom (Int64 int_value) : base(AtomType.FixedPoint)
		{
			this.int_value = int_value;
		}

		public override Int64 get_fixedpoint()
		{
			return this.int_value;
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
	
		public override void to_stream (System.IO.Stream output)
		{
			throw new NotImplementedException();
		}
	}
}

