using System;
using System.Collections.Generic;
using Linkout.Lisp;

namespace Linkout
{
	public class FrameInterpreter : Interpreter
	{
		public FrameInterpreter () : base()
		{
			functions[new StringAtom("getown")] = func_getown;
			functions[new StringAtom("setown")] = func_setown;
			globals = new Dictionary<Atom, Atom>();
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

