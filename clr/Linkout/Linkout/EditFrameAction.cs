/*
 * Copyright 2011 Vincent Povirk
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
	public class EditFrameAction : IUndoAction
	{
		public EditFrameAction (Frame old_frame, uint old_seek, Frame new_frame, uint new_seek)
		{
			this.old_last_frame = old_frame;
			this.old_seek = old_seek;
			this.new_last_frame = new_frame;
			this.new_seek = new_seek;
		}

		public Frame old_last_frame;
		public uint old_seek;

		public Frame new_last_frame;
		public uint new_seek;

		private static readonly Atom atom_advance = new StringAtom("advance").intern();
		private static readonly Atom atom_discard_frames = new StringAtom("discard-frames").intern();
		private static readonly Atom atom_reset = new StringAtom("reset").intern();
		private static readonly Atom atom_seek_to = new StringAtom("seek-to").intern();
		
		static Lisp.Atom[] GetCommand (Frame old_frame, uint old_seek, Frame new_frame, uint new_seek)
		{
			LinkedList<Atom> result = new LinkedList<Atom>();
			uint seek=old_seek;
			
			/* First set the last frames to be the same. */
			if (old_frame == new_frame)
			{
				/* Nothing to do. */
			}
			else if (new_frame == null)
			{
				/* FIXME: Implement (reset) so this can actually work. */
				result.AddLast(new ConsAtom(atom_reset, NilAtom.nil));
			}
			else
			{
				Frame ancestor = old_frame.get_common_ancestor(new_frame);
				bool ancestor_is_last_frame;
				
				/* Make sure we're seeked to an ancestor of the new frame. */
				if (ancestor == null)
				{
					if (new_frame.frame_number == 0)
						ancestor = new_frame;
					else
						ancestor = new_frame.get_previous_frame(0);
					result.AddLast(new ConsAtom(atom_reset, NilAtom.nil));
					result.AddLast(ancestor.to_atom());
					seek = 0;
					ancestor_is_last_frame = true;
				}
				else
				{
					result.AddLast(new ConsAtom(atom_seek_to, new ConsAtom(new FixedPointAtom(ancestor.frame_number << 16), NilAtom.nil)));
					seek = ancestor.frame_number;
					ancestor_is_last_frame = (old_frame == ancestor);
				}
				
				/* Bring the rest of the frame history in line, if necessary. */
				if (ancestor == new_frame)
				{
					if (!ancestor_is_last_frame)
					{
						result.AddLast(new ConsAtom(atom_discard_frames, new ConsAtom(new FixedPointAtom(new_frame.frame_number << 16), NilAtom.nil)));
					}
				}
				else
				{
					Frame last_frame = new_frame;
					LinkedListNode<Atom> last_frame_node = null;
					
					/* Add advance commands to the list in reverse order, because traversing the frames
					 * in reverse is faster than doing it forwards. */
					while (last_frame != ancestor)
					{
						Atom advance_command;
						
						advance_command = new ConsAtom(atom_advance, Atom.from_array(last_frame.external_events));
						
						if (last_frame_node == null)
							last_frame_node = result.AddLast(advance_command);
						else
							last_frame_node = result.AddBefore(last_frame_node, advance_command);
						last_frame = last_frame.get_previous_frame(last_frame.frame_number - 1);
					}
					
					seek = new_frame.frame_number;
				}
			}
		
			if (new_frame != null && seek != new_seek)
			{
				result.AddLast(new ConsAtom(atom_seek_to, new ConsAtom(new FixedPointAtom(seek << 16), NilAtom.nil)));
			}
			
			Atom[] array = new Atom[result.Count];
			
			result.CopyTo(array, 0);
			
			return array;
		}
		
		Lisp.Atom[] IUndoAction.GetUndoCommand ()
		{
			return GetCommand(new_last_frame, new_seek, old_last_frame, old_seek);
		}
		
		Lisp.Atom[] IUndoAction.GetRedoCommand ()
		{
			return GetCommand(old_last_frame, old_seek, new_last_frame, new_seek);
		}
		
		IUndoAction IUndoAction.Combine (IUndoAction next)
		{
			EditFrameAction nextaction = (EditFrameAction)next;
			return new EditFrameAction(old_last_frame, old_seek, nextaction.new_last_frame, nextaction.new_seek);
		}
	}
}

