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
			attributes = new Dictionary<int, Dictionary<byte[], Atom>>();
		}
		
		private Dictionary<int, Dictionary<byte[], Atom>> attributes;

		private bool priv_committed;
		
		public bool committed
		{
			get
			{
				return priv_committed;
			}
		}
		
		public void setattr(int index, byte[] key, Atom val)
		{
			Dictionary<byte[], Linkout.Lisp.Atom> dict;
			
			if (priv_committed)
				throw new InvalidOperationException("This object can no longer be modified.");

			if (!attributes.TryGetValue(index, out dict))
			{
				dict = new Dictionary<byte[], Atom>();
				attributes[index] = dict;
			}
			
			dict[key] = val;
		}
		
		public Atom getattr(int index, byte[] key)
		{
			try
			{
				return attributes[index][key];
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

