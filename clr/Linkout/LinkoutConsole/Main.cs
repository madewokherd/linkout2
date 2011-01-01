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
using System.IO;
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
		
		Stream input;
		Stream output;
		ScriptHost interpreter;
		
		private void OnNewFrame()
		{
			interpreter.frame.to_atom().write_to_stream(output);
		}

		private void OnHint(Atom args)
		{
			new ConsAtom(new StringAtom("hint"), args).write_to_stream(output);
		}

		public int Execute (string[] args, System.IO.Stream input, Context context)
		{
			output = System.Console.OpenStandardOutput();
			interpreter = new ScriptHost();
			
			interpreter.OnNewFrame += OnNewFrame;
			interpreter.OnHint += OnHint;
			
			while (true)
			{
				Linkout.Lisp.Atom atom;
				atom = Linkout.Lisp.Atom.from_stream(input);
				if (atom == null)
					break;
				atom = interpreter.eval(atom, context);
			}
			return 0;
		}
		
		public int instance_main(string[] args)
		{
			int i;
			bool stdin = false;
			string filename = null;
			
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
				return Execute(args, input, new Context());
			
			return Usage();
		}
		
		public static int Main (string[] args)
		{
			return new MainClass().instance_main(args);
		}
	}
}
