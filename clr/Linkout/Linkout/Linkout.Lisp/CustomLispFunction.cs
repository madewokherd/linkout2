/*
 * Copyright 2010, 2011 Vincent Povirk
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
		
		private readonly Atom atom_define = new StringAtom("define").intern();
		private readonly Atom atom_defineex = new StringAtom("defineex").intern();

		public Atom GetDefinition()
		{
			Atom[] args = new Atom[3];
			
			if (eval_args_first)
				args[0] = atom_define;
			else
				args[0] = atom_defineex;
			
			args[1] = new ConsAtom(name, this.args);
			args[2] = body;
			
			return Atom.from_array(args);
		}
	}
}

