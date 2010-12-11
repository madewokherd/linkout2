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

