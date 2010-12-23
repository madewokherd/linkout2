using System;
using System.Collections;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public sealed class Frame : Interpreter, IEnumerable<GameObject>
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
			prev_frames = null;
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
			prev_frames = original.prev_frames;
			
			objectlist = new LinkedList<GameObject>();
			objectdict = new Dictionary<long, LinkedListNode<GameObject>>();

			for (node = original.objectlist.First; node != null; node = node.Next)
			{
				add_object(node.Value.copy());
			}
		}
		
		private bool priv_committed;

		private uint priv_frame_number;
		
		private LinkedList<GameObject> objectlist;
		private Dictionary<long, LinkedListNode<GameObject>> objectdict;
		
		private Atom[] priv_external_events;
		
		public Atom[] external_events
		{
			get
			{
				return priv_external_events;
			}
		}
		
		/* History */
		private int prev_frame_hash;
		private Frame[] prev_frames;
		
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
		
		public uint frame_number
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
		
		private Frame[] build_frame_history(Frame prev_frame)
		{
			uint this_frame_number = prev_frame.priv_frame_number+1;
			int array_size=0;
			uint i=0x80000000;
			Frame[] result;
			
			/* determine how many previous frames to store */
			while (i != 0)
			{
				if ((this_frame_number & i) != 0)
					array_size++;
				i = i >> 1;
			}
			
			result = new Frame[array_size];
			
			for (i=0; i<array_size-1; i++)
				result[i] = prev_frame.prev_frames[i];
			
			result[array_size-1] = prev_frame;
			
			return result;
		}
		
		private void priv_advance(Frame prev_frame, Atom[] external_events)
		{
			LinkedListNode<GameObject> node;
			
			priv_frame_number += 1;
			
			this.prev_frames = build_frame_history(prev_frame);
			
			if (external_events != null)
			{
				Locals locals = new Locals();
				
				priv_external_events = external_events;
				
				foreach (Atom external_event in external_events)
				{
					eval(external_event, locals, this);
				}
			}
			
			for (node = objectlist.Last; node != null; node = node.Previous)
			{
				Atom objectid = new FixedPointAtom(node.Value.id);
				Locals locals = new Locals();
				
				locals.dict[new StringAtom("self")] = objectid;
				
				eval(node.Value.getattr(new StringAtom("OnFrame")), locals, this);
			}
			
			commit();
		}
		
		public Frame advance(Atom[] external_events)
		{
			Frame new_frame = copy();
			
			new_frame.priv_advance(this, external_events);
			
			return new_frame;
		}

		public Frame advance()
		{
			return advance(null);
		}
		
		public Frame get_previous_frame(uint frameid)
		{
			if (frameid >= this.priv_frame_number)
				throw new ArgumentOutOfRangeException("frameid", frameid, String.Format("Must be less than {0}", this.priv_frame_number));
			
			Frame[] prev = this.prev_frames;
			int i=0;
			
			while (true)
			{
				Frame prev_frame = prev[i];
				if (frameid == prev_frame.priv_frame_number)
					return prev_frame;
				else if (frameid < prev_frame.priv_frame_number)
					prev = prev_frame.prev_frames;
				else
					i++;
			}
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
			Atom objectidatom;
			LinkedListNode<GameObject> obj_node;
			
			args = self.eval_args(args, locals, user_data);
			
			if (!locals.dict.TryGetValue(new StringAtom("self"), out objectidatom))
				return NilAtom.nil;
			
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
			
			result = result + (int)priv_frame_number + prev_frame_hash;
			
			// FIXME: Include custom functions
			
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
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return (IEnumerator)GetEnumerator();
		}
		
		public IEnumerator<GameObject> GetEnumerator ()
		{
			LinkedListEnumerator<GameObject> result = new LinkedListEnumerator<GameObject>(objectlist.First);
			
			return result;
		}
		
		public override void add_custom_function (Atom args, bool eval_args_first)
		{
			if (priv_frame_number != 0)
				/* Functions may only be defined on the first frame. */
				return;
			
			base.add_custom_function (args, eval_args_first);
		}
		
		public override void set_global (Atom name, Atom val)
		{
			if (priv_committed)
				return;
			
			base.set_global (name, val);
		}
	}
}
