using System;
using System.Collections.Generic;

namespace Linkout.Lisp
{
	public class Context
	{
		public Dictionary<Atom, Atom> dict;
		
		public Context ()
		{
			dict = new Dictionary<Atom, Atom>();
		}

		public virtual void CopyToContext(Context result)
		{
			result.dict = new Dictionary<Atom, Atom>(this.dict);
		}
		
		public virtual Context Copy()
		{
			Context result = new Context();
			
			CopyToContext(result);
			
			return result;
		}
		
		public Atom get_local(Atom name)
		{
			Atom result = NilAtom.nil;
			
			dict.TryGetValue(name, out result);
			
			return result;
		}
		
		
	}
}

