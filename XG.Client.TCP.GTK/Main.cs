//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

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
