using System;
using Linkout.Lisp;
namespace Linkout
{
	public class ScriptHost : Interpreter
	{
		public ScriptHost () : base()
		{
			functions[new StringAtom("advance")] = func_advance;
			functions[new StringAtom("frame")] = func_frame;
		}

		public delegate void NewFrameEvent();
		
		public event NewFrameEvent OnNewFrame;
		
		public Frame frame;
		
		private void commit_next_frame()
		{
			frame.commit();
			if (OnNewFrame != null)
				OnNewFrame();
		}
		
		private readonly StringAtom name_box = new StringAtom("box");
		
		public void advance_frame()
		{
			frame = frame.advance();
			
			commit_next_frame();
		}
		
		public Atom func_frame(Atom args, Locals locals, object user_data)
		{
			Frame new_frame;
			
			new_frame = new Frame();
			
			while (args.atomtype == AtomType.Cons)
			{
				Atom obj = args.get_car();
				
				if (obj.atomtype == AtomType.Cons)
				{
					Atom typename = obj.get_car();
					GameObject gameobj;
					if (typename.Equals(name_box))
					{
						gameobj = new Box();
					}
					else if (typename.atomtype == AtomType.String)
					{
						throw new NotSupportedException(String.Format("Object type {0} unrecognized", typename));
					}
					else
					{
						args = args.get_cdr();
						continue;
					}
					
					Atom keyvaluepairs = obj.get_cdr();
					while (keyvaluepairs.atomtype == AtomType.Cons)
					{
						Atom keyvaluepair = keyvaluepairs.get_car();
						gameobj.setattr(keyvaluepair.get_car(), keyvaluepair.get_cdr().get_car());
						
						keyvaluepairs = keyvaluepairs.get_cdr();
					}
					
					new_frame.add_object(gameobj);
				}
				else
				{
					throw new NotSupportedException(String.Format("Cannot create object from atom {0}", obj));
				}
				
				args = args.get_cdr();
			}
			
			if (frame == null)
				frame = new_frame;
			else
				throw new NotImplementedException("Can't verify frame state yet.");
			
			commit_next_frame();
			
			return NilAtom.nil;
		}

		public Atom func_advance(Atom args, Locals locals, object user_data)
		{
			int advance_count = 1, i;
			
			args = eval_args(args, locals, user_data);
			
			if (frame == null)
				throw new InvalidOperationException("Create a frame first");
			
			if (args.atomtype == AtomType.Cons)
			{
				Atom count_atom = args.get_car();
				if (count_atom.atomtype == AtomType.FixedPoint)
				{
					advance_count = (int)(count_atom.get_fixedpoint() >> 16);
				}
			}
			
			for (i=0; i<advance_count; i++)
			{
				advance_frame();
			}
			
			return NilAtom.nil;
		}
	}
}

