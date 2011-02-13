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
using System.IO.Compression;
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

			pressed_keys = new Dictionary<uint, bool>();
			recently_pressed_keys = new Dictionary<uint, bool>();
			
			frame_delay = 20; /* 50 frames per second */
			runstate = RunState.Nothing;
			set_mode(RunMode.Gameplay);
		}
	
		ScriptHost scripthost;
		LinkoutDrawing.Drawing drawing;
		
		ReplayLogger replay_logger;
		
		RunState runstate;
		RunMode runmode;
		int frame_delay;
		uint advance_timer;
		
		bool checksum_failed;
		
		public virtual void Quit ()
		{
			if (replay_logger != null)
			{
				replay_logger.Close();
				replay_logger = null;
			}

			Application.Quit ();
		}
		
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Quit();
			a.RetVal = true;
		}
		
		protected virtual void OnQuitClicked (object sender, System.EventArgs e)
		{
			Quit();
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

		protected void OnFrameChange()
		{
			this.drawingarea.QueueDraw();
			
			if (scripthost == null || scripthost.frame == null)
				return;
			
			this.SeekBar.SetRange(0, scripthost.last_frame.frame_number);
			this.SeekBar.Value = (double)scripthost.frame.frame_number;
		}
		
		public bool advance()
		{
			switch (runmode)
			{
			case RunMode.Gameplay:
				try
				{
					scripthost.advance_frame(get_external_events());
				}
				catch (Exception e)
				{
					Utils.show_error_dialog(e, this, "Error running script");
					set_state(RunState.Stopped);
				}
				
				return true;
			case RunMode.Review:
				if (!scripthost.seek_to(scripthost.frame.frame_number + 1))
					return false;
				
				return true;
			default:
				return false;
			}
		}
		
		public bool skip_backwards()
		{
			if (scripthost.frame == null)
				return false;
			
			if (!scripthost.seek_to(scripthost.frame.frame_number - 1))
				return false;
			
			return true;
		}
		
		bool frame_timeout()
		{
			switch (runstate)
			{
			case RunState.Play:
				return advance();
			case RunState.Rewind:
				return skip_backwards();
			case RunState.Nothing:
			case RunState.Stopped:
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
				advance_timer = GLib.Timeout.Add((uint)frame_delay, new TimeoutHandler(frame_timeout));
				this.frame_delay = frame_delay;
			}
			
			recently_pressed_keys.Clear();
			
			runstate = new_state;
			
			if (runstate == RunState.Stopped)
				PauseContinueAction.Label = PauseContinueAction.ShortLabel = Catalog.GetString("Continue");
			else
				PauseContinueAction.Label = PauseContinueAction.ShortLabel = Catalog.GetString("Pause");
		}
		
		public void set_state (RunState new_state)
		{
			set_state(new_state, frame_delay);
		}
		
		public void set_mode (RunMode new_mode)
		{
			runmode = new_mode;
			
			switch (new_mode)
			{
			case RunMode.Gameplay:
				GameplayAction.Active = true;
				break;
			case RunMode.Review:
				ReviewAction.Active = true;
				break;
			case RunMode.Edit:
				EditAction.Active = true;
				break;
			}
			
			ReviewControls.Visible = (new_mode == RunMode.Review);
			
			set_state(runstate, frame_delay);
		}
		
		private Atom checksum_atom = new StringAtom("checksum");
		private Atom replay_file_atom = new StringAtom("replay-file");
		
		public void hint (Atom args)
		{
			if (args.GetType() == typeof(ConsAtom))
			{
				Atom hint_type = args.get_car();
				
				if (hint_type.Equals(replay_file_atom))
				{
					set_mode(RunMode.Review);
				}
				else if (hint_type.Equals(checksum_atom) && !checksum_failed && scripthost != null && scripthost.frame != null)
				{
					if (args.get_cdr() is ConsAtom && args.get_cdr().get_car() is FixedPointAtom)
					{
						int checksum = (int)(args.get_cdr().get_car().get_fixedpoint() >> 16);
						if (checksum != scripthost.frame.frame_hash())
						{
							checksum_failed = true;
							/* FIXME: Show this somewhere in the GUI? */
							System.Console.WriteLine("WARNING: Checksum failed at frame {0}", scripthost.frame.frame_number);
						}
					}
				}
			}
		}
		
		public virtual string get_replay_path()
		{
			string replay_path;

			replay_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			replay_path = System.IO.Path.Combine(replay_path, "linkout");
			replay_path = System.IO.Path.Combine(replay_path, "replays");
			
			return replay_path;
		}
		
		public virtual void start_replay_logging(string name_hint)
		{
			string replay_path, filename, basename, timestamp;
			const string timestamp_format = "yyyy'-'MM'-'dd'T'HHmmss' '";
			FileStream outfile = null;
			GZipStream outstream = null;
			int i = 0;
			AtomWriter atom_writer;
			
			if (replay_logger != null)
			{
				replay_logger.Close();
				replay_logger = null;
			}
			
			replay_path = get_replay_path();
			
			System.IO.Directory.CreateDirectory(replay_path);
			
			timestamp = DateTime.Now.ToString(timestamp_format);
			
			basename = timestamp + name_hint;
			
			filename = System.IO.Path.Combine(replay_path, basename + ".lotb.gz");
			
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
					
					filename = System.IO.Path.Combine(replay_path, basename + "-" + i.ToString() + ".lotb.gz");
				}
				catch (Exception exc)
				{
					Console.WriteLine("Cannot create replay log");
					Console.WriteLine(exc);
					return;
				}
			}
			
			outstream = new GZipStream(outfile, CompressionMode.Compress);
			
			atom_writer = new BinaryAtomWriter(outstream);
			
			replay_logger = new ReplayLogger(scripthost, atom_writer);
		}
		
		protected virtual void OnOpenActivated (object sender, System.EventArgs e)
		{
			Stream infile;
			AtomReader instream;
			string filename;
			int response;
			
			using (FileChooserDialog dialog = new FileChooserDialog(Catalog.GetString("Open File"), this,
			                                                        FileChooserAction.Open,
			                                                        Stock.Cancel, ResponseType.Cancel,
			                                                        Stock.Open, ResponseType.Accept))
			{
				dialog.AddShortcutFolder(get_replay_path());
				
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
				instream = AtomReader.FromStream(infile);
				
				try
				{
					scripthost = new ScriptHost();
					Context context = new Context();
					drawing = new LinkoutDrawing.Drawing();
					
					set_mode(RunMode.Unspecified);
					
					scripthost.OnHint += hint;
					
					start_replay_logging(System.IO.Path.GetFileNameWithoutExtension(filename));
					
					checksum_failed = false;
					
					while (true)
					{
						Atom atom;
						atom = instream.Read();
						if (atom == null)
							break;
						scripthost.eval(atom, context);
					}
					
					instream.Close();
					
					if (runmode == RunMode.Unspecified)
						set_mode(RunMode.Gameplay);
					
					set_state(RunState.Play, 20);
					
					if (scripthost.last_frame != null && runmode == RunMode.Review &&
					    scripthost.last_frame.frame_number == scripthost.frame.frame_number)
					{
						scripthost.seek_to(0);
					}
					
					this.drawingarea.QueueDraw();
					
					scripthost.OnFrameChange += OnFrameChange;
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

			if (this.scripthost != null && this.scripthost.frame != null)
			{
				draw_current_frame();
			}
			else
			{
				this.drawingarea.GdkWindow.Clear();
			}
		}
		
		protected virtual void OnGuiModeChange (object sender, System.EventArgs e)
		{
			if (GameplayAction.Active)
				set_mode(RunMode.Gameplay);
			else if (ReviewAction.Active)
				set_mode(RunMode.Review);
			else if (EditAction.Active)
				set_mode(RunMode.Edit);
		}
		
		protected virtual void OnPauseContinueActionActivated (object sender, System.EventArgs e)
		{
			if (runstate == RunState.Stopped)
			{
				set_state(RunState.Play);
			}
			else
			{
				set_state(RunState.Stopped);
			}
		}
		
		protected virtual void OnPlayActionActivated (object sender, System.EventArgs e)
		{
			set_state(RunState.Play);
		}
		
		protected virtual void OnRewindActionActivated (object sender, System.EventArgs e)
		{
			set_state(RunState.Rewind);
		}
		
		protected virtual void OnSkipForwardActionActivated (object sender, System.EventArgs e)
		{
			advance();
			set_state(RunState.Stopped);
		}
		
		protected virtual void OnSkipBackwardsActionActivated (object sender, System.EventArgs e)
		{
			skip_backwards();
			set_state(RunState.Stopped);
		}
		
		protected virtual void OnSkipToActionActivated (object sender, System.EventArgs e)
		{
			if (this.scripthost == null || this.scripthost.last_frame == null)
				return;
			
			using (SkipToDialog dialog = new SkipToDialog())
			{
				dialog.set_max_framenum(this.scripthost.last_frame.frame_number);
				
				if (dialog.Run() == (int)ResponseType.Ok)
				{
					this.scripthost.seek_to(dialog.get_framenum());
					set_state(RunState.Stopped);
				}
				
				dialog.Destroy();
			}
		}
		
		protected virtual void OnSeekBarValueChanged (object sender, System.EventArgs e)
		{
			uint new_value = (uint)(SeekBar.Value+0.5);
			
			if (scripthost != null && scripthost.frame != null &&
			    scripthost.frame.frame_number != new_value)
			{
				scripthost.seek_to(new_value);
				set_state(RunState.Stopped);
			}
		}
		
		protected virtual void OnSaveAsActionActivated (object sender, System.EventArgs e)
		{
			int response;
			string filename;
			
			if (scripthost == null)
				return;
			
			using (FileChooserDialog dialog = new FileChooserDialog(Catalog.GetString("Save File"), this,
			                                                        FileChooserAction.Save,
			                                                        Stock.Cancel, ResponseType.Cancel,
			                                                        Stock.Save, ResponseType.Accept))
			{
				response = dialog.Run();
				filename = dialog.Filename;
	
				dialog.Destroy();
			}
			
			if (response != (int)ResponseType.Accept)
				return;
			
			try
			{
				using (Stream outfile = new FileStream(filename, FileMode.CreateNew))
				{
					StreamAtomWriter atom_writer = new StreamAtomWriter(outfile);
					
					if (runmode == RunMode.Review)
					{
						atom_writer.Write(new ConsAtom(new StringAtom("hint"), new ConsAtom(new StringAtom("replay-file"), NilAtom.nil)));
					}
					
					scripthost.save_state(atom_writer);
					
					atom_writer.Close();
				}
			}
			catch (Exception exc)
			{
				Utils.show_error_dialog(exc, this, String.Format(Catalog.GetString("Cannot save '{0}'"), filename));
				return;
			}
		}
	}
}
