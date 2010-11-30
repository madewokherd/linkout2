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

		public abstract long get_fixedpoint();

		public abstract byte[] get_string();
		
		public abstract Atom get_car();

		public abstract Atom get_cdr();

		private class FoundDotException : Exception
		{
		}
		
		private static Atom get_cdr(System.IO.Stream input)
		{
			Atom result;
			bool close_paren;
			try
			{
				result = from_stream(input, out close_paren);
			}
			catch (FoundDotException)
			{
				/* (x . y) */
				result = from_stream(input, out close_paren);
				if (!close_paren)
				{
					Atom temp = from_stream(input, out close_paren);
					if (temp != null)
					{
						/* (x . y z) */
						throw new ArgumentException("invalid syntax");
					}
				}
				return result;
			}
			if (result == null)
			{
				return NilAtom.nil;
			}
			else if (close_paren)
			{
				return new ConsAtom(result, NilAtom.nil);
			}
			else
			{
				Atom cdr = get_cdr(input);
				return new ConsAtom(result, cdr);
			}
		}
		
		private static Atom from_stream(System.IO.Stream input, out bool close_paren)
		{
			int current_byte;
			
			close_paren = false;
			
			current_byte = input.ReadByte();
			while (current_byte == ' ' || current_byte == '\t' || current_byte == '\r' || current_byte == '\n')
				current_byte = input.ReadByte();
			
			if (current_byte == -1)
				throw new System.IO.EndOfStreamException();
			
			if (current_byte == '"')
			{
				// Quoted string literal
				System.Collections.Generic.List<byte> result = new System.Collections.Generic.List<byte>();
				
				while (true)
				{
					current_byte = input.ReadByte();
					if (current_byte == -1)
						throw new System.IO.EndOfStreamException();
					
					if (current_byte == '"')
						break;
					else if (current_byte == '\\')
					{
						current_byte = input.ReadByte();
						if (current_byte == -1)
							throw new System.IO.EndOfStreamException();
					}

					result.Add((byte)current_byte);
				}
				
				return new StringAtom(result.ToArray());
			}
			else if ((current_byte >= 'a' && current_byte <= 'z') ||
			         (current_byte >= 'A' && current_byte <= 'Z') ||
			         current_byte == '+' || current_byte == '-' ||
			         current_byte == '*' || current_byte == '/' || current_byte == '%' ||
			         current_byte == '<' || current_byte == '>' || current_byte == '=')
			{
				// Bare string literal
				System.Collections.Generic.List<byte> result = new System.Collections.Generic.List<byte>();
				
				result.Add((byte)current_byte);
				
				while (true)
				{
					current_byte = input.ReadByte();
					if (current_byte == -1)
						throw new System.IO.EndOfStreamException();
					
					if (current_byte == 32 || current_byte == 9 || current_byte == 10 || current_byte == 13)
						break;

					if (current_byte == 41 /* ) */)
					{
						close_paren = true;
						break;
					}

					result.Add((byte)current_byte);
				}
				
				return new StringAtom(result.ToArray());
			}
			else if (current_byte >= '0' && current_byte <= '9')
			{
				int whole_part=current_byte - 48 /* '0' */;
				int number_base = 10;

				current_byte = input.ReadByte();
				if (current_byte == -1)
					throw new System.IO.EndOfStreamException();
		
				if (current_byte == ')')
				{
					close_paren = true;
					return new FixedPointAtom(whole_part << 16);
				}
				
				if (whole_part == 0 && current_byte == 'x')
				{
					number_base = 16;
					current_byte = input.ReadByte();
					if (current_byte == -1)
						throw new System.IO.EndOfStreamException();
				}

				while ((current_byte >= '0' && current_byte <= '9') ||
				       (current_byte >= 'a' && current_byte <= 'z') ||
				       (current_byte >= 'A' && current_byte <= 'Z'))
				{
					int digit;
					if (current_byte >= '0' && current_byte <= '9')
						digit = current_byte - 48 /* '0' */;
					else if (current_byte >= 'a' && current_byte <= 'z')
						digit = current_byte - 97 /* a */ + 10;
					else /* (current_byte >= 'A' && current_byte <= 'Z') */
						digit = current_byte - 65 /* a */ + 10;
					
					if (digit >= number_base)
						throw new ArgumentException("invalid syntax");
					
					whole_part = whole_part * number_base + digit;

					current_byte = input.ReadByte();
					if (current_byte == -1)
						throw new System.IO.EndOfStreamException();
				}
				
				if (current_byte == ')')
				{
					close_paren = true;
					return new FixedPointAtom(whole_part << 16);
				}
				
				if (current_byte == 32 || current_byte == 9 || current_byte == 10 || current_byte == 13)
					return new FixedPointAtom(whole_part << 16);
			
				if (current_byte == 46 /* . */)
					throw new NotImplementedException ();
				
				throw new ArgumentException("invalid syntax");
			}
			else if (current_byte == 40 /* ( */)
			{
				Atom car;
				Atom cdr;
				bool recursive_close_paren;
				/* cons or nil */
				
				car = from_stream(input, out recursive_close_paren);
				if (car == null)
				{
					return NilAtom.nil;
				}
				else if (recursive_close_paren)
				{
					/* Single atom enclosed in parentheses, ie (atom) */
					return new ConsAtom(car, NilAtom.nil);
				}

				cdr = get_cdr(input);
				
				return new ConsAtom(car, cdr);
			}
			else if (current_byte == 41 /* ) */)
			{
				close_paren = true;
				return null;
			}
			
			throw new System.ArgumentException("invalid syntax");
		}
		
		public static Atom from_stream(System.IO.Stream input)
		{
			bool close_paren;
			Atom result;
			
			try
			{
				result = from_stream(input, out close_paren);
			}
			catch (FoundDotException)
			{
				throw new System.ArgumentException("unexpected dot");
			}
			if (close_paren)
				throw new System.ArgumentException("unexpected closing parenthesis");
			return result;
		}
		
		public abstract void to_stream(System.IO.Stream output);

		public void write_to_stream(System.IO.Stream output)
		{
			to_stream(output);
			output.WriteByte(10);
		}
	}
}

