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
using System.Collections;
namespace Linkout.Lisp
{
	public sealed class NilAtom : Atom
	{
		private NilAtom () : base(AtomType.Nil)
		{
		}

		private static readonly NilAtom nil_singleton = new NilAtom();
		
		public static NilAtom nil
		{
			get
			{
				return nil_singleton;
			}
		}

		public override long get_fixedpoint()
		{
			throw new NotSupportedException();
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
			return false;
		}

		public override int GetHashCode ()
		{
			return 0x6839dbce;
		}
		
		private readonly byte[] nil_str = {40, 41}; // ()
		
		public override void to_stream (System.IO.Stream output)
		{
			output.Write(nil_str, 0, 2);
		}
	}
}
