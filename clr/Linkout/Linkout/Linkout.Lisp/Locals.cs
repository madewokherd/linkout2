using System;
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public sealed class Locals
	{
		public Dictionary<StringAtom, Atom> dict;
		
		public readonly Locals parent;
		
		public Locals ()
		{
			dict = new Dictionary<StringAtom, Atom>();
			parent = null;
		}

		public Locals (Locals parent)
		{
			dict = new Dictionary<StringAtom, Atom>();
			this.parent = parent;
		}
	}
}

