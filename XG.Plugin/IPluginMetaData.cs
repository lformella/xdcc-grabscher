using System;

namespace XG.Plugin
{
	public interface IPluginMetaData
	{
		string Name { get; }
		string Description { get; }
		//string Version { get; }
		string Author { get; }
		string Website { get; }
	}
	public static class PluginMetaData
	{
		public const string NAME = "Name";
		public const string DESCRIPTION = "Description";
		//public const string VERSION = "Version";
		public const string AUTHOR = "Author";
		public const string WEBSITE = "Website";
	}
}
