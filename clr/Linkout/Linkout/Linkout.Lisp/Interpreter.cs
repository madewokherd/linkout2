using System;
using System.Collections.Generic;
namespace Linkout.Lisp
{
	public class Interpreter
	{
		public Interpreter ()
		{
			functions = new Dictionary<Atom, LispFunction>();
			functions[new StringAtom("+")] = func_plus;
			functions[new StringAtom("define")] = func_define;
			functions[new StringAtom("defineex")] = func_defineex;
			functions[new StringAtom("let")] = func_let;
			functions[new StringAtom("let*")] = func_let_splat;
			functions[new StringAtom("quot")] = func_quot;

			custom_functions = new Dictionary<Atom, CustomLispFunction>();
		}

		public Interpreter (Dictionary<Atom, LispFunction> functions)
		{
			this.functions = functions;
			custom_functions = new Dictionary<Atom, CustomLispFunction>();
		}

		public delegate Atom LispFunction(Atom args, Locals locals, object user_data);
		
		protected Dictionary<Atom, LispFunction> functions;

		protected Dictionary<Atom, CustomLispFunction> custom_functions;

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
		
		public virtual void add_custom_function(Atom args, bool eval_args_first)
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
			add_custom_function(args, true);
			
			return NilAtom.nil;
		}
		
		public Atom func_defineex(Atom args, Locals locals, object user_data)
		{
			add_custom_function(args, false);
			
			return NilAtom.nil;
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
				
				new_locals.dict[key] = eval(expression, new_locals, user_data);
				
				assignments = assignments.get_cdr();
			}
			
			return eval(inner_block, new_locals, user_data);
		}
		
		public Atom func_quot(Atom args, Locals locals, object user_data)
		{
			return args;
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
				LispFunction func;
				CustomLispFunction custom_func;
				if (custom_functions.TryGetValue(function_name, out custom_func))
				{
					return eval_custom(custom_func, args, locals, user_data);
				}
				if (functions.TryGetValue(function_name, out func))
				{
					return func(function_args, locals, user_data);
				}
				throw new Exception(String.Format("Function {0} not found", function_name));
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
