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
using Linkout.Lisp;
namespace Linkout
{
	public sealed class Box : GameObject
	{
		public Box ()
		{
		}
		
		private Box (Box original) : base(original)
		{
		}
		
		public override Atom to_atom ()
		{
			return new ConsAtom(new StringAtom("box"), attributes_to_atom());
		}
		
		public override GameObject copy ()
		{
			return new Box(this);
		}
		
		private static Atom x_atom = new StringAtom("x");
		private static Atom y_atom = new StringAtom("y");
		private static Atom width_atom = new StringAtom("width");
		private static Atom height_atom = new StringAtom("height");
		
		public override Atom[] check_rectangle (int x, int y, int width, int height)
		{
			int my_x, my_y, my_width, my_height;
			
			try
			{
				my_x = (int)(getattr(x_atom).get_fixedpoint() >> 16);
				my_y = (int)(getattr(y_atom).get_fixedpoint() >> 16);
				my_width = (int)(getattr(width_atom).get_fixedpoint() >> 16);
				my_height = (int)(getattr(height_atom).get_fixedpoint() >> 16);
			}
			catch (NotSupportedException)
			{
				// No such attribute or not a number
				return null;
			}
			
			if (my_width <= 0 || my_height <= 0 || width <= 0 || height <= 0 ||
			    (my_x >= x + width) || (x >= my_x + my_width) ||
			    (my_y >= y + height) || (y >= my_y + my_height))
			{
				// Rectangles do not intersect
				return null;
			}
			else
			{
				Atom[] result = new Atom[1];
				result[0] = FixedPointAtom.One;
				return result;
			}
		}
		
		public override int GetHashCode ()
		{
			return get_base_hash() ^ 0x7b68d637;
		}
	}
}

