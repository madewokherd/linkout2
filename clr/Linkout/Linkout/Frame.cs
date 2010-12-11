using System;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public sealed class Frame : Interpreter
	{
		private void set_functions()
		{
			functions[new StringAtom("getown")] = func_getown;
			functions[new StringAtom("setown")] = func_setown;
		}
		
		public Frame ()
		{
			priv_committed = false;
			priv_frame_number = 0;
			prev_frame_hash = 0;
			objectlist = new LinkedList<GameObject>();
			objectdict = new Dictionary<long, LinkedListNode<GameObject>>();
			
			set_functions();
		}

		private Frame (Frame original) : base(original.functions)
		{
			LinkedListNode<GameObject> node;

			priv_committed = false;
			priv_frame_number = original.priv_frame_number;
			prev_frame_hash = original.GetHashCode();
			
			objectlist = new LinkedList<GameObject>();
			objectdict = new Dictionary<long, LinkedListNode<GameObject>>();

			for (node = original.objectlist.First; node != null; node = node.Next)
			{
				add_object(node.Value.copy());
			}
		}
		
		private bool priv_committed;

		private int priv_frame_number;
		
		private LinkedList<GameObject> objectlist;
		private Dictionary<long, LinkedListNode<GameObject>> objectdict;
		
		private int prev_frame_hash;
		
		public void add_object(GameObject obj)
		{
			if (priv_committed)
				throw new InvalidOperationException("This object can no longer be modified.");
			
			if (objectlist.Last != null && obj.id <= objectlist.Last.Value.id)
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
		
		public Frame copy()
		{
			return new Frame(this);
		}
		
		private void priv_advance()
		{
			LinkedListNode<GameObject> node;
			
			priv_frame_number += 1;
			
			for (node = objectlist.Last; node != null; node = node.Previous)
			{
				Atom objectid = new FixedPointAtom(node.Value.id);
				Locals locals = new Locals();
				
				locals.dict[new StringAtom("self")] = objectid;
				
				eval(node.Value.getattr(new StringAtom("OnFrame")), locals, this);
			}
			
			commit();
		}
		
		public Frame advance()
		{
			Frame new_frame = copy();
			
			new_frame.priv_advance();
			
			return new_frame;
		}

		public static Atom func_getown(Atom args, Locals locals, object user_data)
		{
			Frame self = (Frame)user_data;
			Atom objectidatom = locals.dict[new StringAtom("self")];
			LinkedListNode<GameObject> obj_node;
			
			args = self.eval_args(args, locals, user_data);
			
			if (objectidatom.atomtype == AtomType.FixedPoint &&
			    self.objectdict.TryGetValue(objectidatom.get_fixedpoint(), out obj_node))
			{
				GameObject obj = obj_node.Value;
				if (args.atomtype == AtomType.Cons)
				{
					Atom key = args.get_car();
					return obj.getattr(key);
				}
			}
			
			return NilAtom.nil;
		}

		public static Atom func_setown(Atom args, Locals locals, object user_data)
		{
			Frame self = (Frame)user_data;
			Atom objectidatom = locals.dict[new StringAtom("self")];
			LinkedListNode<GameObject> obj_node;
			
			args = self.eval_args(args, locals, user_data);
			
			if (objectidatom.atomtype == AtomType.FixedPoint &&
			    self.objectdict.TryGetValue(objectidatom.get_fixedpoint(), out obj_node))
			{
				GameObject obj = obj_node.Value;
				while (args.atomtype == AtomType.Cons)
				{
					Atom key = args.get_car();
					args = args.get_cdr();
					if (args.atomtype != AtomType.Cons)
						break;
					Atom val = args.get_car();
					args = args.get_cdr();
					
					obj.setattr(key, val);
				}
			}
			
			return NilAtom.nil;
		}
		
		/* Frame hasing/comparison
		 * 
		 * We're not overriding the standard methods because these methods
		 * do not satisfy the proper constraints. The hash
		 * function takes into account the current frame number and history,
		 * but the comparison only accounts for frame contents.
		 *
		 * Comparing the entire history of a frame would be expensive. */
		public int frame_hash ()
		{
			int result=0;
			
			foreach (GameObject obj in objectlist)
			{
				result = result + obj.GetHashCode();
			}
			
			result = result + priv_frame_number + prev_frame_hash;
			
			return result;
		}
		
		public bool frame_content_equals (Frame obj, out string message)
		{
			if (this.objectlist.Count != obj.objectlist.Count)
			{
				message = String.Format("Object count {0} != {1}", this.objectlist.Count, obj.objectlist.Count);
				return false;
			}
			
			LinkedListNode<GameObject> this_node, obj_node;
			this_node = this.objectlist.First;
			obj_node = obj.objectlist.First;
			
			while (this_node != null)
			{
				if (!this_node.Value.Equals(obj_node.Value, out message))
					return false;
				
				this_node = this_node.Next;
				obj_node = obj_node.Next;
			}
			
			message = "";
			return true;
		}
	}
}

