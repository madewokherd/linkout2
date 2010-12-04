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

		public override bool Equals (object obj)
		{
			if (obj.GetType() != this.GetType())
				return false;
			ConsAtom oth = (ConsAtom)obj;
			return this.car.Equals(oth.car) && this.cdr.Equals(oth.cdr);
		}
		
		public override int GetHashCode ()
		{
			return (this.cdr.GetHashCode() * 17) + this.car.GetHashCode();
		}
		
		private static void write_tail (System.IO.Stream output, Atom atom)
		{
			if (atom.atomtype == AtomType.Cons)
			{
				ConsAtom cons = (ConsAtom)atom;
				output.WriteByte(32);
				cons.car.to_stream(output);
				write_tail(output, cons.cdr);
			}
			else if (atom.atomtype == AtomType.Nil)
				return;
			else
			{
				output.WriteByte(32);
				output.WriteByte(46); /* . */
				output.WriteByte(32);
				atom.to_stream(output);
			}
		}
		
		public override void to_stream (System.IO.Stream output)
		{
			output.WriteByte(40); /* ( */
			car.to_stream(output);
			write_tail(output, cdr);
			output.WriteByte(41); /* ) */
		}
	}
}
