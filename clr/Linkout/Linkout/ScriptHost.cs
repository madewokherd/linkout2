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
			functions[new StringAtom("hint")] = func_hint;
			functions[new StringAtom("seek-to")] = func_seek_to;
		}

		public delegate void NewFrameEvent();
		
		public event NewFrameEvent OnNewFrame;

		public delegate void ContentCheckFailEvent(Frame expected, string message);
		
		public event ContentCheckFailEvent OnContentCheckFail;
		
		public delegate void HintEvent(Atom args);
		
		public event HintEvent OnHint;

		
		public Frame frame;

		public Frame last_frame;

		
		private void commit_next_frame()
		{
			frame.commit();
			if (OnNewFrame != null)
				OnNewFrame();
		}
		
		private readonly StringAtom name_box = new StringAtom("box");
		
		public void advance_frame(Atom[] external_events)
		{
			last_frame = frame = frame.advance(external_events);
			
			commit_next_frame();
		}
		
		public void advance_frame()
		{
			advance_frame(null);
		}
		
		public Atom func_frame(Atom args, Context context)
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
			{
				last_frame = frame = new_frame;
			
				commit_next_frame();
			}
			else
			{
				string message;
				if (!frame.frame_content_equals(new_frame, out message))
				{
					if (OnContentCheckFail != null)
						OnContentCheckFail(new_frame, message);
					else
						Console.WriteLine(String.Format("Frame content verification failed: {0}", message));
				}
			}
			
			return NilAtom.nil;
		}

		public Atom func_advance(Atom args, Context context)
		{
			int i;
			Atom[] external_events = null;
			
			if (frame == null)
				throw new InvalidOperationException("Create a frame first");
			
			if (args.atomtype == AtomType.Cons)
			{
				external_events = new Atom[((ConsAtom)args).GetLength()];
				
				for (i=0; i<external_events.Length; i++)
				{
					external_events[i] = args.get_car();
					args = args.get_cdr();
				}
			}

			advance_frame(external_events);
			
			return NilAtom.nil;
		}
		
		public bool seek_to(uint new_framenum)
		{
			if (new_framenum < last_frame.frame_number)
			{
				frame = last_frame.get_previous_frame(new_framenum);
				return true;
			}
			else if (new_framenum == last_frame.frame_number)
			{
				frame = last_frame;
				return true;
			}
			else
				return false;
		}
		
		public Atom func_seek_to(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			if (frame == null)
				throw new InvalidOperationException("Create a frame first");
			
			if (args.atomtype == AtomType.Cons)
			{
				Atom count_atom = args.get_car();
				if (count_atom.atomtype == AtomType.FixedPoint)
				{
					uint new_framenum = (uint)(count_atom.get_fixedpoint() >> 16);
					seek_to(new_framenum);
				}
			}
			
			return NilAtom.nil;
		}
		
		public Atom func_hint(Atom args, Context context)
		{
			args = eval_args(args, context);

			if (OnHint != null)
				OnHint(args);
			
			return NilAtom.nil;
		}
	}
}

