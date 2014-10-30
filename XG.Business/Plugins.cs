// 
//  Plugins.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//  

using System;
using System.IO;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Collections.Generic;
using XG.Config.Properties;
using XG.Plugin;

namespace XG.Business
{
	public class Plugins
	{
		#region VARIABLES

		readonly HashSet<APlugin> _plugins;

		#endregion

		#region FUNCTIONS

		/// <summary>
		/// Loads the plugins, from the plugins directory
		/// </summary>
		/// <remarks>
		/// uses System.ComponentModel.Composition to do all the heavy lifting.
		/// </remarks>
		public static void Load(App obj)
		{
			var pluginsDirectoryInfo = new DirectoryInfo(Settings.Default.GetAppDataPath () + "plugins");
			if(!pluginsDirectoryInfo.Exists)
			{
				try
				{
					pluginsDirectoryInfo.Create();
				}
				catch(Exception ex)
				{
					Console.Write(ex.Message);
				}
			}

			var catalog = new AggregateCatalog();
			//catalog.Catalogs.Add(new AssemblyCatalog(typeof(App).Assembly));
			catalog.Catalogs.Add(new DirectoryCatalog(pluginsDirectoryInfo.ToString()));

			var container = new CompositionContainer(catalog);

			try
			{
				container.ComposeParts(obj);
			}
			catch(CompositionException ex)
			{
				Console.Write(ex.Message);
			}
		}

		public Plugins()
		{
			_plugins = new HashSet<APlugin>();
		}

		public void Add(APlugin aPlugin)
		{
			_plugins.Add(aPlugin);
		}

		public void StartAll()
		{
			foreach (APlugin plugin in _plugins)
			{
				plugin.Start(plugin.GetType().ToString());
			}
		}

		public void StopAll()
		{
			foreach (APlugin plugin in _plugins)
			{
				plugin.Stop();
			}
		}

		#endregion
	}
}
