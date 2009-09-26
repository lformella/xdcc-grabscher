using System;
using XG.Core;
using XG.Server;
using XG.Server.TCP;
using XG.Server.Web;

namespace XG.Server.Cmd
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			try
			{
				ServerRunner runner = new ServerRunner ();
				runner.Start ();
				
				if (Settings.Instance.StartTCPServer)
				{
					TCPServer tcp = new TCPServer ();
					tcp.Start (runner);
				}
				
				if (Settings.Instance.StartWebServer)
				{
					WebServer web = new WebServer ();
					web.Start (runner);
				}
			}
			catch (Exception ex)
			{
				// die bitch, but stay there
				Console.WriteLine(ex.ToString());
				Console.ReadLine();
			}
		}
	}
}
