using System.ServiceProcess;

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
		static void Main() => ServiceBase.Run(new ServerService(new ServerFactory()));
	}
}
