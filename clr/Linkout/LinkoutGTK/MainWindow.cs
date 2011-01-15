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
			
			pressed_keys = new Dictionary<uint, bool>();
			recently_pressed_keys = new Dictionary<uint, bool>();
		}
	
		ScriptHost scripthost;
		LinkoutDrawing.Drawing drawing;
		
		ReplayLogger replay_logger;
		
		RunState runstate;
		RunMode runmode;
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

		Dictionary<uint, bool> pressed_keys;

		Dictionary<uint, bool> recently_pressed_keys;
		
		protected virtual void OnDrawingareaKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			if (!pressed_keys.ContainsKey(args.Event.KeyValue))
				recently_pressed_keys[args.Event.KeyValue] = true;
			pressed_keys[args.Event.KeyValue] = true;
		}
		
		protected virtual void OnDrawingareaKeyReleaseEvent (object o, Gtk.KeyReleaseEventArgs args)
		{
			pressed_keys.Remove(args.Event.KeyValue);
		}
		
		private Atom get_key_event(StringAtom name, uint keyval)
		{
			Atom val;
			
			if (pressed_keys.ContainsKey(keyval) || recently_pressed_keys.ContainsKey(keyval))
				val = FixedPointAtom.One;
			else
				val = FixedPointAtom.Zero;
			
			return new ConsAtom(new StringAtom("setglobal"), new ConsAtom(name, new ConsAtom(val, NilAtom.nil)));
		}
		
		private Atom[] get_external_events()
		{
			List<Atom> result = new List<Atom>();
			
			// Pressed keys
			
			// FIXME: Don't hard-code the key values.
			result.Add(get_key_event(new StringAtom("left-pressed"), (uint)Gdk.Key.Left));
			result.Add(get_key_event(new StringAtom("right-pressed"), (uint)Gdk.Key.Right));
			result.Add(get_key_event(new StringAtom("up-pressed"), (uint)Gdk.Key.Up));
			result.Add(get_key_event(new StringAtom("down-pressed"), (uint)Gdk.Key.Down));
			
			recently_pressed_keys.Clear();
			
			return result.ToArray();
		}

		bool advance()
		{
			switch (runstate)
			{
			case RunState.Gameplay:
				try
				{
					scripthost.advance_frame(get_external_events());
				}
				catch (Exception e)
				{
					Utils.show_error_dialog(e, this, "Error running script");
					set_state(RunState.Stopped);
				}
				this.drawingarea.QueueDraw();
				return true;
			case RunState.Review:
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
			
			recently_pressed_keys.Clear();
			
			runstate = new_state;
		}
		
		public void set_state (RunState new_state)
		{
			set_state(new_state, frame_delay);
		}
		
		private Atom replay_file_atom = new StringAtom("replay-file");
		
		public void hint (Atom args)
		{
			if (args.GetType() == typeof(ConsAtom))
			{
				Atom hint_type = args.get_car();
				
				if (hint_type.Equals(replay_file_atom))
				{
					runmode = RunMode.Review;
				}
			}
		}
		
		public virtual void start_replay_logging(string name_hint)
		{
			string replay_path, filename, basename, timestamp;
			const string timestamp_format = "yyyy'-'MM'-'dd'T'HHmmss' '";
			FileStream outfile = null;
			int i = 0;
			AtomWriter atom_writer;
			
			if (replay_logger != null)
			{
				replay_logger.Close();
				replay_logger = null;
			}
			
			replay_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			replay_path = System.IO.Path.Combine(replay_path, "linkout");
			replay_path = System.IO.Path.Combine(replay_path, "replays");
			
			System.IO.Directory.CreateDirectory(replay_path);
			
			timestamp = DateTime.Now.ToString(timestamp_format);
			
			basename = timestamp + name_hint;
			
			filename = System.IO.Path.Combine(replay_path, basename + ".lot");
			
			while (outfile == null)
			{
				try
				{
					outfile = new FileStream(filename, FileMode.CreateNew);
				}
				catch (IOException exc)
				{
					i = i + 1;
					
					if (i >= 32)
					{
						Console.WriteLine("Cannot create replay log");
						Console.WriteLine(exc);
						return;
					}
					
					filename = System.IO.Path.Combine(replay_path, basename + "-" + i.ToString() + ".lot");
				}
				catch (Exception exc)
				{
					Console.WriteLine("Cannot create replay log");
					Console.WriteLine(exc);
					return;
				}
			}
			
			atom_writer = new StreamAtomWriter(outfile);
			
			replay_logger = new ReplayLogger(scripthost, atom_writer);
		}
		
		protected virtual void OnOpenActivated (object sender, System.EventArgs e)
		{
			Stream infile;
			string filename;
			int response;
			
			using (FileChooserDialog dialog = new FileChooserDialog(Catalog.GetString("Open File"), this,
			                                                        FileChooserAction.Open,
			                                                        Stock.Cancel, ResponseType.Cancel,
			                                                        Stock.Open, ResponseType.Accept))
			{
				response = dialog.Run();
				filename = dialog.Filename;
	
				dialog.Destroy();
			}

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
					Context context = new Context();
					drawing = new LinkoutDrawing.Drawing();
					
					runmode = RunMode.Unspecified;
					
					scripthost.OnHint += hint;
					
					start_replay_logging(System.IO.Path.GetFileNameWithoutExtension(filename));
					
					while (true)
					{
						Atom atom;
						atom = Atom.from_stream(infile);
						if (atom == null)
							break;
						scripthost.eval(atom, context);
					}
					
					if (runmode == RunMode.Unspecified)
						runmode = RunMode.Gameplay;
					
					if (runmode == RunMode.Gameplay)
						set_state(RunState.Gameplay, 20);
					else
					{
						if (scripthost.last_frame != null)
						{
							scripthost.seek_to(0);
						}
						set_state(RunState.Review, 20);
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
				infile.Dispose();
			}
		}
		
		private void draw_current_frame()
		{
			System.Drawing.Graphics g = Gtk.DotNet.Graphics.FromDrawable(this.drawingarea.GdkWindow);
			RectangleF output_region;
			int width;
			int height;
			
			try
			{
				width = this.drawingarea.Allocation.Size.Width;
				height = this.drawingarea.Allocation.Size.Height;
				
				output_region = new RectangleF(0, 0, width, height);
				
				drawing.DrawFrameAt(g, scripthost.frame, output_region);
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
			case RunState.Review:
			case RunState.Stopped:
			case RunState.Gameplay:
				draw_current_frame();
				break;
			}
		}
	}
}
	