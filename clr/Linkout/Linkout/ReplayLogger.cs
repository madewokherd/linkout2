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
using Linkout.Lisp;

namespace Linkout
{
	public class ReplayLogger
	{
		public ReplayLogger (ScriptHost host, AtomWriter writer)
		{
			this.host = host;
			this.writer = writer;
			host.OnHint += HintHandler;
			host.OnNewFrame += NewFrameHandler;
			writer.Write(new ConsAtom(atom_hint, new ConsAtom(atom_replayfile, NilAtom.nil)));
		}
		
		ScriptHost host;
		AtomWriter writer;
		
		uint next_framenum = 0;
		
		private readonly StringAtom atom_advance = new StringAtom("advance").intern();
		private readonly StringAtom atom_checksum = new StringAtom("checksum").intern();
		private readonly StringAtom atom_hint = new StringAtom("hint").intern();
		private readonly StringAtom atom_replayfile = new StringAtom("replay-file").intern();
		private readonly StringAtom atom_reset = new StringAtom("reset").intern();
		private readonly StringAtom atom_seekto = new StringAtom("seek-to").intern();
		
		void HintHandler(Atom args)
		{
			writer.Write(new ConsAtom(atom_hint, args));
		}
		
		uint frames_since_last_checksum;
		
		readonly uint checksum_frequency = 256;
		
		void NewFrameHandler()
		{
			if (!host.frame.committed)
				return;
			if (host.frame.frame_number == 0)
			{
				if (next_framenum != 0)
				{
					writer.Write(new ConsAtom(atom_reset, NilAtom.nil));
				}
				
				writer.Write(host.frame.to_atom());
				
				next_framenum = 1;
				frames_since_last_checksum = 1;
			}
			else
			{
				if (next_framenum != host.frame.frame_number)
				{
					writer.Write(new ConsAtom(atom_seekto, new ConsAtom(new FixedPointAtom((long)(host.frame.frame_number-1) << 16), NilAtom.nil)));
				}
				
				writer.Write(new ConsAtom(atom_advance, Atom.from_array(host.frame.external_events)));
				
				next_framenum++;
				frames_since_last_checksum++;
			}

			if (frames_since_last_checksum >= checksum_frequency)
			{
				Atom[] list = new Atom[3];
				list[0] = atom_hint;
				list[1] = atom_checksum;
				list[2] = new FixedPointAtom((long)host.frame.frame_hash() << 16);
				
				writer.Write(Atom.from_array(list));
				frames_since_last_checksum = 0;
			}
		}
		
		public void Close()
		{
			host.OnHint -= HintHandler;
			host.OnNewFrame -= NewFrameHandler;
			writer.Close();
		}
	}
}

