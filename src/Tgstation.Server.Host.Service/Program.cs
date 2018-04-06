using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Contains the entrypoint for the application
	/// </summary>
	static class Program
	{
		/// <summary>
		/// Entrypoint for the application
		/// </summary>
		[ExcludeFromCodeCoverage]
		static void Main() => ServiceBase.Run(new ServerService(new WatchdogFactory()));
	}
}
