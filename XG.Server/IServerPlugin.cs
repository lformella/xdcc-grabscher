using System;
using XG.Core;

namespace XG.Server
{
	/// <summary>
	/// 
	/// </summary>
	public interface IServerPlugin
	{
		/// <summary>
		/// 
		/// </summary>
		void Start(ServerRunner aParent);
		
		/// <summary>
		/// 
		/// </summary>
		void Stop();
		
		/// <summary>
		/// 
		/// </summary>
		void Restart();
	}
}
