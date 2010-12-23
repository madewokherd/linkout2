using System;
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public class Interpreter
	{
		public Interpreter ()
		{
			functions = new Dictionary<Atom, LispFunction>();
			functions[new StringAtom("*")] = func_mult;
			functions[new StringAtom("+")] = func_plus;
			functions[new StringAtom("=")] = func_eq;
			functions[new StringAtom("and")] = func_and;
			functions[new StringAtom("apply")] = func_apply;
			functions[new StringAtom("begin")] = func_begin;
			functions[new StringAtom("define")] = func_define;
			functions[new StringAtom("defineex")] = func_defineex;
			functions[new StringAtom("delglobal")] = func_delglobal;
			functions[new StringAtom("display")] = func_display;
			functions[new StringAtom("eval")] = func_eval;
			functions[new StringAtom("get")] = func_get;
			functions[new StringAtom("getglobal")] = func_getglobal;
			functions[new StringAtom("getlocal")] = func_get;
			functions[new StringAtom("if")] = func_if;
			functions[new StringAtom("let")] = func_let;
			functions[new StringAtom("let*")] = func_let_splat;
			functions[new StringAtom("list")] = func_list;
			functions[new StringAtom("not")] = func_not;
			functions[new StringAtom("or")] = func_or;
			functions[new StringAtom("quot")] = func_quot;
			functions[new StringAtom("setglobal")] = func_setglobal;

			custom_functions = new Dictionary<Atom, CustomLispFunction>();
			globals = new Dictionary<Atom, Atom>();
		}

		public Interpreter (Dictionary<Atom, LispFunction> functions)
		{
			this.functions = functions;
			custom_functions = new Dictionary<Atom, CustomLispFunction>();
			globals = new Dictionary<Atom, Atom>();
		}

		public delegate Atom LispFunction(Atom args, Locals locals, object user_data);
		
		protected Dictionary<Atom, LispFunction> functions;

		protected Dictionary<Atom, CustomLispFunction> custom_functions;

		protected Dictionary<Atom, Atom> globals;
		
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

		public Atom func_mult(Atom args, Locals locals, object user_data)
		{
			long result = 0x10000;
			args = eval_args(args, locals, user_data);
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
		
		public Atom func_plus(Atom args, Locals locals, object user_data)
		{
			long result = 0;
			args = eval_args(args, locals, user_data);
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
		
		public Atom func_eq(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 2, "=");
			
			if (arglist[0].Equals(arglist[1]))
				return FixedPointAtom.One;
			else
				return FixedPointAtom.Zero;
		}
		
		public Atom func_and(Atom args, Locals locals, object user_data)
		{
			Atom result = NilAtom.nil;
			
			while (args.atomtype == AtomType.Cons)
			{
				result = eval(args.get_car(), locals, user_data);
				
				if (!result.is_true())
					break;
				
				args = args.get_cdr();
			}
			
			return result;
		}
		
		public Atom func_apply(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 2, "apply");
			
			if (arglist == null)
				return NilAtom.nil;
			else
				return eval(new ConsAtom(arglist[0], arglist[1]), locals, user_data);
		}
		
		public Atom func_begin(Atom args, Locals locals, object user_data)
		{
			Atom result = NilAtom.nil;
			
			while (args.atomtype == AtomType.Cons)
			{
				result = eval(args.get_car(), locals, user_data);
				
				args = args.get_cdr();
			}
			
			return result;
		}
		
		public virtual void add_custom_function(Atom args, bool eval_args_first, object user_data)
		{
			CustomLispFunction f;
			
			f = CustomLispFunction.from_args(args, eval_args_first);
			
			if (f != null)
			{
				custom_functions[f.name] = f;
			}
		}
		
		public Atom func_define(Atom args, Locals locals, object user_data)
		{
			add_custom_function(args, true, user_data);
			
			return NilAtom.nil;
		}
		
		public Atom func_defineex(Atom args, Locals locals, object user_data)
		{
			add_custom_function(args, false, user_data);
			
			return NilAtom.nil;
		}
		
		public Atom func_delglobal(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 1, "delglobal");
			
			if (arglist != null)
				set_global(arglist[0], NilAtom.nil, user_data);
			
			return NilAtom.nil;
		}
		
		public virtual void write_line(string line, bool error, object user_data)
		{
			if (error)
				Console.Error.WriteLine(line);
			else
				Console.WriteLine(line);
		}
		
		public Atom func_display(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 1, "display");
			
			if (arglist != null)
				write_line(arglist[0].ToString(), false, user_data);
			
			return NilAtom.nil;
		}
		
		public Atom func_eval(Atom args, Locals locals, object user_data)
		{
			Atom result;
			
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 1, "eval");
			
			if (arglist == null)
				result = NilAtom.nil;
			else
				result = eval(arglist[0], locals, user_data);
			
			return result;
		}
		
		public Atom func_get(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 1, "get");
			Atom result = NilAtom.nil;
			
			if (arglist != null)
				result = locals.get_value(arglist[0]);
			
			return result;
		}
		
		public virtual Atom get_global(Atom name, object user_data)
		{
			Atom result;
			
			if (!globals.TryGetValue(name, out result))
				result = NilAtom.nil;
			
			return result;
		}
		
		public Atom func_getglobal(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 1, "getglobal");
			
			if (arglist == null)
				return NilAtom.nil;
			else
				return get_global(arglist[0], user_data);
		}
		
		public Atom func_if(Atom args, Locals locals, object user_data)
		{
			Atom[] arglist = get_n_args(args, 3, "if");
			Atom condition_result;
			
			if (arglist == null)
				return NilAtom.nil;
			
			condition_result = eval(arglist[0], locals, user_data);
			
			if (condition_result.is_true())
				return eval(arglist[1], locals, user_data);
			else
				return eval(arglist[2], locals, user_data);
		}
		
		public Atom func_let(Atom args, Locals locals, object user_data)
		{
			Atom assignments;
			Atom inner_block;
			Locals new_locals = new Locals(locals);
			
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
				
				key = eval(key, locals, user_data);
				new_locals.dict[key] = eval(expression, locals, user_data);
				
				assignments = assignments.get_cdr();
			}
			
			return eval(inner_block, new_locals, user_data);
		}
		
		public Atom func_let_splat(Atom args, Locals locals, object user_data)
		{
			Atom assignments;
			Atom inner_block;
			Locals new_locals = new Locals(locals);
			
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
				
				key = eval(key, new_locals, user_data);
				new_locals.dict[key] = eval(expression, new_locals, user_data);
				
				assignments = assignments.get_cdr();
			}
			
			return eval(inner_block, new_locals, user_data);
		}
		
		public Atom func_list(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			return args;
		}
		
		public Atom func_or(Atom args, Locals locals, object user_data)
		{
			Atom result = NilAtom.nil;
			
			while (args.atomtype == AtomType.Cons)
			{
				result = eval(args.get_car(), locals, user_data);
				
				if (result.is_true())
					break;
				
				args = args.get_cdr();
			}
			
			return result;
		}
		
		public Atom func_not(Atom args, Locals locals, object user_data)
		{
			Atom[] arglist;
			
			args = eval_args(args, locals, user_data);
			
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
		
		public Atom func_quot(Atom args, Locals locals, object user_data)
		{
			return args;
		}
		
		public virtual void set_global(Atom name, Atom val, object user_data)
		{
			if (val == NilAtom.nil)
				globals.Remove(name);
			else
				globals[name] = val;
		}
		
		public Atom func_setglobal(Atom args, Locals locals, object user_data)
		{
			args = eval_args(args, locals, user_data);
			
			Atom[] arglist = get_n_args(args, 2, "setglobal");
			
			if (arglist != null)
			{
				set_global(arglist[0], arglist[1], user_data);
			}
			
			return NilAtom.nil;
		}

		public Atom eval_custom(CustomLispFunction f, Atom args, Locals locals, object user_data)
		{
			Locals new_locals;
			
			if (f.eval_args_first)
				args = eval_args(args, locals, user_data);
			
			new_locals = Locals.from_pattern(locals, f.args, args);
			
			return eval(f.body, new_locals, user_data);
		}
		
		public Atom eval(Atom args, Locals locals, object user_data)
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
					result = eval_custom(custom_func, function_args, locals, user_data);
				}
				else if (functions.TryGetValue(function_name, out func))
				{
					result = func(function_args, locals, user_data);
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

		public Atom eval_args(Atom args, Locals locals, object user_data)
		{
			if (args.atomtype == AtomType.Cons)
			{
				Atom car = args.get_car();
				Atom cdr = args.get_cdr();
				return new ConsAtom(eval(car, locals, user_data), eval_args(cdr, locals, user_data));
			}
			else
				return args;
		}
	}
}
