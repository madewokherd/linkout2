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
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public class StringAtom : Atom
	{
		public readonly byte[] string_value;
		
		public StringAtom (byte[] string_value) : base(AtomType.String)
		{
			this.string_value = string_value;
			this.calculated_hashcode = false;
		}

		public StringAtom (string string_value) : base(AtomType.String)
		{
			System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
			this.string_value = encoding.GetBytes(string_value);
			this.calculated_hashcode = false;
		}

		private static Dictionary<StringAtom, StringAtom> interned_atoms = new Dictionary<StringAtom, StringAtom>();
		
		public StringAtom intern()
		{
			StringAtom result;
			
			if (!interned_atoms.TryGetValue(this, out result))
			{
				try
				{
					interned_atoms.Add(this, this);
					return this;
				}
				catch (ArgumentException)
				{
					return interned_atoms[this];
				}
			}
			
			return result;
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
			if (Object.ReferenceEquals(this, obj))
				return true;
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
		
		private bool calculated_hashcode;
		private int hashcode;
		
		public override int GetHashCode ()
		{
			int result = -0x3465eda6, i;
			
			if (calculated_hashcode)
				return hashcode;
			
			for (i=0; i<this.string_value.Length; i++)
				result = result * 33 + i;
			
			hashcode = result;
			calculated_hashcode = true;
			
			return result;
		}
		
		private bool is_simple_literal()
		{
			int i;
			if (this.string_value.Length == 0)
				return false;
			if ((this.string_value[0] < 'a' || this.string_value[0] > 'z') &&
			    (this.string_value[0] < 'A' || this.string_value[0] > 'Z') &&
			    this.string_value[0] != '+' && this.string_value[0] != '-' &&
			    this.string_value[0] != '*' && this.string_value[0] != '/' && this.string_value[0] != '%' &&
			    this.string_value[0] != '<' && this.string_value[0] != '>' && this.string_value[0] != '=')
				return false;
			if (this.string_value[0] == '-' && this.string_value.Length != 1 &&
			    this.string_value[1] >= '0' && this.string_value[1] <= '9')
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
