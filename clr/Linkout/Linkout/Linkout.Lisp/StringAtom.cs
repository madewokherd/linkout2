using System;
namespace Linkout.Lisp
{
	public class StringAtom : Atom
	{
		public readonly byte[] string_value;
		
		public StringAtom (byte[] string_value) : base(AtomType.String)
		{
			this.string_value = string_value;
		}

		public override Int64 get_fixedpoint()
		{
			throw new NotSupportedException();
		}

		public override byte[] get_string()
		{
			return this.string_value;
		}
		
		public override Atom get_car()
		{
			throw new NotSupportedException();
		}

		public override Atom get_cdr()
		{
			throw new NotSupportedException();
		}
	}
}
