using System;
namespace Linkout.Lisp
{
	public abstract class AtomWriter
	{
		public abstract void Write(Atom data);
		
		public abstract void Close();
	}
}

