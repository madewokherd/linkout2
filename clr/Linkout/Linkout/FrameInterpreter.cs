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
	public class FrameInterpreter : Interpreter
	{
		public FrameInterpreter () : base()
		{
			functions[new StringAtom("box").intern()] = func_box;
			functions[new StringAtom("check-rectangle").intern()] = func_check_rectangle;
			functions[new StringAtom("getown").intern()] = func_getown;
			functions[new StringAtom("hint").intern()] = func_hint;
			functions[new StringAtom("setown").intern()] = func_setown;
			globals = new Dictionary<Atom, Atom>();
			isolated = true;
		}
		
		public Atom func_box(Atom args, Context context)
		{
			Frame frame = ((FrameContext)context).frame;

			Box result = new Box();

			Atom keyvaluepairs = args;
			while (keyvaluepairs.atomtype == AtomType.Cons)
			{
				Atom keyvaluepair = keyvaluepairs.get_car();
				result.setattr(keyvaluepair.get_car(), keyvaluepair.get_cdr().get_car());
				
				keyvaluepairs = keyvaluepairs.get_cdr();
			}
			
			frame.add_object(result);
			
			return new FixedPointAtom(result.id);
		}
		
		private struct object_atom_pair
		{
			public long id;
			public Atom atom;
			
			public object_atom_pair(long id, Atom atom)
			{
				this.id = id;
				this.atom = atom;
			}
		}
		
		public Atom func_check_rectangle(Atom args, Context context)
		{
			Frame frame = ((FrameContext)context).frame;
			Atom[] arglist = eval_n_args(args, 6, 7, "check-rectangle", context);
			LinkedList<object_atom_pair> results = new LinkedList<object_atom_pair>();
			int x, y, width, height;
			
			if (arglist == null)
				return NilAtom.nil;
			
			try
			{
				x = (int)(arglist[0].get_fixedpoint() >> 16);
				y = (int)(arglist[1].get_fixedpoint() >> 16);
				width = (int)(arglist[2].get_fixedpoint() >> 16);
				height = (int)(arglist[3].get_fixedpoint() >> 16);
			}
			catch (NotSupportedException)
			{
				// One of the arguments is not a number
				return NilAtom.nil;
			}
			
			foreach (GameObject obj in frame)
			{
				Atom[] pixel_types = obj.check_rectangle(x, y, width, height);
				
				if (pixel_types != null)
				{
					foreach (Atom pixel_type in pixel_types)
					{
						results.AddLast(new object_atom_pair(obj.id, pixel_type));
					}
				}
			}
			
			foreach (object_atom_pair result in results)
			{
				Context new_context = context.Copy();
				
				new_context.dict[arglist[4]] = new FixedPointAtom(result.id);
				new_context.dict[arglist[5]] = result.atom;
				
				Atom res = eval(arglist[6], new_context);
				
				if (res != NilAtom.nil)
					return res;
			}
			
			return NilAtom.nil;
		}
			
		public Atom func_getown(Atom args, Context context)
		{
			Frame frame = ((FrameContext)context).frame;
			Atom objectidatom = context.dict[new StringAtom("self")];
			LinkedListNode<GameObject> obj_node;
			
			args = eval_args(args, context);
			
			if (objectidatom.atomtype == AtomType.FixedPoint &&
			    frame.objectdict.TryGetValue(objectidatom.get_fixedpoint(), out obj_node))
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

		public Atom func_hint(Atom args, Context context)
		{
			Frame frame = ((FrameContext)context).frame;
			
			args = eval_args(args, context);
			
			if (!frame.committed)
				frame.add_hint(args);
			
			return NilAtom.nil;
		}
		
		public Atom func_setown(Atom args, Context context)
		{
			Frame frame = ((FrameContext)context).frame;
			Atom objectidatom;
			LinkedListNode<GameObject> obj_node;
			
			args = eval_args(args, context);
			
			if (!context.dict.TryGetValue(new StringAtom("self"), out objectidatom))
				return NilAtom.nil;
			
			if (objectidatom.atomtype == AtomType.FixedPoint &&
			    frame.objectdict.TryGetValue(objectidatom.get_fixedpoint(), out obj_node))
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
		
		public override Atom get_global(Atom name, Context context)
		{
			Frame frame = ((FrameContext)context).frame;
			Atom result;
			
			if (!frame.globals.TryGetValue(name, out result))
				result = NilAtom.nil;
			
			return result;
		}
		
		public override void set_global (Atom name, Atom val, Context context)
		{
			Frame frame = ((FrameContext)context).frame;

			if (frame.committed)
				return;
			
			if (val == NilAtom.nil)
				frame.globals.Remove(name);
			else
				frame.globals[name] = val;
		}
	}
}

