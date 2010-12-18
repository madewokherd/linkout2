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

