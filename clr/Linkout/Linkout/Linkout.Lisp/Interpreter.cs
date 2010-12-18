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
			functions[new StringAtom("let")] = func_let;
			functions[new StringAtom("let*")] = func_let_splat;
		}

		public Interpreter (Dictionary<Atom, LispFunction> functions)
		{
			this.functions = functions;
		}

		public delegate Atom LispFunction(Atom args, Locals locals, object user_data);
		
		protected Dictionary<Atom, LispFunction> functions;
		
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

		public Atom eval(Atom args, Locals locals, object user_data)
		{
			if (args.atomtype == AtomType.Cons)
			{
				Atom function_name = args.get_car();
				Atom function_args = args.get_cdr();
				LispFunction func;
				if (!functions.TryGetValue(function_name, out func))
				{
					throw new Exception(String.Format("Function {0} not found", function_name));
				}
				return func(function_args, locals, user_data);
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
