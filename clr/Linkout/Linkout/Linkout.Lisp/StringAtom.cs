using System;
namespace Linkout.Lisp
{
	public class StringAtom : Atom
	{
		public readonly string string_value;
		
		public StringAtom (string string_value) : base(AtomType.String)
		{
			this.string_value = string_value;
		}

		public override Int64 get_fixedpoint()
		{
			throw new NotSupportedException();
		}

		public override string get_string()
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
