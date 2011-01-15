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
using Linkout.Lisp;
namespace Linkout
{
	public class ScriptHost : Interpreter
	{
		public ScriptHost () : base()
		{
			functions[new StringAtom("advance").intern()] = func_advance;
			functions[new StringAtom("frame").intern()] = func_frame;
			functions[new StringAtom("hint").intern()] = func_hint;
			functions[new StringAtom("in-frame").intern()] = func_in_frame;
			functions[new StringAtom("seek-to").intern()] = func_seek_to;
		}

		public delegate void NewFrameEvent();
		
		public event NewFrameEvent OnNewFrame;

		public delegate void FrameChangeEvent();
		
		public event FrameChangeEvent OnFrameChange;

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
			if (OnFrameChange != null)
				OnFrameChange();
		}
		
		public void advance_frame(Atom[] external_events)
		{
			if (!frame.committed)
				commit_next_frame();
			
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
				Atom instruction = args.get_car();
				
				new_frame.eval(instruction, context);
				
				args = args.get_cdr();
			}
			
			if (frame == null)
			{
				last_frame = frame = new_frame;
			
				if (OnNewFrame != null)
					OnNewFrame();
				if (OnFrameChange != null)
					OnFrameChange();
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
			if (frame.frame_number == new_framenum)
				return true;
			
			if (new_framenum < last_frame.frame_number)
			{
				frame = last_frame.get_previous_frame(new_framenum);
				if (OnFrameChange != null)
					OnFrameChange();
				
				return true;
			}
			else if (new_framenum == last_frame.frame_number)
			{
				frame = last_frame;
				if (OnFrameChange != null)
					OnFrameChange();

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
		
		public Atom func_in_frame(Atom args, Context context)
		{
			Atom[] arglist = eval_n_args(args, 1, 2, "in-frame", context);
			
			if (arglist == null || !(arglist[0] is FixedPointAtom))
				return NilAtom.nil;
			
			uint framenum = (uint)(arglist[0].get_fixedpoint() >> 16);
			
			Frame frame;
			
			if (last_frame == null)
				return NilAtom.nil;
			
			if (framenum < last_frame.frame_number)
			{
				frame = last_frame.get_previous_frame(framenum);
			}
			else if (framenum == last_frame.frame_number)
			{
				frame = last_frame;
			}
			else
			{
				return NilAtom.nil;
			}
			
			return frame.eval(arglist[1], context);
		}
	}
}

