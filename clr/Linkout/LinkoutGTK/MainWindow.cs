using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Mono.Unix;
using Gtk;
using Gtk.DotNet;
using Linkout;
using Linkout.Lisp;
using LinkoutDrawing;

namespace LinkoutGTK
{
	public partial class MainWindow : Gtk.Window
	{
		public MainWindow () : base(Gtk.WindowType.Toplevel)
		{
			Build ();
		}
	
		ScriptHost scripthost;
		LinkoutDrawing.Drawing drawing;
		
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
				try
				{
					scripthost = new ScriptHost();
					Locals no_locals = new Locals();
					drawing = new LinkoutDrawing.Drawing();
					
					while (true)
					{
						Atom atom;
						atom = Atom.from_stream(infile);
						if (atom == null)
							break;
						scripthost.eval(atom, no_locals, null);
					}
					
					this.drawingarea.QueueDraw();
				}
				catch (Exception exc)
				{
					Utils.show_error_dialog(exc, this, String.Format(Catalog.GetString("Cannot load '{0}'"), filename));
					return;
				}
			}
			finally
			{
				infile.Close();
			}
		}
		
		protected virtual void OnDrawingareaExposeEvent (object o, Gtk.ExposeEventArgs args)
		{
			System.Drawing.Graphics g = Gtk.DotNet.Graphics.FromDrawable(this.drawingarea.GdkWindow);
			RectangleF game_region;
			RectangleF output_region;
			int width;
			int height;

			args.RetVal = true;

			if (scripthost == null || scripthost.frame == null)
			{
				this.drawingarea.GdkWindow.Clear();
				return;
			}
			
			width = this.drawingarea.Allocation.Size.Width;
			height = this.drawingarea.Allocation.Size.Height;
			
			game_region = new RectangleF(0, 0, width, height);
			output_region = new RectangleF(0, 0, width, height);
			
			drawing.DrawFrame(g, scripthost.frame, game_region, output_region);
		}
	}
}
	