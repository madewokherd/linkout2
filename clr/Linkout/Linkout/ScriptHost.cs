using System;
using Linkout.Lisp;
namespace Linkout
{
	public class ScriptHost : Interpreter
	{
		public ScriptHost () : base()
		{
			functions[new StringAtom("frame")] = func_frame;
		}

		public Frame frame;
		
		private readonly StringAtom name_box = new StringAtom("box");
		
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
			
			return NilAtom.nil;
		}
	}
}

