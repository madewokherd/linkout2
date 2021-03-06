/*
 * Copyright 2010, 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Linkout is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *  
 * You should have received a copy of the GNU General Public License
 * along with Linkout.  If not, see <http://www.gnu.org/licenses/>.
 */

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
			this.calculated_hashcode = false;
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

		public override bool is_true ()
		{
			return true;
		}

		public override bool Equals (object obj)
		{
			if (obj.GetType() != this.GetType())
				return false;
			ConsAtom oth = (ConsAtom)obj;
			return this.car.Equals(oth.car) && this.cdr.Equals(oth.cdr);
		}
		
		private bool calculated_hashcode;
		private int hashcode;

		public override int GetHashCode ()
		{
			int result;
			
			if (calculated_hashcode)
				return hashcode;
			
			result = (this.cdr.GetHashCode() * 17) + this.car.GetHashCode();
			
			hashcode = result;
			calculated_hashcode = true;
			
			return result;
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
		
		public uint GetLength()
		{
			uint result = 1;
			Atom rest = this.cdr;
			
			while (rest.GetType() == typeof(ConsAtom))
			{
				result++;
				rest = rest.get_cdr();
			}
			
			if (rest.atomtype != AtomType.Nil)
				// Not a valid list.
				return 0;

			return result;
		}
	}
}
