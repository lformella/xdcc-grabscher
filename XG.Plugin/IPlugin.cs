using System;
using System.Configuration;

namespace XG.Plugin
{
	public interface IPlugin
	{
		void StartRun();
		void StopRun();
	}
}