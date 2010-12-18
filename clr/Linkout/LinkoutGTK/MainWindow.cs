using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Mono.Unix;
using GLib;
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
			runstate = RunState.Nothing;
			frame_delay = 20; /* 50 frames per second */
		}
	
		ScriptHost scripthost;
		LinkoutDrawing.Drawing drawing;
		
		RunState runstate;
		int frame_delay;
		uint advance_timer;
		
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}
	
		protected virtual void OnQuitClicked (object sender, System.EventArgs e)
		{
			Application.Quit ();
		}
		
				                 
		bool advance()
		{
			switch (runstate)
			{
			case RunState.Running:
				scripthost.advance_frame();
				this.drawingarea.QueueDraw();
				return true;
			case RunState.Playing:
				if (!scripthost.seek_to(scripthost.frame.frame_number + 1))
				{
					set_state(RunState.Stopped);
					return false;
				}
				this.drawingarea.QueueDraw();
				return true;
			default:
				return false;
			}
		}
		
		public void set_state (RunState new_state, int frame_delay)
		{
			if (scripthost == null || scripthost.frame == null)
				new_state = RunState.Nothing;
			
			if (advance_timer != 0)
			{
				GLib.Source.Remove(advance_timer);
				advance_timer = 0;
			}
			
			if (frame_delay != -1 && (new_state != RunState.Nothing && new_state != RunState.Stopped))
			{
				advance_timer = GLib.Timeout.Add((uint)frame_delay, new TimeoutHandler(advance));
				this.frame_delay = frame_delay;
			}
			
			runstate = new_state;
		}
		
		public void set_state (RunState new_state)
		{
			set_state(new_state, frame_delay);
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
					
					set_state(RunState.Running, 50);
					
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
		
		private void draw_current_frame()
		{
			System.Drawing.Graphics g = Gtk.DotNet.Graphics.FromDrawable(this.drawingarea.GdkWindow);
			RectangleF game_region;
			RectangleF output_region;
			int width;
			int height;
			
			try
			{
				width = this.drawingarea.Allocation.Size.Width;
				height = this.drawingarea.Allocation.Size.Height;
				
				game_region = new RectangleF(0, 0, width, height);
				output_region = new RectangleF(0, 0, width, height);
				
				drawing.DrawFrame(g, scripthost.frame, game_region, output_region);
			}
			finally
			{
				g.Dispose();
			}
		}
		
		protected virtual void OnDrawingareaExposeEvent (object o, Gtk.ExposeEventArgs args)
		{
			args.RetVal = true;

			switch (runstate)
			{
			case RunState.Nothing:
				this.drawingarea.GdkWindow.Clear();
				break;
			case RunState.Playing:
			case RunState.Stopped:
			case RunState.Running:
				draw_current_frame();
				break;
			}
		}
	}
}
	