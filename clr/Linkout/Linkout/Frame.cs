/*
 * Copyright 2010, 2011 Vincent Povirk
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
			prev_frames = null;
			objectlist = new LinkedList<GameObject>();
			objectdict = new Dictionary<long, LinkedListNode<GameObject>>();
			
			priv_hints = null;
			priv_hintlist = new List<Atom>();
			
			globals = new Dictionary<Atom, Atom>();
			
			interpreter = new FrameInterpreter();
		}

		private Frame (Frame original)
		{
			LinkedListNode<GameObject> node;

			priv_committed = false;
			priv_frame_number = original.priv_frame_number;
			prev_frames = original.prev_frames;
			
			priv_hints = null;
			priv_hintlist = new List<Atom>();
			
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
		
		private Atom priv_hints;
		private List<Atom> priv_hintlist;
		
		public void add_hint(Atom hint)
		{
			if (priv_committed)
				throw new InvalidOperationException("This object can no longer be modified.");
			
			priv_hintlist.Add(hint);
		}
		
		public Atom hints
		{
			get
			{
				if (!priv_committed || priv_hints == null)
				{
					Atom result = NilAtom.nil;
					int i;
					
					i = priv_hintlist.Count;
					
					while (i != 0)
					{
						i--;
						result = new ConsAtom(priv_hintlist[i], result);
					}
					
					if (priv_committed)
					{
						priv_hints = result;
						priv_hintlist = null;
					}
					
					return result;
				}
				return priv_hints;
			}
		}
		
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
		
		public void save_state(AtomWriter atom_writer)
		{
			Atom[] args = new Atom[3];
			
			args[0] = new StringAtom("setglobal");
			foreach (KeyValuePair<Atom, Atom> kvp in globals)
			{
				args[1] = kvp.Key;
				args[2] = kvp.Value.escape();
				
				atom_writer.Write(Atom.from_array(args));
			}
			
			interpreter.save_state(atom_writer);
			
			foreach (GameObject obj in objectlist)
			{
				// FIXME: Include the object id's somehow?
				atom_writer.Write(obj.to_atom());
			}
		}
		
		public Atom to_atom()
		{
			AtomListBuilder builder = new AtomListBuilder();
			
			save_state(builder);
			
			return new ConsAtom(new StringAtom("frame"), builder.ToAtom());
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
		
		public Atom eval(Atom args, Context context)
		{
			FrameContext new_context = new FrameContext();
			
			context.CopyToContext(new_context);
			new_context.frame = this;
			
			return interpreter.eval(args, new_context);
		}
		
		private void priv_advance(Frame prev_frame, Atom[] external_events)
		{
			LinkedListNode<GameObject> node;
			LinkedListNode<GameObject> last_node;
			
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
			
			last_node = objectlist.Last;
			
			for (node = objectlist.First; node != null; node = node.Next)
			{
				Atom objectid = new FixedPointAtom(node.Value.id);
				FrameContext context = new FrameContext();
				
				context.frame = this;
				context.dict[new StringAtom("self")] = objectid;
				
				interpreter.eval(node.Value.getattr(new StringAtom("OnFrame")), context);
				
				if (node == last_node)
					break;
			}
			
			commit();
		}
		
		public Frame advance(Atom[] external_events)
		{
			Frame new_frame = copy();
			
			interpreter.immutable = true;
			
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
		
		public Frame get_common_ancestor(Frame other)
		{
			/* Find the most recent frame in both frames' history. */
			Frame self = this;
			if (self.frame_number > other.frame_number)
				self = self.get_previous_frame(other.frame_number);
			else if (other.frame_number > self.frame_number)
				other = other.get_previous_frame(self.frame_number);

			if (self == other)
				return self;
			
			Frame result = null;
			int i=0;
			
			while (i < self.prev_frames.Length)
			{
				if (self.prev_frames[i] == other.prev_frames[i])
				{
					result = self.prev_frames[i];
					i++;
				}
				else
				{
					self = self.prev_frames[i];
					other = other.prev_frames[i];
				}
			}
			
			return result;
		}
		
		/* Frame hasing/comparison
		 * 
		 * We're not overriding the standard methods because these methods
		 * do not satisfy the proper constraints. The hash
		 * function takes into account the current frame number and history,
		 * but the comparison only accounts for frame contents.
		 *
		 * Comparing the entire history of a frame would be expensive. */
		private int priv_hash;
		private bool hash_calculated;
		
		public int frame_hash ()
		{
			int result=0;
			
			if (committed && hash_calculated)
				return priv_hash;
			
			foreach (GameObject obj in objectlist)
			{
				result = result + obj.GetHashCode();
			}
			
			foreach (KeyValuePair<Atom, Atom> kvp in globals)
			{
				result = result + (kvp.Key.GetHashCode() * (kvp.Value.GetHashCode() + 3));
			}
			
			result = result + (int)priv_frame_number;
			
			if (priv_frame_number == 0)
			{
				foreach (KeyValuePair<Atom, CustomLispFunction> kvp in interpreter.custom_functions)
				{
					CustomLispFunction f = kvp.Value;
					result = result + (f.name.GetHashCode() *
					                   (f.args.GetHashCode() + (f.eval_args_first ? 5 : 3)) *
					                   (f.body.GetHashCode() + 1));
				}
			}
			else
			{
				result = result + get_previous_frame(priv_frame_number-1).frame_hash();
			}
			
			if (committed)
			{
				priv_hash = result;
				hash_calculated = true;
			}
			
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
