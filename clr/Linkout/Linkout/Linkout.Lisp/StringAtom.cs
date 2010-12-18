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

		public StringAtom (string string_value) : base(AtomType.String)
		{
			System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
			this.string_value = encoding.GetBytes(string_value);
		}

		public override long get_fixedpoint()
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
		
		public override bool is_true ()
		{
			return true;
		}
	
		public override bool Equals (object obj)
		{
			int i;
			if (obj.GetType() != this.GetType())
				return false;
			StringAtom oth = (StringAtom)obj;
			if (this.string_value.Length != oth.string_value.Length)
				return false;
			for (i=0; i<this.string_value.Length; i++)
				if (this.string_value[i] != oth.string_value[i])
					return false;
			return true;
		}
		
		public override int GetHashCode ()
		{
			int result = -0x3465eda6, i;
			for (i=0; i<this.string_value.Length; i++)
				result = result * 33 + i;
			return result;
		}
		
		private bool is_simple_literal()
		{
			int i;
			if ((this.string_value[0] < 'a' || this.string_value[0] > 'z') &&
			    (this.string_value[0] < 'A' || this.string_value[0] > 'Z') &&
			    this.string_value[0] != '+' && this.string_value[0] != '-' &&
			    this.string_value[0] != '*' && this.string_value[0] != '/' && this.string_value[0] != '%' &&
			    this.string_value[0] != '<' && this.string_value[0] != '>' && this.string_value[0] != '=')
				return false;
			for (i=0; i<this.string_value.Length; i++)
			{
				if (this.string_value[i] <= 32 || this.string_value[i] == 41 /* ) */)
					return false;
			}
			return true;
		}
		
		public override void to_stream (System.IO.Stream output)
		{
			if (this.string_value.Length <= 127 && is_simple_literal())
			{
				output.Write(this.string_value, 0, this.string_value.Length);
			}
			else
			{
				/* TODO: Define and use a compressed literal syntax. */
				int i;
				output.WriteByte(34); /* " */
				for (i=0; i<this.string_value.Length; i++)
				{
					if (this.string_value[i] == 92 /* \ */ || this.string_value[i] == 34 /* " */)
						output.WriteByte(92);
					output.WriteByte(this.string_value[i]);
				}
				output.WriteByte(34); /* " */
			}
		}
	}
}
