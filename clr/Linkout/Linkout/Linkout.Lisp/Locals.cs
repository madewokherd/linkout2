using System;
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public sealed class Locals
	{
		public Dictionary<Atom, Atom> dict;
		
		public readonly Locals parent;
		
		public Locals ()
		{
			dict = new Dictionary<Atom, Atom>();
			parent = null;
		}

		public Locals (Locals parent)
		{
			// FIXME: We should probably copy rather than link to the parent.
			dict = new Dictionary<Atom, Atom>();
			this.parent = parent;
		}
		
		private static void pattern_match(Dictionary<Atom, Atom> names, Atom pattern, Atom atom)
		{
			if (pattern.atomtype == AtomType.String)
			{
				names[pattern] = atom;
			}
			else if (pattern.atomtype == AtomType.Cons && atom.atomtype == AtomType.Cons)
			{
				pattern_match(names, pattern.get_car(), atom.get_car());
				pattern_match(names, pattern.get_cdr(), atom.get_cdr());
			}
		}
		
		public static Locals from_pattern(Locals parent, Atom pattern, Atom atom)
		{
			Locals result = new Locals(parent);
			
			pattern_match(result.dict, pattern, atom);
			
			if (result.dict.Count != 0)
				return result;
			else
				return parent;
		}
	}
}

