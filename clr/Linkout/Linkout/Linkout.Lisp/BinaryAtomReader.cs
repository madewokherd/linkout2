using System;
using System.IO;
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public class BinaryAtomReader : AtomReader
	{
		public BinaryAtomReader (System.IO.Stream stream)
		{
			byte[] magic = new byte[4];
			
			this.priv_stream = stream;
			
			priv_stream.Read(magic, 0, 4);
			if (magic[0] != 0x4c || magic[1] != 0x00 || magic[2] != 0x74 || magic[3] != 0x01)
			{
				throw new InvalidDataException("data does not contain a Lot binary file header");
			}
			
			cached_atom_list = new Atom[128];
			next_cache_slot = 0;
			atom_stack = new Stack<Atom>();
		}
		
		System.IO.Stream priv_stream;
		private Atom[] cached_atom_list;
		private int next_cache_slot;
		private Stack<Atom> atom_stack;
		
		public System.IO.Stream stream
		{
			get
			{
				return priv_stream;
			}
		}

		private void Cache(Atom atom)
		{
			cached_atom_list[next_cache_slot] = atom;
			
			next_cache_slot++;
			
			if (next_cache_slot == 128)
				next_cache_slot = 0;
		}
		
		private bool ProcessCommand (out Atom result)
		{
			int type = priv_stream.ReadByte();
			result = null;
			
			if (type == -1)
				return false;
			else if (type >= 0x80)
			{
				Atom cached_atom = cached_atom_list[type&0x7f];
				
				if (cached_atom == null)
					throw new InvalidDataException("Lot binary file refers to non-existent cached item");
				
				atom_stack.Push(cached_atom);
				return true;
			}
			else if (type >= 0x40)
			{
				return true;
			}
			else
			{
				switch (type)
				{
				case 0x00: // nil
					atom_stack.Push(NilAtom.nil);
					break;
				case 0x01: // number
				{
					long num_value=0;
					FixedPointAtom atom;
					byte[] num_data = new byte[6];
					int i;
					
					if (6 != priv_stream.Read(num_data, 0, 6))
					{
						throw new EndOfStreamException("Stream ended inside an integer value");
					}
					
					for (i=0; i<6; i++)
					{
						num_value |= ((long)(num_data[i]) << (i * 8));
					}
					
					atom = new FixedPointAtom(num_value);
					
					Cache(atom);
					atom_stack.Push(atom);
					
					break;
				}
				case 0x02: // string
				{
					int length=0;
					byte[] length_data = new byte[4];
					byte[] val;
					int i;
					StringAtom atom;
					
					if (4 != priv_stream.Read(length_data, 0, 4))
					{
						throw new EndOfStreamException("Stream ended inside a string length");
					}
					
					for (i=0; i<4; i++)
					{
						length |= length_data[i] << (i * 8);
					}
					
					if (length < 0)
					{
						throw new Exception("String is too long for this implementation");
					}
					
					val = new byte[length];
					if (length != priv_stream.Read(val, 0, length))
					{
						throw new EndOfStreamException("Stream ended inside string data");
					}
					
					atom = new StringAtom(val);
					
					Cache(atom);
					atom_stack.Push(atom);
					
					break;
				}
				case 0x03: // cons
				{
					Atom car, cdr, atom;
					
					car = atom_stack.Pop();
					cdr = atom_stack.Pop();
					
					atom = new ConsAtom(car, cdr);
					
					Cache(atom);
					atom_stack.Push(atom);
					
					break;
				}
				case 0x04: // yield
					result = atom_stack.Pop();
					break;
				case 0x05: // pop
					atom_stack.Pop();
					break;
				default:
					throw new NotImplementedException("Stream uses an unimplemented command");
				}
				
				return true;
			}
		}
		
		public override Atom Read ()
		{
			Atom result;
			
			do
			{
				if (!ProcessCommand(out result))
					return null;
			} while (result == null);
			
			return result;
		}
		
		public override void Close ()
		{
			priv_stream.Close();
		}
	}
}

