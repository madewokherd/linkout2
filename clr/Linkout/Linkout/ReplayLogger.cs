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

