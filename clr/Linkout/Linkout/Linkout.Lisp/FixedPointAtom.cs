using System;
namespace Linkout.Lisp
{
	public class FixedPointAtom : Atom
	{
		public readonly long int_value;
		
		public FixedPointAtom (long int_value) : base(AtomType.FixedPoint)
		{
			this.int_value = int_value;
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
	
		public override void to_stream (System.IO.Stream output)
		{
			int shift;
			int precision=16;
			bool printed_wholepart = false;
			
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
			for (shift=60; shift >= precision; shift -= 4)
			{
				long digit = (int_value >> shift) & 0xf;
				if (precision == 12)
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
	}
}

