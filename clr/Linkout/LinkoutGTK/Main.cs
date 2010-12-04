using System;
using Mono.Unix;
using Gtk;

namespace LinkoutGTK
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Catalog.Init("i18n", "./locale"); // probably not good enough for actual translation
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();
		}
	}
}

