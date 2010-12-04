using System;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public abstract class GameObject
	{
		internal GameObject ()
		{
			priv_committed = false;
			attributes = new Dictionary<Atom, Atom>();
		}
		
		private Dictionary<Atom, Atom> attributes;

		private bool priv_committed;
		
		public bool committed
		{
			get
			{
				return priv_committed;
			}
		}
		
		public void setattr(Atom key, Atom val)
		{
			if (priv_committed)
				throw new InvalidOperationException("This object can no longer be modified.");

			attributes[key] = val;
		}
		
		public Atom getattr(Atom key)
		{
			try
			{
				return attributes[key];
			}
			catch (KeyNotFoundException)
			{
				return NilAtom.nil;
			}
		}
		
		public virtual void commit()
		{
			priv_committed = true;
		}
		
		public abstract Atom to_atom();
	}
}

