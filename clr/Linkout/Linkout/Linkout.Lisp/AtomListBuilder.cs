using System;
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public class AtomListBuilder : AtomWriter
	{
		public AtomListBuilder ()
		{
			atoms = new List<Atom>();
		}
		
		public List<Atom> atoms;
		
		public override void Write (Atom data)
		{
			atoms.Add(data);
		}
		
		public override void Close ()
		{
		}
		
		public Atom ToAtom ()
		{
			Atom result = NilAtom.nil;
			int i = atoms.Count;
			
			while (i > 0)
			{
				i--;
				result = new ConsAtom(atoms[i], result);
			}
			
			return result;
		}
	}
}

