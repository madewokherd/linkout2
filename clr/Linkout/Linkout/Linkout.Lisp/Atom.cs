using System;
namespace Linkout.Lisp
{
	public abstract class Atom
	{
		public AtomType atomtype;
		
		internal Atom(AtomType atomtype)
		{
			this.atomtype = atomtype;
		}

		public abstract Int64 get_fixedpoint();

		public abstract string get_string();
		
		public abstract Atom get_car();

		public abstract Atom get_cdr();
	}
}

