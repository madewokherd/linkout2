using System;
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public sealed class Locals
	{
		public Dictionary<Atom, Atom> dict;
		
		public Locals ()
		{
			dict = new Dictionary<Atom, Atom>();
		}

		public Locals (Locals parent)
		{
			dict = new Dictionary<Atom, Atom>(parent.dict);
		}

		public Locals (Dictionary<Atom, Atom> dict)
		{
			this.dict = dict;
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
			
			return result;
		}
		
		public Atom get_value(Atom name)
		{
			Atom result = NilAtom.nil;
			
			dict.TryGetValue(name, out result);
			
			return result;
		}
	}
}

