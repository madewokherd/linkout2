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
using Gtk;

namespace LinkoutGTK
{
	public class Utils
	{
		public static string html_escape(string str)
		{
			return str.Replace("&", "&amp;").Replace("<", "&lt").Replace(">", "&gt");
		}
		
		public static void show_error_dialog(Exception exc, Gtk.Window parent, string primary_text)
		{
			string secondary_text, markup;
			MessageDialog error_dialog;
			
			Console.WriteLine(exc);
			
			secondary_text = exc.Message;
			
			markup = string.Format("<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}",
			                       html_escape(primary_text), html_escape(secondary_text));
			
			error_dialog = new MessageDialog(parent, DialogFlags.Modal, MessageType.Error,
			                                 ButtonsType.Ok, markup);
			
			error_dialog.Run();
			error_dialog.Destroy();
		}
	}
}

