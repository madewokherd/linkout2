/*
 * Copyright 2011 Vincent Povirk
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
namespace Linkout.Lisp
{
	public class StreamAtomReader : AtomReader
	{
		public StreamAtomReader (System.IO.Stream stream)
		{
			this.priv_stream = stream;
		}
		
		private System.IO.Stream priv_stream;
		
		public System.IO.Stream stream
		{
			get
			{
				return priv_stream;
			}
		}
		
		public override Atom Read ()
		{
			return Atom.from_stream(priv_stream);
		}
		
		public override void Close ()
		{
			priv_stream.Close();
		}
	}
}
