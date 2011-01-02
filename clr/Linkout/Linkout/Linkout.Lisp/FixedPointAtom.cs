/*
 * Copyright 2010, 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
namespace Linkout.Lisp
{
	public class FixedPointAtom : Atom
	{
		public readonly long int_value;
		
		public FixedPointAtom (long int_value) : base(AtomType.FixedPoint)
		{
			this.int_value = (int_value << 16) >> 16;
			this.calculated_hashcode = false;
		}

		public override long get_fixedpoint()
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
		
		public override bool is_true ()
		{
			return this.int_value != 0;
		}
	
		public override bool Equals (object obj)
		{
			if (obj.GetType() != this.GetType())
				return false;
			FixedPointAtom oth = (FixedPointAtom)obj;
			return this.int_value == oth.int_value;
		}
		
		private bool calculated_hashcode;
		private int hashcode;

		public override int GetHashCode ()
		{
			int result;
			
			if (calculated_hashcode)
				return hashcode;
			
			result = (int)((this.int_value >> 16) ^ (this.int_value & 0xffff) ^ 0xfa425617);
			
			hashcode = result;
			calculated_hashcode = true;
			
			return result;
		}
		
		public override void to_stream (System.IO.Stream output)
		{
			int shift;
			int precision=16;
			bool printed_wholepart = false;
			
			if (int_value < 0)
			{
				output.WriteByte(45); /* - */
				FixedPointAtom inverse = new FixedPointAtom(-int_value);
				if (inverse.int_value >= 0)
				{
					inverse.to_stream(output);
					return;
				}
			}
			
			/* find the least significant non-zero fractional digit */
			for (shift=0; shift < 16; shift += 4)
			{
				if (((int_value >> shift) & 0xf) != 0)
				{
					precision = shift;
					break;
				}
			}
			
			output.WriteByte(48); /* 0 */
			output.WriteByte(120); /* x */
			for (shift=44; shift >= precision; shift -= 4)
			{
				long digit = (int_value >> shift) & 0xf;
				if (shift == 12)
					output.WriteByte(46); /* . */
				if (digit != 0 || printed_wholepart || shift <= 16)
				{
					byte digit_char;
					if (digit < 10)
						digit_char = (byte)(48 + digit);
					else
						digit_char = (byte)(97 /* a */ + digit - 10);
					output.WriteByte(digit_char);
					printed_wholepart = true;
				}
			}
		}
		
		public static FixedPointAtom Zero = new FixedPointAtom(0);

		public static FixedPointAtom One = new FixedPointAtom(0x10000);
	}
}

