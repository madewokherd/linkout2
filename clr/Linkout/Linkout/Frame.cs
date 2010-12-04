using System;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public sealed class Frame : Interpreter
	{
		public Frame ()
		{
			priv_committed = false;
			priv_frame_number = 0;
			objectlist = new LinkedList<GameObject>();
			objectdict = new Dictionary<long, LinkedListNode<GameObject>>();
		}

		private bool priv_committed;

		private int priv_frame_number;
		
		private LinkedList<GameObject> objectlist;
		private Dictionary<long, LinkedListNode<GameObject>> objectdict;
		
		public void add_object(GameObject obj)
		{
			if (priv_committed)
				throw new InvalidOperationException("This object can no longer be modified.");
			
			if (objectlist.Last != null && obj.id < objectlist.Last.Value.id)
			{
				obj.id = objectlist.Last.Value.id + 1;
			}
			
			objectdict[obj.id] = objectlist.AddLast(obj);
		}
		
		public int frame_number
		{
			set
			{
				if (priv_committed)
					throw new InvalidOperationException("This object can no longer be modified.");
				priv_frame_number = frame_number;
			}
			get
			{
				return priv_frame_number;
			}
		}
		
		public void commit()
		{
			priv_committed = true;
		}
		
		public Atom to_atom()
		{
			Atom objectlistatom = NilAtom.nil;
			LinkedListNode<GameObject> node;
			
			for (node = objectlist.Last; node != null; node = node.Previous)
			{
				Atom objectatom = node.Value.to_atom();
				objectlistatom = new ConsAtom(objectatom, objectlistatom);
			}
			
			/* FIXME: Include frame id in the state. */
			
			return new ConsAtom(new StringAtom("frame"), objectlistatom);
		}
	}
}

