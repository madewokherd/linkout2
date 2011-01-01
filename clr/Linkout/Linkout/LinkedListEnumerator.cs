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
using System.Collections.Generic;

namespace Linkout
{
	internal class LinkedListEnumerator<T> : IEnumerator<T>
	{
		public LinkedListEnumerator (LinkedListNode<T> first_node)
		{
			this.first_node = first_node;
			this.current_node = null;
		}
		
		LinkedListNode<T> first_node;
		LinkedListNode<T> current_node;
		
		object IEnumerator.Current {
			get {
				return current_node.Value;
			}
		}
		
		public T Current {
			get {
				return current_node.Value;
			}
		}
		
		public void Dispose ()
		{
			first_node = null;
			current_node = null;
		}
		
		public void Reset ()
		{
			current_node = first_node;
		}
		
		public bool MoveNext ()
		{
			LinkedListNode<T> next_node;
			if (current_node == null)
				next_node = first_node;
			else
				next_node = current_node.Next;
			if (next_node != null)
			{
				current_node = next_node;
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}

