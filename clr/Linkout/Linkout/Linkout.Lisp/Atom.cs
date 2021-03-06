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
using System.Collections.Generic;
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

		public abstract bool is_true();
		
		private static Atom get_cdr(System.IO.Stream input)
		{
			Atom result;
			bool close_paren, found_dot;
			result = from_stream(input, out close_paren, out found_dot);
			if (found_dot)
			{
				/* (x . y) */
				result = from_stream(input, out close_paren);
				if (!close_paren)
				{
					if (result == null)
						throw new System.IO.EndOfStreamException();
					
					Atom temp = from_stream(input, out close_paren);
					if (temp != null)
					{
						/* (x . y z) */
						throw new ArgumentException("invalid syntax");
					}
					else if (!close_paren)
					{
						throw new System.IO.EndOfStreamException();
					}
				}
				return result;
			}
			if (result == null)
			{
				if (close_paren)
					return NilAtom.nil;
				else
					throw new System.IO.EndOfStreamException();
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
		
		private static Atom parse_number(System.IO.Stream input, int first_byte, bool negate, out bool close_paren)
		{
			long whole_part=first_byte - 48 /* '0' */;
			int number_base = 10;
			int current_byte;
			long num_result;
			long numerator, denominator, fractional_part=0;

			close_paren = false;
			
			current_byte = input.ReadByte();
			
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

				if (whole_part > 0x80000000 || (!negate && whole_part == 0x80000000))
					throw new OverflowException();
				
				current_byte = input.ReadByte();
			}
			
			if (current_byte == '.')
			{
				numerator = 0;
				denominator = 1;
				current_byte = input.ReadByte();
				while ((current_byte >= '0' && current_byte <= '9') ||
				       (current_byte >= 'a' && current_byte <= 'z') ||
				       (current_byte >= 'A' && current_byte <= 'Z'))
				{
					long new_denominator = denominator * number_base;
					
					if (new_denominator > 0x1000000000000)
						throw new OverflowException();
					
					denominator = new_denominator;
					
					int digit;
					if (current_byte >= '0' && current_byte <= '9')
						digit = current_byte - 48 /* '0' */;
					else if (current_byte >= 'a' && current_byte <= 'z')
						digit = current_byte - 97 /* a */ + 10;
					else /* (current_byte >= 'A' && current_byte <= 'Z') */
						digit = current_byte - 65 /* a */ + 10;
					
					if (digit >= number_base)
						throw new ArgumentException("invalid syntax");
					
					numerator = numerator * number_base + digit;
	
					current_byte = input.ReadByte();
				}
				
				fractional_part = (numerator << 16) / denominator;
				
				if (negate && ((numerator << 16) % denominator) > 0)
				{
					fractional_part += 1;
				}
			}
			
			num_result = (whole_part << 16) + fractional_part;
			
			if (negate)
				num_result = -num_result;
			
			if (current_byte == ')')
			{
				close_paren = true;
				return new FixedPointAtom(num_result);
			}
			
			if (current_byte == 32 || current_byte == 9 || current_byte == 10 || current_byte == 13 || current_byte == -1)
				return new FixedPointAtom(num_result);
			
			throw new ArgumentException("invalid syntax");
		}
		
		private static Atom from_stream(System.IO.Stream input, out bool close_paren, out bool found_dot)
		{
			int current_byte;
			
			close_paren = false;
			found_dot = false;
			
			current_byte = input.ReadByte();
			while (current_byte == ' ' || current_byte == '\t' || current_byte == '\r' || current_byte == '\n' || current_byte == ';')
			{
				if (current_byte == ';')
				{
					// Single-line comment
					while (current_byte != -1 && current_byte != '\n' && current_byte != '\r')
						current_byte = input.ReadByte();
				}
				current_byte = input.ReadByte();
			}
			
			if (current_byte == -1)
				return null;
			
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
				
				return new StringAtom(result.ToArray()).intern();
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
				
				bool maybe_number = (current_byte == '-');
				
				while (true)
				{
					current_byte = input.ReadByte();
					if (current_byte == -1)
						throw new System.IO.EndOfStreamException();
					
					if (maybe_number && current_byte >= '0' && current_byte <= '9')
					{
						return parse_number(input, current_byte, true, out close_paren);
					}
					
					maybe_number = false;
					
					if (current_byte == 32 || current_byte == 9 || current_byte == 10 || current_byte == 13)
						break;

					if (current_byte == 41 /* ) */)
					{
						close_paren = true;
						break;
					}

					result.Add((byte)current_byte);
				}
				
				return new StringAtom(result.ToArray()).intern();
			}
			else if (current_byte >= '0' && current_byte <= '9')
			{
				return parse_number(input, current_byte, false, out close_paren);
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
					if (recursive_close_paren)
						return NilAtom.nil;
					else
						throw new System.IO.EndOfStreamException();
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
			else if (current_byte == '.')
			{
				found_dot = true;
				return null;
			}
			
			throw new System.ArgumentException("invalid syntax");
		}

		private static Atom from_stream(System.IO.Stream input, out bool close_paren)
		{
			bool found_dot;
			Atom result;
			
			result = from_stream(input, out close_paren, out found_dot);

			if (found_dot)
				throw new System.ArgumentException("unexpected dot");
			
			return result;
		}
		
		public static Atom from_stream(System.IO.Stream input)
		{
			bool close_paren, found_dot;
			Atom result;
			
			result = from_stream(input, out close_paren, out found_dot);
			
			if (found_dot)
				throw new System.ArgumentException("unexpected dot");
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
		
		public override string ToString ()
		{
			System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(false, false);
			
			using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
			{
				to_stream(stream);
				return encoding.GetString(stream.GetBuffer());
			}
		}
		
		public static void pattern_match(Dictionary<Atom, Atom> names, Atom pattern, Atom atom)
		{
			if (pattern.atomtype == AtomType.String)
			{
				names[pattern] = atom;
			}
			else if (pattern.atomtype == AtomType.Cons && atom.atomtype == AtomType.Cons)
			{
				pattern_match(names, pattern.get_car(), atom.get_car());
				pattern_match(names, pattern.get_cdr(), atom.get_cdr());
			}
		}
		
		public static Atom from_array(Atom[] array)
		{
			if (array == null)
				return NilAtom.nil;
			
			int i=array.Length;
			Atom result = NilAtom.nil;
			
			while (i > 0)
			{
				i = i - 1;
				
				result = new ConsAtom(array[i], result);
			}
			
			return result;
		}
		
		public Atom escape()
		{
			if (this is ConsAtom)
				return new ConsAtom(new StringAtom("quot"), this);
			else
				return this;
		}
	}
}

