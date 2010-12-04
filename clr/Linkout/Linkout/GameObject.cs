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
		
		private long priv_id;
		
		protected Dictionary<Atom, Atom> attributes;

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
		
		public long id
		{
			set
			{
				if (priv_committed)
					throw new InvalidOperationException("This object can no longer be modified.");
				
				priv_id = id;
			}
			get
			{
				return priv_id;
			}
		}
		
		public virtual void commit()
		{
			priv_committed = true;
		}
		
		public Atom attributes_to_atom()
		{
			Atom attributelist = NilAtom.nil;
			
			foreach (KeyValuePair<Atom, Atom> kvp in attributes)
			{
				// attributelist = ((key value) . attributelist)
				attributelist = new ConsAtom(new ConsAtom(kvp.Key, new ConsAtom(kvp.Value, NilAtom.nil)), attributelist);
			}
			
			return attributelist;
		}
		
		public abstract Atom to_atom();
	}
}

