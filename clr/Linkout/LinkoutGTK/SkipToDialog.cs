using System;
namespace LinkoutGTK
{
	public partial class SkipToDialog : Gtk.Dialog
	{
		public SkipToDialog ()
		{
			this.Build ();
		}
		
		public void set_max_framenum (uint max_frame)
		{
			this.FrameNumberButton.SetRange(0.0, (double)max_frame);
		}
		
		public uint get_framenum ()
		{
			return (uint)FrameNumberButton.ValueAsInt;
		}
	}
}

