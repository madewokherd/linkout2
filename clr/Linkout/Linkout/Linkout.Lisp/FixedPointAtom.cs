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
		
		/* Dear reader, I present for your contemplation an arguement
		 * demonstrating the correctness and consistency of the following functions.
		 *
		 * In general, the division and modulo operators in any programming language
		 * should satisfy the following equation:
		 * 
		 * dividend = divisor * (dividend // divisior) + (dividend % divisor)
		 *  where * is the multiplication operator, // is integer division, and % is the modulo operator
		 * 
		 * Because the numbers manipulated by these functions are not used as integers
		 * but as fixed-point binary numbers, the following division function is not
		 * an integer division function. For purposes of satisfying the above inequality,
		 * I define the result of integer division of x by y to be equal to
		 * floor(x / y)
		 *  where / is the divide function defined below, and floor is an operation that
		 *  clears all fractional bits
		 *
		 * For this language, the sign of the result of the
		 * modulo operator shall be equal to the sign of the divisor, except when the
		 * divisor divides the dividend exactly.
		 * 
		 * Also, the fixed-point division operator always rounds down when the result
		 * of the division cannot be expressed exactly.
		 * 
		 * The corresponding operators in C# work differently. In C#, the sign of the
		 * modulo matches the sign of the dividend, and division rounds towards 0.
		 * 
		 * The fixed-point numbers on which we operate contain 16 fractional bits and
		 * 32 bits representing the whole number. The most siginificant 16 bits are
		 * unused (equal to the sign bit).
		 * 
		 * The fractional precision must always be considered. Because we are representing
		 * a real number x using an integral number equal to x*2**16, where ** is an
		 * exponent operator (not supported by C#), the result of
		 * dividing two of those integers would be (x*2**16) / (y*2**16), which is equal
		 * to x / y.
		 * 
		 * However, the result we want is (x / y) * 2**16. Therefore, we multiply x before
		 * the division, so that we get (x*2**16)*2**16 / (y*2**16) = (x / y) * 2**16.
		 * In the code, I have written <<16 instead of *2**16, but it is equivalent.
		 * 
		 * Because the upper 16 bits are equal to the sign bit, we know that it's impossible
		 * for (x*2**16)*2**16 to overflow, but we will get an inexact representation of
		 * (x*2**16)*2**16 / (y*2**16) when that number would be fractional. When this happens,
		 * the result, which we've stored in div, will use C#'s rounding rules.
		 * 
		 * We also must consider whether this division can overflow and what will happen in that
		 * case. Because (y*2**16) is an integer, we know that its absolute value cannot be less
		 * than one unless
		 * it is equal to 0 (and when it is equal to 0, this operation is undefined). Thus, the
		 * result cannot be greater in magnitude than (x*2**16)*2**16. However, the largest
		 * integer that can be represented (in magnitude) differs depending on sign. We can
		 * represent -2**63, but we cannot represent 2**63. Therefore, if (x*2**16)*2**16 is
		 * equal to -2**63, and (y*2**16) is equal to -1, the result of the division will be
		 * -2**63. However, this case is unimportant because -1 divides -2**63 evenly, and
		 * the overall operation (but not this particular division) will overflow and throw away
		 * the sign bit.
		 * 
		 * In order to correct for C#'s rounding rules, we must alter the result when C# rounds
		 * differently. If the result of division is positive, C# will round towards 0, which is
		 * down, and no correction is necessary. If the result of division is negative, C# will
		 * round up, and we need to correct this. However, we cannot use div to determine the sign
		 * of the result of the division because that number will be equal to 0 whenever (y*2**16)
		 * is greater in magnitude than (x*2**16)*2**16. Instead, we compare the sign of the dividend
		 * with the sign of the divisor. If they are equal, the result is positive. If they are not
		 * equal, the result is negative. If either is 0, the result is either exactly 0 or undefined,
		 * so we need not be concerned with this case.
		 * 
		 * If the result of division is negative, and mod is non-zero (meaning (y*2**16) does not
		 * evenly divide (x*2**16)*2**16), C# will round up when we want to round down. Therefore,
		 * we subtract 1 in that case.
		 * 
		 * Remember that the upper 16 bits are supposed to be unused. The divide function returns
		 * a fixed-point number with a whole precision of 48 bits, but it is important that the caller
		 * throw away the upper 16 bits. This can result in an overflow, in which case the result of
		 * division is actually (((x / y) + 2**31) % 2**32) - 2**31. This is important because it
		 * matches the semantics of addition, subtraction, and multiplication, and allows the modulo
		 * operator to behave sanely when division would overflow.
		 * 
		 * 
		 **/
		public static long divide(long a, long b)
		{
			long div = (a << 16) / b;
			long mod = (a << 16) % b;
			
			if (((a > 0) != (b > 0)) && mod != 0)
				div = div - 1;
			
			return div;
		}
	}
}

