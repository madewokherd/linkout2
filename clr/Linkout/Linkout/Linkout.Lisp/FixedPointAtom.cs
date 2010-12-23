using System;
namespace Linkout.Lisp
{
	public class FixedPointAtom : Atom
	{
		public readonly long int_value;
		
		public FixedPointAtom (long int_value) : base(AtomType.FixedPoint)
		{
			this.int_value = (int_value << 8) >> 8;
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
		
		public override int GetHashCode ()
		{
			return (int)((this.int_value >> 16) ^ (this.int_value & 0xffff) ^ 0xfa425617);
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
			for (shift=48; shift >= precision; shift -= 4)
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

