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
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public class ScriptHost : Interpreter
	{
		public ScriptHost () : base()
		{
			functions[atom_advance] = func_advance;
			functions[new StringAtom("discard-frames").intern()] = func_discard_frames;
			functions[new StringAtom("frame").intern()] = func_frame;
			functions[atom_hint] = func_hint;
			functions[new StringAtom("in-frame").intern()] = func_in_frame;
			functions[new StringAtom("reset").intern()] = func_reset;
			functions[atom_seek_to] = func_seek_to;
			undo_history = new LinkedList<UndoSnapshot>();
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

		
		public LinkedList<UndoSnapshot> undo_history;
		public LinkedListNode<UndoSnapshot> undo_position;
		private bool in_undo_redo;
		
		public void AddUndoAction(IUndoAction action)
		{
			if (in_undo_redo)
				return;
			
			if (undo_position != null)
			{
				if (!undo_position.Value.is_open)
				{
					UndoSnapshot snapshot = new UndoSnapshot("Actions in progress");
					snapshot.undo_command = new UndoCommand();
					snapshot.is_open = true;
					
					undo_position = undo_history.AddAfter(undo_position, snapshot);
					while (undo_position.Next != null)
					{
						undo_history.Remove(undo_position.Next);
					}
				}
				
				undo_position.Value.undo_command.AppendAction(action);
			}
		}

		public void AddUndoSnapshot(string name)
		{
			if (in_undo_redo)
				return;
			
			if (undo_position == null)
			{
				/* First snapshot */
				if (name == null)
					name = "Automatic snapshot";
				UndoSnapshot snapshot = new UndoSnapshot(name);
				undo_position = undo_history.AddLast(snapshot);
				snapshot.is_open = false;
			}
			else if (undo_position.Value.is_open)
			{
				if (name != null)
					undo_position.Value.name = name;
				undo_position.Value.is_open = false;
			}
			/* else we haven't done anything since the last snapshot. */
		}
		
		public void NameCurrentSnapshot(string name)
		{
			if (undo_position != null && undo_position.Value.is_open)
				undo_position.Value.name = name;
		}
		
		public void UndoTo(UndoSnapshot snapshot)
		{
			if (in_undo_redo)
				throw new InvalidOperationException("Already in an undo/redo operation");
			
			if (undo_position == null)
				throw new InvalidOperationException("No snapshots exist yet");

			AddUndoSnapshot(null);
			
			UndoCommand command = new UndoCommand();
			command.PrependCommand(undo_position.Value.undo_command);
			LinkedListNode<UndoSnapshot> cursor = undo_position.Previous;
			
			while (cursor != null && cursor.Value != snapshot)
			{
				command.PrependCommand(cursor.Value.undo_command);
				cursor = cursor.Previous;
			}
			
			in_undo_redo = true;
			
			try
			{
				Context context = new Context();
				
				foreach (KeyValuePair<Type, IUndoAction> kvp in command.actions)
				{
					Atom[] statements = kvp.Value.GetUndoCommand();
					int i=0;
					
					for (i=0; i<statements.Length; i++)
					{
						eval(statements[i], context);
					}
				}
			}
			finally
			{
				in_undo_redo = false;
			}
		}
		
		public void RedoTo(UndoSnapshot snapshot)
		{
			if (in_undo_redo)
				throw new InvalidOperationException("Already in an undo/redo operation");
			
			if (undo_position == null)
				throw new InvalidOperationException("No snapshots exist yet");
			
			UndoCommand command = new UndoCommand();
			LinkedListNode<UndoSnapshot> cursor = undo_position.Next;
			command.AppendCommand(cursor.Value.undo_command);
			
			while (cursor != null && cursor.Value != snapshot)
			{
				cursor = cursor.Next;
				command.AppendCommand(cursor.Value.undo_command);
			}
			
			in_undo_redo = true;
			
			try
			{
				Context context = new Context();
				
				foreach (KeyValuePair<Type, IUndoAction> kvp in command.actions)
				{
					Atom[] statements = kvp.Value.GetRedoCommand();
					int i=0;
					
					for (i=0; i<statements.Length; i++)
					{
						eval(statements[i], context);
					}
				}
			}
			finally
			{
				in_undo_redo = false;
			}
		}
		
		public void Undo()
		{
			if (in_undo_redo)
				throw new InvalidOperationException("Already in an undo/redo operation");
			
			if (undo_position == null)
				throw new InvalidOperationException("No snapshots exist yet");
			
			UndoTo(undo_position.Previous.Value);
		}
		
		public void Redo()
		{
			if (in_undo_redo)
				throw new InvalidOperationException("Already in an undo/redo operation");
			
			if (undo_position == null)
				throw new InvalidOperationException("No snapshots exist yet");
			
			RedoTo(undo_position.Next.Value);
		}
		
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
			bool needs_snapshot = last_frame != frame;
			Frame old_last_frame = last_frame;
			uint old_seek = frame.frame_number;
			
			if (!frame.committed)
				commit_next_frame();

			if (needs_snapshot)
				AddUndoSnapshot(null);
			
			last_frame = frame = frame.advance(external_events);

			AddUndoAction(new EditFrameAction(old_last_frame, old_seek, last_frame, frame.frame_number));
			
			NameCurrentSnapshot("Gameplay");
			
			commit_next_frame();
		}
		
		public void advance_frame()
		{
			advance_frame(null);
		}
		
		public Atom func_discard_frames(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "discard-frames");

			if (arglist == null || !(arglist[0] is FixedPointAtom) || last_frame == null)
				return NilAtom.nil;
			
			uint framenum = (uint)(((FixedPointAtom)arglist[0]).int_value >> 16);
			
			if (framenum >= last_frame.frame_number)
				return NilAtom.nil;
			
			Frame new_last_frame = last_frame.get_previous_frame(framenum);
			
			Frame old_last_frame = last_frame;
			uint old_seek = frame.frame_number;
			
			last_frame = new_last_frame;
			
			if (frame.frame_number > framenum)
				frame = last_frame;
			
			AddUndoSnapshot(null);
			AddUndoAction(new EditFrameAction(old_last_frame, old_seek, last_frame, frame.frame_number));
			NameCurrentSnapshot("Discard frames");
			
			if (OnFrameChange != null)
				OnFrameChange();
			
			return NilAtom.nil;
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

				AddUndoAction(new EditFrameAction(null, 0, last_frame, 0));
				
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
		
		public Atom func_reset(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			AddUndoSnapshot(null);
			
			AddUndoAction(new EditFrameAction(last_frame, frame.frame_number, null, 0));
			
			frame = last_frame = null;
			
			if (OnFrameChange != null)
				OnFrameChange();

			return NilAtom.nil;
		}
		
		public bool seek_to(uint new_framenum)
		{
			if (frame.frame_number == new_framenum)
				return true;
			
			uint old_seek = frame.frame_number;
			
			if (new_framenum < last_frame.frame_number)
			{
				frame = last_frame.get_previous_frame(new_framenum);
				
				AddUndoAction(new EditFrameAction(last_frame, old_seek, last_frame, new_framenum));
				
				if (OnFrameChange != null)
					OnFrameChange();
				
				return true;
			}
			else if (new_framenum == last_frame.frame_number)
			{
				frame = last_frame;

				AddUndoAction(new EditFrameAction(last_frame, old_seek, last_frame, new_framenum));
				
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
			Atom result;
			
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
			
			/* FIXME: This function shouldn't be able to change state because that screws up undo. */

			result = frame.eval(arglist[1], context);
			
			return result;
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
		
		public override void set_custom_function (Atom name, CustomLispFunction func)
		{
			CustomLispFunction prev_func = null;
			
			custom_functions.TryGetValue(name, out prev_func);
			
			base.set_custom_function (name, func);
			
			AddUndoAction(new EditFunctionsAction(name, prev_func, func));
		}

		public override void set_global (Atom name, Atom val, Context context)
		{
			Atom prev_val = null;
			
			globals.TryGetValue(name, out prev_val);
			
			base.set_global (name, val, context);
			
			AddUndoAction(new EditGlobalsAction(name, prev_val, val));
		}
	}
}

