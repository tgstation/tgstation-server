using System.ServiceProcess;

namespace TGS.Server.Service
{
	/// <inheritdoc />
	sealed class ServiceRunner : IServiceRunner
	{
		/// <inheritdoc />
		public void Run(ServiceBase service)
		{
			ServiceBase.Run(service);
		}
	}
}
