using System;
using Gtk;

namespace XG.Client.TCP.GTK
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			MainWindow view = null;
#if !UNSAFE
			try
			{
#endif
				Application.Init();
				view = new MainWindow();
				view.Show();
				Application.Run();
#if !UNSAFE
			}
			catch(Exception ex)
			{
				MessageDialog md = new MessageDialog(view, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Close, "Error:\n" + ex.ToString());
				md.Run();
				md.Destroy();
			}
#endif
		}
	}
}
