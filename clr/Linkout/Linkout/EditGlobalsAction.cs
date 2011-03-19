/*
 * Copyright 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Linkout is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *  
 * You should have received a copy of the GNU General Public License
 * along with Linkout.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public class EditGlobalsAction : IUndoAction
	{
		public EditGlobalsAction (Atom key, Atom old_value, Atom new_value)
		{
			this.old_values = new Dictionary<Atom, Atom>();
			this.new_values = new Dictionary<Atom, Atom>();
			
			this.old_values[key] = old_value;
			this.new_values[key] = new_value;
		}

		private EditGlobalsAction (Dictionary<Atom, Atom> old_values, Dictionary<Atom, Atom> new_values)
		{
			this.old_values = old_values;
			this.new_values = new_values;
		}
		
		Dictionary<Atom, Atom> old_values;
		Dictionary<Atom, Atom> new_values;
		
		private readonly Atom atom_setglobal = new StringAtom("setglobal").intern();
		private readonly Atom atom_delglobal = new StringAtom("delglobal").intern();
		
		Atom[] IUndoAction.GetUndoCommand ()
		{
			Atom[] result = new Atom[old_values.Count];
			int i=0;
			
			foreach (KeyValuePair<Atom, Atom> kvp in old_values)
			{
				if (kvp.Value is NilAtom)
					result[i] = new ConsAtom(atom_delglobal, new ConsAtom(kvp.Key, NilAtom.nil));
				else
					result[i] = new ConsAtom(atom_setglobal, new ConsAtom(kvp.Key, new ConsAtom(kvp.Value, NilAtom.nil)));
				i++;
			}
			
			return result;
		}
		
		Atom[] IUndoAction.GetRedoCommand ()
		{
			Atom[] result = new Atom[new_values.Count];
			int i=0;
			
			foreach (KeyValuePair<Atom, Atom> kvp in new_values)
			{
				if (kvp.Value is NilAtom)
					result[i] = new ConsAtom(atom_delglobal, new ConsAtom(kvp.Key, NilAtom.nil));
				else
					result[i] = new ConsAtom(atom_setglobal, new ConsAtom(kvp.Key, new ConsAtom(kvp.Value, NilAtom.nil)));
				i++;
			}
			
			return result;
		}
		
		IUndoAction IUndoAction.Combine (IUndoAction next)
		{
			EditGlobalsAction next_globals = (EditGlobalsAction)next;
			Dictionary<Atom, Atom> old_values = new Dictionary<Atom, Atom>();
			Dictionary<Atom, Atom> new_values = new Dictionary<Atom, Atom>();
			
			foreach (KeyValuePair<Atom, Atom> kvp in this.old_values)
			{
				Atom new_value;
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
			
			foreach (KeyValuePair<Atom, Atom> kvp in next_globals.old_values)
			{
				if (old_values.ContainsKey(kvp.Key))
					continue;
				
				old_values[kvp.Key] = kvp.Value;
				new_values[kvp.Key] = next_globals.new_values[kvp.Key];
			}
			
			return new EditGlobalsAction(old_values, new_values);
		}
	}
}

