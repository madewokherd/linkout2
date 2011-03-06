using System;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public class EditFunctionsAction : IUndoAction
	{
		public EditFunctionsAction (Atom key, CustomLispFunction old_value, CustomLispFunction new_value)
		{
			this.old_values = new Dictionary<Atom, CustomLispFunction>();
			this.new_values = new Dictionary<Atom, CustomLispFunction>();
			
			this.old_values[key] = old_value;
			this.new_values[key] = new_value;
		}

		private EditFunctionsAction (Dictionary<Atom, CustomLispFunction> old_values, Dictionary<Atom, CustomLispFunction> new_values)
		{
			this.old_values = old_values;
			this.new_values = new_values;
		}
		
		Dictionary<Atom, CustomLispFunction> old_values;
		Dictionary<Atom, CustomLispFunction> new_values;
		
		private readonly Atom atom_undefine = new StringAtom("undefine").intern();
		
		Atom[] IUndoAction.GetUndoCommand ()
		{
			Atom[] result = new Atom[old_values.Count];
			int i=0;
			
			foreach (KeyValuePair<Atom, CustomLispFunction> kvp in old_values)
			{
				if (kvp.Value == null)
					result[i] = new ConsAtom(atom_undefine, new ConsAtom(kvp.Key, NilAtom.nil));
				else
					result[i] = kvp.Value.GetDefinition();
				i++;
			}
			
			return result;
		}
		
		Atom[] IUndoAction.GetRedoCommand ()
		{
			Atom[] result = new Atom[new_values.Count];
			int i=0;
			
			foreach (KeyValuePair<Atom, CustomLispFunction> kvp in new_values)
			{
				if (kvp.Value == null)
					result[i] = new ConsAtom(atom_undefine, new ConsAtom(kvp.Key, NilAtom.nil));
				else
					result[i] = kvp.Value.GetDefinition();
				i++;
			}
			
			return result;
		}
		
		IUndoAction IUndoAction.Combine (IUndoAction next)
		{
			EditFunctionsAction next_globals = (EditFunctionsAction)next;
			Dictionary<Atom, CustomLispFunction> old_values = new Dictionary<Atom, CustomLispFunction>();
			Dictionary<Atom, CustomLispFunction> new_values = new Dictionary<Atom, CustomLispFunction>();
			
			foreach (KeyValuePair<Atom, CustomLispFunction> kvp in this.old_values)
			{
				CustomLispFunction new_value;
				old_values[kvp.Key] = kvp.Value;
				if (next_globals.new_values.TryGetValue(kvp.Key, out new_value))
				{
					new_values[kvp.Key] = new_value;
				}
				else
				{
					new_values[kvp.Key] = this.new_values[kvp.Key];
				}
			}
			
			foreach (KeyValuePair<Atom, CustomLispFunction> kvp in next_globals.old_values)
			{
				if (old_values.ContainsKey(kvp.Key))
					continue;
				
				old_values[kvp.Key] = kvp.Value;
				new_values[kvp.Key] = next_globals.new_values[kvp.Key];
			}
			
			return new EditFunctionsAction(old_values, new_values);
		}
	}
}
