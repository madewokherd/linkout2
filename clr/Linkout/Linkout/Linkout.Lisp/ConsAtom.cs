using System;
namespace Linkout.Lisp
{
	public class ConsAtom : Atom
	{
		public readonly Atom car;
		public readonly Atom cdr;
		
		public ConsAtom (Atom car, Atom cdr) : base(AtomType.Cons)
		{
			this.car = car;
			this.cdr = cdr;
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
			return this.car;
		}

		public override Atom get_cdr()
		{
			return this.cdr;
		}
	}
}
