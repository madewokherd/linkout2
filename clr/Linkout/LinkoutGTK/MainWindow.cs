using System;
using Mono.Unix;
using Gtk;

public partial class MainWindow : Gtk.Window
{
	public MainWindow () : base(Gtk.WindowType.Toplevel)
	{
		Build ();
	}

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
		
		dialog = new FileChooserDialog(Catalog.GetString("Open File"), this,
		                               FileChooserAction.Open,
		                               Stock.Cancel, ResponseType.Cancel,
		                               Stock.Open, ResponseType.Accept);
		
		dialog.Run();
		
		/* TODO: Actually do something with the file. */
		
		dialog.Destroy();
	}
}

