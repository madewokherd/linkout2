using System;
using Linkout.Lisp;
namespace Linkout
{
	public interface IUndoAction
	{
		Atom[] GetUndoCommand();
		
		Atom[] GetRedoCommand();
		
		IUndoAction Combine(IUndoAction next);
	}
}

