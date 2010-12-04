using System;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public sealed class Box : GameObject
	{
		public Box ()
		{
		}
		
		private Box (Box original) : base(original)
		{
		}
		
		public override Atom to_atom ()
		{
			return new ConsAtom(new StringAtom("box"), attributes_to_atom());
		}
		
		public override GameObject copy ()
		{
			return new Box(this);
		}
	}
}

