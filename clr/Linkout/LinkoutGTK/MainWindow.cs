using System;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;
using Gtk;
using Linkout;
using Linkout.Lisp;

namespace LinkoutGTK
{
	public partial class MainWindow : Gtk.Window
	{
		public MainWindow () : base(Gtk.WindowType.Toplevel)
		{
			Build ();
		}
	
		//ScriptHost scripthost;
		
		//LinkedList<Atom> script_commands;
		
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}
	
		protected virtual void OnQuitClicked (object sender, System.EventArgs e)
		{
			Application.Quit ();
		}
		
		protected virtual void OnOpenActivated (object sender, System.EventArgs e)
		{
			FileChooserDialog dialog;
			Stream infile;
			string filename;
			int response;
			
			dialog = new FileChooserDialog(Catalog.GetString("Open File"), this,
			                               FileChooserAction.Open,
			                               Stock.Cancel, ResponseType.Cancel,
			                               Stock.Open, ResponseType.Accept);
			
			response = dialog.Run();
			filename = dialog.Filename;

			dialog.Destroy();

			if (response == (int)ResponseType.Cancel)
				return;
			
			try
			{
				infile = new FileStream(filename, FileMode.Open, FileAccess.Read);
			}
			catch (Exception exc)
			{
				Utils.show_error_dialog(exc, this, String.Format(Catalog.GetString("Cannot open '{0}'"), filename));
				return;
			}
			
			try
			{
				/*
				try
				{
					while (true)
					{
						Atom atom;
						atom = Atom.from_stream(infile);
					}
				}
				catch (System.IO.EndOfStreamException)
				{
				}
				*/
			}
			finally
			{
				infile.Close();
			}
		}
	}
}
	