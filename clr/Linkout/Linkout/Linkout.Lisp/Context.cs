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
	public class Context
	{
		public Dictionary<Atom, Atom> dict;
		
		public Context ()
		{
			dict = new Dictionary<Atom, Atom>();
		}

		public virtual void CopyToContext(Context result)
		{
			result.dict = new Dictionary<Atom, Atom>(this.dict);
		}
		
		public virtual Context Copy()
		{
			Context result = new Context();
			
			CopyToContext(result);
			
			return result;
		}
		
		public Atom get_local(Atom name)
		{
			Atom result = NilAtom.nil;
			
			dict.TryGetValue(name, out result);
			
			return result;
		}
		
		
	}
}

