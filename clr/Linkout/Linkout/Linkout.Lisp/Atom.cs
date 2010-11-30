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

		public abstract byte[] get_string();
		
		public abstract Atom get_car();

		public abstract Atom get_cdr();

		public static Atom from_stream(System.IO.Stream input)
		{
			int current_byte;
			
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
			         (current_byte >= 'A' && current_byte <= 'Z'))
			{
				// Bare string literal
				System.Collections.Generic.List<byte> result = new System.Collections.Generic.List<byte>();
				
				result.Add((byte)current_byte);
				
				while (true)
				{
					current_byte = input.ReadByte();
					if (current_byte == -1)
						throw new System.IO.EndOfStreamException();
					
					if (current_byte == ' ' || current_byte == '\t' || current_byte == '\r' || current_byte == '\n')
						break;

					result.Add((byte)current_byte);
				}
				
				return new StringAtom(result.ToArray());
			}
			
			throw new System.ArgumentException("invalid syntax");
		}
	}
}

