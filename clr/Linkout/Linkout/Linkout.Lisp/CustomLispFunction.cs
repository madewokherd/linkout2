/*
 * Copyright 2010, 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
namespace Linkout.Lisp
{
	public class CustomLispFunction
	{
		public CustomLispFunction(Atom name, Atom args, Atom body, bool eval_args_first)
		{
			this.name = name;
			this.args = args;
			this.body = body;
			this.eval_args_first = eval_args_first;
		}
		
		public static CustomLispFunction from_args(Atom args, bool eval_args_first)
		{
			Atom name;
			Atom func_args;
			Atom body;
			
			try
			{
				name = args.get_car().get_car();
				func_args = args.get_car().get_cdr();
				body = args.get_cdr().get_car();
			}
			catch (NotSupportedException)
			{
				return null;
			}
			
			return new CustomLispFunction(name, func_args, body, eval_args_first);
		}
		
		public readonly Atom name;
		public readonly Atom args;
		public readonly Atom body;
		
		public readonly bool eval_args_first;
	}
}

