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
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public class Interpreter
	{
		public Interpreter ()
		{
			functions = new Dictionary<Atom, LispFunction>();
			functions[new StringAtom("*").intern()] = func_mult;
			functions[new StringAtom("+").intern()] = func_plus;
			functions[new StringAtom("-").intern()] = func_sub;
			functions[new StringAtom("/").intern()] = func_div;
			functions[new StringAtom("=").intern()] = func_eq;
			functions[new StringAtom("and").intern()] = func_and;
			functions[new StringAtom("apply").intern()] = func_apply;
			functions[new StringAtom("begin").intern()] = func_begin;
			functions[new StringAtom("bitwise-and").intern()] = func_bitwise_and;
			functions[new StringAtom("bitwise-not").intern()] = func_bitwise_not;
			functions[new StringAtom("bitwise-or").intern()] = func_bitwise_or;
			functions[new StringAtom("bitwise-xor").intern()] = func_bitwise_xor;
			functions[atom_define] = func_define;
			functions[atom_defineex] = func_defineex;
			functions[new StringAtom("delglobal").intern()] = func_delglobal;
			functions[new StringAtom("display").intern()] = func_display;
			functions[new StringAtom("eval").intern()] = func_eval;
			functions[new StringAtom("get").intern()] = func_get;
			functions[new StringAtom("getglobal").intern()] = func_getglobal;
			functions[new StringAtom("getlocal").intern()] = func_get;
			functions[new StringAtom("if").intern()] = func_if;
			functions[new StringAtom("let").intern()] = func_let;
			functions[new StringAtom("let*").intern()] = func_let_splat;
			functions[new StringAtom("list").intern()] = func_list;
			functions[new StringAtom("not").intern()] = func_not;
			functions[new StringAtom("or").intern()] = func_or;
			functions[new StringAtom("quot").intern()] = func_quot;
			functions[atom_setglobal] = func_setglobal;
			functions[new StringAtom("trunc").intern()] = func_trunc;

			custom_functions = new Dictionary<Atom, CustomLispFunction>();
			globals = new Dictionary<Atom, Atom>();
			
			immutable = false;
			isolated = false;
		}

		private readonly Atom atom_define = new StringAtom("define").intern();
		private readonly Atom atom_defineex = new StringAtom("defineex").intern();
		private readonly Atom atom_setglobal = new StringAtom("setglobal").intern();
		
		public delegate Atom LispFunction(Atom args, Context context);
		
		protected Dictionary<Atom, LispFunction> functions;

		protected Dictionary<Atom, CustomLispFunction> custom_functions;

		protected Dictionary<Atom, Atom> globals;
		
		/* If immutable is true, functions should not make changes to this object that will be visible to interpreted
		 * code. */
		public bool immutable;
		
		/* If isolated is true, functions should not make be able to explicitly read or write anything outside the
		 * interpreted environment, so all programs should be fully determinstic. For example, a function to get the
		 * current time would not function or would return a constant dummy value. */
		public bool isolated;
		
		private static readonly bool trace_call = System.Environment.GetEnvironmentVariable("LINKOUT_TRACE") != null;
		
		public Atom[] get_n_args(Atom args, uint n, string function_name)
		{
			Atom[] result = new Atom[n];
			Atom remaining_args = args;
			uint i;
			
			try
			{
				for (i=0; i<n; i++)
				{
					result[i] = remaining_args.get_car();
					remaining_args = remaining_args.get_cdr();
				}
			}
			catch (NotSupportedException)
			{
				Console.WriteLine("Expected {0} arguments to {1}, got {2}", n, function_name, args);
				return null;
			}
			
			if (remaining_args.atomtype != AtomType.Nil)
				Console.WriteLine("Expected {0} arguments to {1}, got extra arguments: {2}", n, function_name, args);
			
			return result;
		}

		public Atom[] eval_n_args(Atom args, uint n, uint m, string function_name, Context context)
		{
			Atom[] result = get_n_args(args, m, function_name);
			uint i;
			
			if (result != null)
			{
				for (i=0; i<n; i++)
				{
					result[i] = eval(result[i], context);
				}
			}
			
			return result;
		}
		
		public Atom func_mult(Atom args, Context context)
		{
			long result = 0x10000;
			args = eval_args(args, context);
			try
			{
				while (true)
				{
					result = (result * args.get_car().get_fixedpoint()) >> 16;
					args = args.get_cdr();
				}
			}
			catch (NotSupportedException)
			{
			}
			return new FixedPointAtom(result);
		}
		
		public Atom func_plus(Atom args, Context context)
		{
			long result = 0;
			args = eval_args(args, context);
			try
			{
				while (true)
				{
					result = result + args.get_car().get_fixedpoint();
					args = args.get_cdr();
				}
			}
			catch (NotSupportedException)
			{
			}
			return new FixedPointAtom(result);
		}
		
		public Atom func_sub(Atom args, Context context)
		{
			long result=0;
			int len=0;
			
			args = eval_args(args, context);
			
			while (args.atomtype == AtomType.Cons)
			{
				Atom car = args.get_car();
				
				if (car.atomtype != AtomType.FixedPoint)
					break;
				
				if (len == 0)
					result = car.get_fixedpoint();
				else
					result = result - car.get_fixedpoint();
				
				len++;
				args = args.get_cdr();
			}
			
			if (len == 0)
				return NilAtom.nil;
			else if (len == 1)
				return new FixedPointAtom(-result);
			else
				return new FixedPointAtom(result);
		}
		
		public Atom func_div(Atom args, Context context)
		{
			long a, b;
			
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 2, "/");
			
			if (arglist == null)
				return NilAtom.nil;
			
			try
			{
				a = arglist[0].get_fixedpoint();
				b = arglist[1].get_fixedpoint();
			}
			catch (NotSupportedException)
			{
				return NilAtom.nil;
			}
			
			if (b == 0)
				return NilAtom.nil;
			
			return new FixedPointAtom(FixedPointAtom.divide(a, b));
		}
		
		public Atom func_eq(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 2, "=");
			
			if (arglist == null)
				return NilAtom.nil;
			
			if (arglist[0].Equals(arglist[1]))
				return FixedPointAtom.One;
			else
				return FixedPointAtom.Zero;
		}
		
		public Atom func_and(Atom args, Context context)
		{
			Atom result = NilAtom.nil;
			
			while (args.atomtype == AtomType.Cons)
			{
				result = eval(args.get_car(), context);
				
				if (!result.is_true())
					break;
				
				args = args.get_cdr();
			}
			
			return result;
		}
		
		public Atom func_apply(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 2, "apply");
			
			if (arglist == null)
				return NilAtom.nil;
			else
				return eval(new ConsAtom(arglist[0], arglist[1]), context);
		}
		
		public Atom func_begin(Atom args, Context context)
		{
			Atom result = NilAtom.nil;
			
			while (args.atomtype == AtomType.Cons)
			{
				result = eval(args.get_car(), context);
				
				args = args.get_cdr();
			}
			
			return result;
		}
		
		public Atom func_bitwise_and(Atom args, Context context)
		{
			long result = -1;
			args = eval_args(args, context);
			try
			{
				while (true)
				{
					result = (result & args.get_car().get_fixedpoint());
					args = args.get_cdr();
				}
			}
			catch (NotSupportedException)
			{
			}
			return new FixedPointAtom(result);
		}

		public Atom func_bitwise_not(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "bitwise-not");
			
			if (arglist != null && arglist[0].atomtype == AtomType.FixedPoint)
				return new FixedPointAtom(~(arglist[0].get_fixedpoint()));
			else
				return NilAtom.nil;
		}
		
		public Atom func_bitwise_or(Atom args, Context context)
		{
			long result = 0;
			args = eval_args(args, context);
			try
			{
				while (true)
				{
					result = (result | args.get_car().get_fixedpoint());
					args = args.get_cdr();
				}
			}
			catch (NotSupportedException)
			{
			}
			return new FixedPointAtom(result);
		}
		
		public Atom func_bitwise_xor(Atom args, Context context)
		{
			long result = 0;
			args = eval_args(args, context);
			try
			{
				while (true)
				{
					result = (result ^ args.get_car().get_fixedpoint());
					args = args.get_cdr();
				}
			}
			catch (NotSupportedException)
			{
			}
			return new FixedPointAtom(result);
		}

		private void add_custom_function(Atom args, bool eval_args_first, Context context)
		{
			CustomLispFunction f;
			
			if (immutable)
				return;
			
			f = CustomLispFunction.from_args(args, eval_args_first);
			
			if (f != null)
			{
				custom_functions[f.name] = f;
			}
		}
		
		public Atom func_define(Atom args, Context context)
		{
			add_custom_function(args, true, context);
			
			return NilAtom.nil;
		}
		
		public Atom func_defineex(Atom args, Context context)
		{
			add_custom_function(args, false, context);
			
			return NilAtom.nil;
		}
		
		public Atom func_delglobal(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "delglobal");
			
			if (arglist != null)
				set_global(arglist[0], NilAtom.nil, context);
			
			return NilAtom.nil;
		}
		
		public virtual void write_line(string line, bool error, object user_data)
		{
			if (error)
				Console.Error.WriteLine(line);
			else
				Console.WriteLine(line);
		}
		
		public Atom func_display(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "display");
			
			if (arglist != null)
				write_line(arglist[0].ToString(), false, context);
			
			return NilAtom.nil;
		}
		
		public Atom func_eval(Atom args, Context context)
		{
			Atom result;
			
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "eval");
			
			if (arglist == null)
				result = NilAtom.nil;
			else
				result = eval(arglist[0], context);
			
			return result;
		}
		
		public Atom func_get(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "get");
			Atom result = NilAtom.nil;
			
			if (arglist != null)
			{
				if (!context.dict.TryGetValue(arglist[0], out result))
					result = NilAtom.nil;
			}
			
			return result;
		}
		
		public virtual Atom get_global(Atom name, Context context)
		{
			Atom result;
			
			if (!globals.TryGetValue(name, out result))
				result = NilAtom.nil;
			
			return result;
		}
		
		public Atom func_getglobal(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "getglobal");
			
			if (arglist == null)
				return NilAtom.nil;
			else
				return get_global(arglist[0], context);
		}
		
		public Atom func_if(Atom args, Context context)
		{
			Atom[] arglist = get_n_args(args, 3, "if");
			Atom condition_result;
			
			if (arglist == null)
				return NilAtom.nil;
			
			condition_result = eval(arglist[0], context);
			
			if (condition_result.is_true())
				return eval(arglist[1], context);
			else
				return eval(arglist[2], context);
		}
		
		public Atom func_let(Atom args, Context context)
		{
			Atom assignments;
			Atom inner_block;
			Context new_context = context.Copy();
			
			try
			{
				assignments = args.get_car();
				inner_block = args.get_cdr().get_car();
			}
			catch (NotSupportedException)
			{
				return NilAtom.nil;
			}
			
			while (assignments.atomtype == AtomType.Cons)
			{
				Atom assignment = assignments.get_car();
				Atom key;
				Atom expression;
				
				try
				{
					key = assignment.get_car();
					expression = assignment.get_cdr().get_car();
				}
				catch (NotSupportedException)
				{
					break;
				}
				
				key = eval(key, context);
				new_context.dict[key] = eval(expression, context);
				
				assignments = assignments.get_cdr();
			}
			
			return eval(inner_block, new_context);
		}
		
		public Atom func_let_splat(Atom args, Context context)
		{
			Atom assignments;
			Atom inner_block;
			Context new_context = context.Copy();
			
			try
			{
				assignments = args.get_car();
				inner_block = args.get_cdr().get_car();
			}
			catch (NotSupportedException)
			{
				return NilAtom.nil;
			}
			
			while (assignments.atomtype == AtomType.Cons)
			{
				Atom assignment = assignments.get_car();
				Atom key;
				Atom expression;
				
				try
				{
					key = assignment.get_car();
					expression = assignment.get_cdr().get_car();
				}
				catch (NotSupportedException)
				{
					break;
				}
				
				key = eval(key, context);
				new_context.dict[key] = eval(expression, new_context);
				
				assignments = assignments.get_cdr();
			}
			
			return eval(inner_block, new_context);
		}
		
		public Atom func_list(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			return args;
		}
		
		public Atom func_or(Atom args, Context context)
		{
			Atom result = NilAtom.nil;
			
			while (args.atomtype == AtomType.Cons)
			{
				result = eval(args.get_car(), context);
				
				if (result.is_true())
					break;
				
				args = args.get_cdr();
			}
			
			return result;
		}
		
		public Atom func_not(Atom args, Context context)
		{
			Atom[] arglist;
			
			args = eval_args(args, context);
			
			arglist = get_n_args(args, 1, "not");
			
			if (arglist != null)
			{
				if (arglist[0].is_true())
					return FixedPointAtom.Zero;
				else
					return FixedPointAtom.One;
			}
			else
				return NilAtom.nil;
		}
		
		public Atom func_quot(Atom args, Context context)
		{
			return args;
		}
		
		public virtual void set_global(Atom name, Atom val, Context context)
		{
			if (immutable)
				return;
			
			if (val == NilAtom.nil)
				globals.Remove(name);
			else
				globals[name] = val;
		}
		
		public Atom func_setglobal(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 2, "setglobal");
			
			if (arglist != null)
			{
				set_global(arglist[0], arglist[1], context);
			}
			
			return NilAtom.nil;
		}
		
		public Atom func_trunc(Atom args, Context context)
		{
			args = eval_args(args, context);
			
			Atom[] arglist = get_n_args(args, 1, "trunc");
			
			if (arglist == null || arglist[0].atomtype != AtomType.FixedPoint)
				return NilAtom.nil;
			else
				return new FixedPointAtom(arglist[0].get_fixedpoint() & -0x10000);
		}

		public Atom eval_custom(CustomLispFunction f, Atom args, Context context)
		{
			Context new_context;
			
			if (f.eval_args_first)
				args = eval_args(args, context);
			
			new_context = context.Copy();
			
			Atom.pattern_match(new_context.dict, f.args, args);
			
			return eval(f.body, new_context);
		}
		
		public Atom eval(Atom args, Context context)
		{
			if (args.atomtype == AtomType.Cons)
			{
				Atom function_name = args.get_car();
				Atom function_args = args.get_cdr();
				Atom result;
				LispFunction func;
				CustomLispFunction custom_func;
				if (trace_call)
					System.Console.Error.WriteLine("CALL {0}", args);
				if (custom_functions.TryGetValue(function_name, out custom_func))
				{
					result = eval_custom(custom_func, function_args, context);
				}
				else if (functions.TryGetValue(function_name, out func))
				{
					result = func(function_args, context);
				}
				else
					throw new Exception(String.Format("Function {0} not found", function_name));
				if (trace_call)
					System.Console.Error.WriteLine("RET {0} {1}", args, result);
				return result;
			}
			else
				return args;
		}

		public Atom eval_args(Atom args, Context context)
		{
			if (args.atomtype == AtomType.Cons)
			{
				Atom car = args.get_car();
				Atom cdr = args.get_cdr();
				return new ConsAtom(eval(car, context), eval_args(cdr, context));
			}
			else
				return args;
		}
		
		public virtual void save_state(AtomWriter atom_writer)
		{
			Atom[] args = new Atom[3];
			
			args[0] = atom_setglobal;
			foreach (KeyValuePair<Atom, Atom> kvp in globals)
			{
				args[1] = kvp.Key;
				args[2] = kvp.Value.escape();
				
				atom_writer.Write(Atom.from_array(args));
			}
			
			foreach (KeyValuePair<Atom, CustomLispFunction> kvp in custom_functions)
			{
				if (kvp.Value.eval_args_first)
					args[0] = atom_define;
				else
					args[0] = atom_defineex;
				
				args[1] = new ConsAtom(kvp.Key, kvp.Value.args);
				args[2] = kvp.Value.body;
				
				atom_writer.Write(Atom.from_array(args));
			}
		}
	}
}
