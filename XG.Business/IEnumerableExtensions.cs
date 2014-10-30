using System;
using System.Collections.Generic;
using XG.Plugin;

namespace XG.Business
{
	public static class IEnumerableExtensions
	{
		public static void StartRunAll(this IEnumerable<Lazy<IPlugin, IPluginMetaData>> Plugins)
		{
			foreach(var plugin in Plugins)
			{
				plugin.Value.StartRun();
			}
		}
		public static void StopRunAll(this IEnumerable<Lazy<IPlugin, IPluginMetaData>> Plugins)
		{
			foreach(var plugin in Plugins)
			{
				plugin.Value.StopRun();
			}
		}
	}
}
