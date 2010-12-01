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
		}
		
		public delegate Atom LispFunction(Atom args, Locals locals);
		
		protected Dictionary<Atom, LispFunction> functions;
		
		public Atom func_plus(Atom args, Locals locals)
		{
			long result = 0;
			args = eval_args(args, locals);
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

		public Atom eval(Atom args, Locals locals)
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
				return func(function_args, locals);
			}
			else
				return args;
		}

		public Atom eval_args(Atom args, Locals locals)
		{
			if (args.atomtype == AtomType.Cons)
			{
				Atom car = args.get_car();
				Atom cdr = args.get_cdr();
				return new ConsAtom(eval(car, locals), eval_args(cdr, locals));
			}
			else
				return args;
		}
	}
}
