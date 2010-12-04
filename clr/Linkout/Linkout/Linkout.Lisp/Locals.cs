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
			dict = new Dictionary<Atom, Atom>();
			this.parent = parent;
		}
	}
}

