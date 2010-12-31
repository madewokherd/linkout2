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
			functions[new StringAtom("-")] = func_sub;
			functions[new StringAtom("=")] = func_eq;
			functions[new StringAtom("and")] = func_and;
			functions[new StringAtom("apply")] = func_apply;
			functions[new StringAtom("begin")] = func_begin;
			functions[new StringAtom("bitwise-and")] = func_bitwise_and;
			functions[new StringAtom("bitwise-not")] = func_bitwise_not;
			functions[new StringAtom("bitwise-or")] = func_bitwise_or;
			functions[new StringAtom("bitwise-xor")] = func_bitwise_xor;
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
			functions[new StringAtom("trunc")] = func_trunc;

			custom_functions = new Dictionary<Atom, CustomLispFunction>();
			globals = new Dictionary<Atom, Atom>();
			
			immutable = false;
			isolated = false;
		}

		public delegate Atom LispFunction(Atom args, Context context);
		
		protected Dictionary<Atom, LispFunction> functions;

		protected Dictionary<Atom, CustomLispFunction> custom_functions;

		protected Dictionary<Atom, Atom> globals;
		
		/* If immutable is true, functions should not make changes to this object that will be visible to interpreted
		 * code. */
		public bool immutable;
		
		/* If immutable is true, functions should not make be able to explicitly read or write anything outside the
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
	}
}
