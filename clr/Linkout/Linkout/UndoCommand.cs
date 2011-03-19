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
	public class UndoCommand
	{
		public UndoCommand ()
		{
			actions = new Dictionary<Type, IUndoAction>();
		}
		
		public Dictionary<Type, IUndoAction> actions;
		
		public void AppendAction(IUndoAction action)
		{
			Type type = action.GetType();
			
			if (actions.ContainsKey(type))
			{
				actions[type] = actions[type].Combine(action);
			}
			else
			{
				actions[type] = action;
			}
		}
		
		public void PrependAction(IUndoAction action)
		{
			Type type = action.GetType();
			
			if (actions.ContainsKey(type))
			{
				actions[type] = action.Combine(actions[type]);
			}
			else
			{
				actions[type] = action;
			}
		}
		
		public void AppendCommand(UndoCommand command)
		{
			foreach (KeyValuePair<Type, IUndoAction> kvp in command.actions)
			{
				AppendAction(kvp.Value);
			}
		}
		
		public void PrependCommand(UndoCommand command)
		{
			foreach (KeyValuePair<Type, IUndoAction> kvp in command.actions)
			{
				PrependAction(kvp.Value);
			}
		}
	}
}

