using System;
using Linkout;
using Linkout.Lisp;

namespace LinkoutConsole
{
	class MainClass
	{
		public static int Usage()
		{
			Console.Write(
@"Usage: linkout <command> [<switches>] [<filename>]

<commands>
  e: Execute a script or replay and write out the final frame

<switches>
  -si: Read data from stdin
");
			return 1;
		}
		
		public static int Execute (string[] args, System.IO.Stream input, object user_data)
		{
			// For now, just deserialize so we can test the parsing code
			System.IO.Stream output = System.Console.OpenStandardOutput();
			ScriptHost interpreter = new ScriptHost();
			Linkout.Lisp.Locals no_locals = new Linkout.Lisp.Locals();
			try
			{
				while (true)
				{
					Linkout.Lisp.Atom atom;
					atom = Linkout.Lisp.Atom.from_stream(input);
					atom = interpreter.eval(atom, no_locals, user_data);
					atom.write_to_stream(output);
				}
			}
			catch (System.IO.EndOfStreamException)
			{
			}
			return 0;
		}
		
		public static int Main (string[] args)
		{
			int i;
			bool stdin = false;
			string filename = null;
			System.IO.Stream input = null;
			
			if (args.Length == 0)
				return Usage();

			for (i=1; i<args.Length; i++)
			{
				if (args[i] == "-si")
					stdin = true;
				else if (args[i].StartsWith("-"))
				{
					Console.WriteLine(String.Format("linkout: invalid option %s", args[i]));
					return Usage();
				}
				else if (filename == null)
				{
					filename = args[i];
				}
				else
				{
					Console.WriteLine(String.Format("linkout: unexpected argument %s", args[i]));
					return Usage();
				}
			}
			
			if (stdin)
				input = System.Console.OpenStandardInput();
			else if (filename != null)
				input = System.IO.File.OpenRead(filename);
			
			if (args[0] == "e")
				return Execute(args, input, null);
			
			return Usage();
		}
	}
}
