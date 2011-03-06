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

