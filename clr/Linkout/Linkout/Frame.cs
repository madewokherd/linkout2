using System;
using System.Collections;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public sealed class Frame : IEnumerable<GameObject>
	{
		public Frame ()
		{
			priv_committed = false;
			priv_frame_number = 0;
			prev_frame_hash = 0;
			prev_frames = null;
			objectlist = new LinkedList<GameObject>();
			objectdict = new Dictionary<long, LinkedListNode<GameObject>>();
			
			globals = new Dictionary<Atom, Atom>();
			
			interpreter = new FrameInterpreter();
		}

		private Frame (Frame original)
		{
			LinkedListNode<GameObject> node;

			priv_committed = false;
			priv_frame_number = original.priv_frame_number;
			prev_frame_hash = original.GetHashCode();
			prev_frames = original.prev_frames;
			
			globals = new Dictionary<Atom, Atom>(original.globals);
			interpreter = original.interpreter;
			
			objectlist = new LinkedList<GameObject>();
			objectdict = new Dictionary<long, LinkedListNode<GameObject>>();

			for (node = original.objectlist.First; node != null; node = node.Next)
			{
				add_object(node.Value.copy());
			}
		}
		
		private FrameInterpreter interpreter;
		
		private bool priv_committed;

		private uint priv_frame_number;
		
		internal LinkedList<GameObject> objectlist;
		internal Dictionary<long, LinkedListNode<GameObject>> objectdict;
		
		public bool committed
		{
			get
			{
				return priv_committed;
			}
		}
		
		private Atom[] priv_external_events;
		
		public Atom[] external_events
		{
			get
			{
				return priv_external_events;
			}
		}
		
		internal Dictionary<Atom, Atom> globals;
		
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
		
		public void eval(Atom args, Context context)
		{
			FrameContext new_context = new FrameContext();
			
			context.CopyToContext(new_context);
			new_context.frame = this;
			
			interpreter.eval(args, new_context);
		}
		
		private void priv_advance(Frame prev_frame, Atom[] external_events)
		{
			LinkedListNode<GameObject> node;
			
			priv_frame_number += 1;
			
			this.prev_frames = build_frame_history(prev_frame);
			
			if (external_events != null)
			{
				FrameContext context = new FrameContext();
				
				context.frame = this;
				
				priv_external_events = external_events;
				
				foreach (Atom external_event in external_events)
				{
					interpreter.eval(external_event, context);
				}
			}
			
			for (node = objectlist.Last; node != null; node = node.Previous)
			{
				Atom objectid = new FixedPointAtom(node.Value.id);
				FrameContext context = new FrameContext();
				
				context.frame = this;
				context.dict[new StringAtom("self")] = objectid;
				
				interpreter.eval(node.Value.getattr(new StringAtom("OnFrame")), context);
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
	}
}
