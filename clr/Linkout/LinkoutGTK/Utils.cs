/*
 * Copyright 2010, 2011 Vincent Povirk
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
using System.IO;
using Mono.Unix;
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
			
			Console.WriteLine(exc);
			
			secondary_text = exc.Message;
			
			markup = string.Format("<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}",
			                       html_escape(primary_text), html_escape(secondary_text));
			
			using (MessageDialog error_dialog = new MessageDialog(parent, DialogFlags.Modal,
			                                                      MessageType.Error,
			                                                      ButtonsType.Ok, markup))
			{
				error_dialog.Run();
				error_dialog.Destroy();
			}
		}
	}
}

