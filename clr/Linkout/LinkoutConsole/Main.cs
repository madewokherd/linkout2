using System;

namespace LinkoutConsole
{
	class MainClass
	{
		public static int Usage()
		{
			Console.Write(
@"Usage: linkout <command> [<switches>] [<filename>]

<commands>
  e: Evaluate a script or replay and write out the final frame

<switches>
  -si: Read data from stdin
");
			return 1;
		}
		
		public static int Main (string[] args)
		{
			if (args.Length == 0)
				return Usage();
			return Usage();
		}
	}
}
