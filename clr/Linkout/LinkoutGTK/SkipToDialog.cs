/*
 * Copyright 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Linkout is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *  
 * You should have received a copy of the GNU General Public License
 * along with Linkout.  If not, see <http://www.gnu.org/licenses/>.
 */

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

