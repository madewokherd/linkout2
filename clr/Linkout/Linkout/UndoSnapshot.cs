using System;
using System.Collections.Generic;
namespace Linkout
{
	public class UndoSnapshot
	{
		public UndoSnapshot (string name)
		{
			user_data = new Dictionary<string, object>();
			this.name = name;
		}
		
		public Dictionary<string, object> user_data;
		
		public string name;
		
		public bool is_open;
		
		public UndoCommand undo_command;
	}
}

