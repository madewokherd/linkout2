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
	public class BinaryAtomWriter : AtomWriter
	{
		public BinaryAtomWriter (System.IO.Stream stream)
		{
			byte[] magic = {0x4c,0x00,0x74,0x02};
			
			priv_stream = stream;
			cached_atoms = new Dictionary<Atom, int>();
			cached_atom_list = new Atom[128];
			next_cache_slot = 0;
			
			stream.Write(magic, 0, 4);
		}

		private System.IO.Stream priv_stream;
		private Dictionary<Atom, int> cached_atoms;
		private Atom[] cached_atom_list;
		private int next_cache_slot;
		
		public System.IO.Stream stream
		{
			get
			{
				return priv_stream;
			}
		}
		
		private void Cache (Atom data)
		{
			if (cached_atom_list[next_cache_slot] != null)
			{
				cached_atoms.Remove(cached_atom_list[next_cache_slot]);
			}
			
			cached_atom_list[next_cache_slot] = data;
			cached_atoms[data] = (byte)next_cache_slot;
			
			next_cache_slot += 1;
			if (next_cache_slot == 128)
			{
				next_cache_slot = 0;
			}
		}
		
		private void Push (Atom data)
		{
			int type;
			
			if (data is NilAtom)
			{
				priv_stream.WriteByte(0x00);
			}
			else if (cached_atoms.TryGetValue(data, out type))
			{
				priv_stream.WriteByte((byte)(type | 0x80));
			}
			else if (data is FixedPointAtom)
			{
				long val = data.get_fixedpoint();
				int i;
				priv_stream.WriteByte(0x01);
				for (i=0; i<6; i++)
				{
					priv_stream.WriteByte((byte)val);
					val = val >> 8;
				}
				Cache(data);
			}
			else if (data is StringAtom)
			{
				byte[] contents = data.get_string();
				int length = contents.Length;
				int i;
				
				priv_stream.WriteByte(0x02);
				for (i=0; i<4; i++)
				{
					priv_stream.WriteByte((byte)length);
					length = length >> 8;
				}
				priv_stream.Write(contents, 0, contents.Length);
				Cache(data);
			}
			else if (data is ConsAtom)
			{
				priv_stream.WriteByte(0x03);
				Push(data.get_car());
				Push(data.get_cdr());
				Cache(data);
			}
		}
		
		public override void Write (Atom data)
		{
			Push(data);
		}
		
		public override void Flush ()
		{
			priv_stream.Flush();
		}
		
		
		public override void Close ()
		{
			priv_stream.Close();
		}
	}
}

