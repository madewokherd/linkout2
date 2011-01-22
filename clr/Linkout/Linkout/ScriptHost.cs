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
			functions[atom_advance] = func_advance;
			functions[new StringAtom("frame").intern()] = func_frame;
			functions[atom_hint] = func_hint;
			functions[new StringAtom("in-frame").intern()] = func_in_frame;
			functions[atom_seek_to] = func_seek_to;
		}

		readonly Atom atom_advance = new StringAtom("advance").intern();
		readonly Atom atom_checksum = new StringAtom("checksum").intern();
		readonly Atom atom_hint = new StringAtom("hint").intern();
		readonly Atom atom_seek_to = new StringAtom("seek-to").intern();
		
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
		
		public void save_state (AtomWriter atom_writer, int checksum_frequency)
		{
			base.save_state(atom_writer);
			
			if (frame != null)
			{
				Frame first_frame;
				if (frame.frame_number == 0)
					first_frame = frame;
				else
					first_frame = frame.get_previous_frame(0);
				
				atom_writer.Write(first_frame.to_atom());
				
				for (uint i=1; i<last_frame.frame_number; i++)
				{
					Atom external_events = Atom.from_array(last_frame.get_previous_frame(i).external_events);
					atom_writer.Write(new ConsAtom(atom_advance, external_events));
					
					if (i % checksum_frequency == 0)
					{
						Atom[] list = new Atom[3];
						list[0] = atom_hint;
						list[1] = atom_checksum;
						list[2] = new FixedPointAtom((long)last_frame.get_previous_frame(i).frame_hash() << 16);
						
						atom_writer.Write(Atom.from_array(list));
					}
				}
				
				if (last_frame.frame_number != 0)
				{
					Atom external_events = Atom.from_array(last_frame.external_events);
					atom_writer.Write(new ConsAtom(atom_advance, external_events));
				}
				
				if (frame.frame_number != last_frame.frame_number)
				{
					atom_writer.Write(new ConsAtom(atom_seek_to, new ConsAtom(new FixedPointAtom(frame.frame_number << 16), NilAtom.nil)));
				}
			}
		}
		
		public override void save_state (AtomWriter atom_writer)
		{
			save_state(atom_writer, 256);
		}
	}
}

