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

