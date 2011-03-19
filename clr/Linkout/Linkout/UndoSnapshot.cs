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

