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
			priv_id = 0;
		}
		
		internal GameObject (GameObject original)
		{
			priv_committed = false;
			attributes = new Dictionary<Atom, Atom>(original.attributes);
			priv_id = original.priv_id;
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
				
				priv_id = value;
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
		
		public abstract GameObject copy();
		
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
		
		protected int get_base_hash()
		{
			int result=0;
			
			foreach (KeyValuePair<Atom, Atom> kvp in attributes)
			{
				result = result ^ (kvp.Key.GetHashCode() * (kvp.Value.GetHashCode()+1));
			}
			
			result = result ^ (new FixedPointAtom(priv_id)).GetHashCode();
			
			return result;
		}
		
		public virtual bool Equals (GameObject obj, out string message)
		{
			if (this.GetType() != obj.GetType())
			{
				message = String.Format("Object type {0} != {1}", this.GetType(), obj.GetType());
				return false;
			}
			
			if (this.id != obj.id)
			{
				message = String.Format("Object id {0} != {1}", this.id, obj.id);
				return false;
			}
			
			if (this.attributes.Count != obj.attributes.Count)
			{
				message = String.Format("Object id {0} attribute count {1} != {2}", this.id,
				                        this.attributes.Count, obj.attributes.Count);
				return false;
			}
			
			foreach (KeyValuePair<Atom, Atom> kvp in attributes)
			{
				Atom obj_value;
				if (!obj.attributes.TryGetValue(kvp.Key, out obj_value))
				{
					message = String.Format("Object id {0} has unexpected attribute {1}",
					                        this.id, kvp.Key);
					return false;
				}
				if (!kvp.Value.Equals(obj_value))
				{
					message = String.Format("Object id {0} attribute {1} value mismatch {2} != {3}",
					                        this.id, kvp.Key, kvp.Value, obj_value);
					return false;
				}
			}

			message = "";
			return true;
		}
		
		public override bool Equals (object obj)
		{
			string dummy;
			
			if (this.GetType() != obj.GetType())
				return false;
			
			return Equals((GameObject)obj, out dummy);
		}
	}
}

